using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KLC.Hubs;
using KLC.Ocpp;
using KLC.PowerSharing;
using KLC.Stations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace KLC.Ocpp;

/// <summary>
/// Background service that periodically recalculates power allocations
/// for active power sharing groups and dispatches SetChargingProfile
/// commands to connected chargers via OCPP.
/// </summary>
public class PowerBalancingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OcppConnectionManager _connectionManager;
    private readonly IMonitoringNotifier _notifier;
    private readonly ILogger<PowerBalancingService> _logger;
    private readonly TimeSpan _balancingInterval = TimeSpan.FromSeconds(10);

    public PowerBalancingService(
        IServiceProvider serviceProvider,
        OcppConnectionManager connectionManager,
        IMonitoringNotifier notifier,
        ILogger<PowerBalancingService> logger)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = connectionManager;
        _notifier = notifier;
        _logger = logger;
    }

    /// <summary>
    /// Triggers an immediate rebalancing cycle for all active groups.
    /// Called by OcppMessageHandler on StartTransaction/StopTransaction events.
    /// </summary>
    public void TriggerRebalance()
    {
        _rebalanceTrigger.TrySetResult();
    }

    private volatile TaskCompletionSource _rebalanceTrigger = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PowerBalancingService started (interval: {Interval}s)", _balancingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for either the periodic interval or an immediate trigger
                var delayTask = Task.Delay(_balancingInterval, stoppingToken);
                var triggerTask = _rebalanceTrigger.Task;
                await Task.WhenAny(delayTask, triggerTask);

                // Reset trigger for next cycle
                Interlocked.Exchange(ref _rebalanceTrigger, new TaskCompletionSource());

                if (stoppingToken.IsCancellationRequested) break;

                await BalanceAllGroupsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PowerBalancingService");
            }
        }

        _logger.LogInformation("PowerBalancingService stopped");
    }

    private async Task BalanceAllGroupsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var groupRepository = scope.ServiceProvider.GetRequiredService<IRepository<PowerSharingGroup, Guid>>();
        var stationRepository = scope.ServiceProvider.GetRequiredService<IRepository<ChargingStation, Guid>>();
        var connectorRepository = scope.ServiceProvider.GetRequiredService<IRepository<Connector, Guid>>();
        var powerSharingService = scope.ServiceProvider.GetRequiredService<IPowerSharingService>();
        var remoteCommandService = scope.ServiceProvider.GetRequiredService<IOcppRemoteCommandService>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = unitOfWorkManager.Begin();

        var activeGroups = await groupRepository.GetListAsync(g => g.IsActive);
        if (activeGroups.Count == 0)
            return;

        foreach (var group in activeGroups)
        {
            try
            {
                await BalanceGroupAsync(group, stationRepository, connectorRepository,
                    powerSharingService, remoteCommandService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to balance power sharing group {GroupId} ({GroupName})",
                    group.Id, group.Name);
            }
        }

        await uow.CompleteAsync();
    }

    private async Task BalanceGroupAsync(
        PowerSharingGroup group,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IPowerSharingService powerSharingService,
        IOcppRemoteCommandService remoteCommandService)
    {
        var allocations = await powerSharingService.RecalculateAllocationsAsync(group.Id);
        if (allocations.Count == 0)
            return;

        // Build lookup: StationId → StationCode for OCPP commands
        var stationIds = allocations.Select(a => a.StationId).Distinct().ToList();
        var stations = await stationRepository.GetListAsync(s => stationIds.Contains(s.Id));
        var stationCodeMap = stations.ToDictionary(s => s.Id, s => s.StationCode);

        // Build lookup: ConnectorId → ConnectorNumber for OCPP commands
        var connectorIds = allocations.Select(a => a.ConnectorId).Distinct().ToList();
        var connectors = await connectorRepository.GetListAsync(c => connectorIds.Contains(c.Id));
        var connectorNumberMap = connectors.ToDictionary(c => c.Id, c => c.ConnectorNumber);

        var dispatched = 0;
        var skipped = 0;
        var connectorAllocations = new List<ConnectorAllocation>();

        foreach (var allocation in allocations)
        {
            if (!stationCodeMap.TryGetValue(allocation.StationId, out var stationCode) ||
                !connectorNumberMap.TryGetValue(allocation.ConnectorId, out var connectorNumber))
            {
                skipped++;
                continue;
            }

            // Track allocation for SignalR notification
            connectorAllocations.Add(new ConnectorAllocation(
                allocation.ConnectorId,
                allocation.StationId,
                stationCode,
                connectorNumber,
                allocation.AllocatedPowerKw,
                allocation.MaxPowerKw));

            // Only send profiles to connected stations with non-zero allocations
            if (allocation.AllocatedPowerKw <= 0 || !_connectionManager.IsConnected(stationCode))
            {
                skipped++;
                continue;
            }

            var limitWatts = allocation.AllocatedPowerKw * 1000m;

            var profile = new ChargingProfilePayload(
                ChargingProfileId: 100 + connectorNumber, // Unique per connector
                TransactionId: null,
                StackLevel: 0,
                ChargingProfilePurpose: "TxDefaultProfile",
                ChargingProfileKind: "Absolute",
                ChargingSchedule: new ChargingSchedulePayload(
                    ChargingRateUnit: "W",
                    ChargingSchedulePeriod: new List<ChargingSchedulePeriodPayload>
                    {
                        new(StartPeriod: 0, Limit: limitWatts)
                    }));

            var result = await remoteCommandService.SendSetChargingProfileAsync(
                stationCode, connectorNumber, profile);

            if (result.Accepted)
            {
                dispatched++;
            }
            else
            {
                _logger.LogWarning(
                    "SetChargingProfile rejected for {StationCode} connector {ConnectorNumber}: {Error}",
                    stationCode, connectorNumber, result.ErrorMessage);
            }
        }

        if (dispatched > 0 || _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogInformation(
                "Power balancing for group '{GroupName}': {Dispatched} profiles sent, {Skipped} skipped",
                group.Name, dispatched, skipped);
        }

        // Push real-time allocation update via SignalR
        if (connectorAllocations.Count > 0)
        {
            var update = new PowerAllocationUpdate(
                group.Id,
                group.Name,
                group.MaxCapacityKw,
                connectorAllocations.Sum(a => a.AllocatedPowerKw),
                connectorAllocations.Count(a => a.AllocatedPowerKw > 0),
                dispatched,
                connectorAllocations,
                DateTime.UtcNow);

            await _notifier.NotifyPowerAllocationChangedAsync(update);
        }

        // Record load profile snapshot periodically (every 6th cycle ≈ 60s)
        if (DateTime.UtcNow.Second < 10)
        {
            await powerSharingService.RecordLoadProfileAsync(group.Id);
        }
    }
}
