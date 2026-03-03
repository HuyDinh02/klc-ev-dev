using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Notifications;
using KLC.Stations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace KLC.Ocpp;

/// <summary>
/// Background service that monitors Charge Point connections and marks offline stations.
/// </summary>
public class HeartbeatMonitorService : BackgroundService
{
    private readonly OcppConnectionManager _connectionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HeartbeatMonitorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromMinutes(6); // 5 min interval + 1 min grace

    public HeartbeatMonitorService(
        OcppConnectionManager connectionManager,
        IServiceProvider serviceProvider,
        ILogger<HeartbeatMonitorService> logger)
    {
        _connectionManager = connectionManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HeartbeatMonitorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await CheckStaleConnectionsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HeartbeatMonitorService");
            }
        }

        _logger.LogInformation("HeartbeatMonitorService stopped");
    }

    private async Task CheckStaleConnectionsAsync()
    {
        var staleConnections = _connectionManager.GetStaleConnections(_heartbeatTimeout).ToList();

        if (staleConnections.Count == 0)
        {
            if (_connectionManager.ConnectionCount > 0)
            {
                _logger.LogDebug("Active OCPP connections: {Count}", _connectionManager.ConnectionCount);
            }
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var stationRepository = scope.ServiceProvider.GetRequiredService<IRepository<ChargingStation, Guid>>();
        var alertRepository = scope.ServiceProvider.GetRequiredService<IRepository<Alert, Guid>>();
        var guidGenerator = scope.ServiceProvider.GetRequiredService<IGuidGenerator>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = unitOfWorkManager.Begin();

        foreach (var connection in staleConnections)
        {
            _logger.LogWarning(
                "ChargePoint {ChargePointId} heartbeat timeout. Last heartbeat: {LastHeartbeat}",
                connection.ChargePointId,
                connection.LastHeartbeat);

            try
            {
                var station = await stationRepository.FirstOrDefaultAsync(
                    s => s.StationCode == connection.ChargePointId);

                if (station == null)
                {
                    _logger.LogWarning("Station not found for ChargePoint {ChargePointId}", connection.ChargePointId);
                    continue;
                }

                // Mark station as offline
                station.UpdateStatus(StationStatus.Unavailable);
                await stationRepository.UpdateAsync(station);

                // Create HeartbeatTimeout alert
                var alert = new Alert(
                    guidGenerator.Create(),
                    AlertType.HeartbeatTimeout,
                    $"Station {station.Name} ({station.StationCode}) heartbeat timeout. Last heartbeat: {connection.LastHeartbeat:u}",
                    station.Id);

                await alertRepository.InsertAsync(alert);

                _logger.LogInformation(
                    "Station {StationCode} marked offline and alert created due to heartbeat timeout",
                    connection.ChargePointId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process stale connection for {ChargePointId}",
                    connection.ChargePointId);
            }
        }

        await uow.CompleteAsync();

        if (_connectionManager.ConnectionCount > 0)
        {
            _logger.LogDebug("Active OCPP connections: {Count}", _connectionManager.ConnectionCount);
        }
    }
}
