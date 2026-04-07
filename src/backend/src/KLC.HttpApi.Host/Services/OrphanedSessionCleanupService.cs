using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Sessions;
using KLC.Stations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace KLC.Services;

/// <summary>
/// Background service that cleans up orphaned sessions:
/// 1. Pending/Starting sessions that never got a charger response (configurable timeout)
/// 2. InProgress sessions whose station has been offline beyond grace period
///
/// Configure in appsettings.json:
///   "SessionCleanup": {
///     "CheckIntervalMinutes": 1,
///     "PendingTimeoutMinutes": 5,
///     "OfflineGracePeriodMinutes": 10
///   }
/// </summary>
public class OrphanedSessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrphanedSessionCleanupService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _pendingTimeout;
    private readonly TimeSpan _offlineGracePeriod;
    private readonly TimeSpan _staleInProgressTimeout;

    public OrphanedSessionCleanupService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OrphanedSessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _checkInterval = TimeSpan.FromMinutes(configuration.GetValue("SessionCleanup:CheckIntervalMinutes", 1));
        _pendingTimeout = TimeSpan.FromMinutes(configuration.GetValue("SessionCleanup:PendingTimeoutMinutes", 5));
        _offlineGracePeriod = TimeSpan.FromMinutes(configuration.GetValue("SessionCleanup:OfflineGracePeriodMinutes", 10));
        _staleInProgressTimeout = TimeSpan.FromMinutes(configuration.GetValue("SessionCleanup:StaleInProgressMinutes", 15));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OrphanedSessionCleanupService started (pending timeout: {PendingTimeout}, offline grace: {GracePeriod})",
            _pendingTimeout, _offlineGracePeriod);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await CleanupOrphanedSessionsAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OrphanedSessionCleanupService");
            }
        }
    }

    private async Task CleanupOrphanedSessionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<IRepository<ChargingSession, Guid>>();
        var stationRepo = scope.ServiceProvider.GetRequiredService<IRepository<ChargingStation, Guid>>();
        var connectorRepo = scope.ServiceProvider.GetRequiredService<IRepository<Connector, Guid>>();
        var tariffRepo = scope.ServiceProvider.GetRequiredService<IRepository<Tariffs.TariffPlan, Guid>>();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin();
        var totalCleaned = 0;

        // --- 1. Timeout Pending/Starting/Stopping sessions ---
        var cutoff = DateTime.UtcNow - _pendingTimeout;
        var stuckSessions = await sessionRepo.GetListAsync(
            s => (s.Status == SessionStatus.Pending ||
                  s.Status == SessionStatus.Starting ||
                  s.Status == SessionStatus.Stopping) &&
                 s.StartTime < cutoff);

        if (stuckSessions.Count > 0)
        {
            // Batch-load tariffs and connectors to avoid N+1
            var stuckTariffIds = stuckSessions
                .Where(s => s.TariffPlanId.HasValue)
                .Select(s => s.TariffPlanId!.Value)
                .Distinct().ToList();
            var stuckTariffs = stuckTariffIds.Count > 0
                ? (await (await tariffRepo.GetQueryableAsync())
                    .Where(t => stuckTariffIds.Contains(t.Id))
                    .ToListAsync())
                    .ToDictionary(t => t.Id)
                : new Dictionary<Guid, Tariffs.TariffPlan>();

            var stuckConnectorKeys = stuckSessions
                .Select(s => new { s.StationId, s.ConnectorNumber })
                .Distinct().ToList();
            var stuckStationIds = stuckConnectorKeys.Select(k => k.StationId).Distinct().ToList();
            var stuckConnectors = (await (await connectorRepo.GetQueryableAsync())
                .Where(c => stuckStationIds.Contains(c.StationId))
                .ToListAsync())
                .ToDictionary(c => (c.StationId, c.ConnectorNumber));

            foreach (var session in stuckSessions)
            {
                var reason = session.Status == SessionStatus.Stopping
                    ? $"Timed out after {_pendingTimeout.TotalMinutes:0} minutes — charger never confirmed stop"
                    : $"Timed out after {_pendingTimeout.TotalMinutes:0} minutes — charger never started";
                var tariff = session.TariffPlanId.HasValue
                    ? stuckTariffs.GetValueOrDefault(session.TariffPlanId.Value)
                    : null;
                session.MarkFailed(reason, tariff);
                await sessionRepo.UpdateAsync(session);

                if (stuckConnectors.TryGetValue((session.StationId, session.ConnectorNumber), out var connector)
                    && (connector.Status == ConnectorStatus.Preparing || connector.Status == ConnectorStatus.Finishing))
                {
                    connector.UpdateStatus(ConnectorStatus.Available);
                    await connectorRepo.UpdateAsync(connector);
                }

                totalCleaned++;
                _logger.LogInformation(
                    "Stuck {Status} session {SessionId} timed out after {Minutes}min (user: {UserId}, station: {StationId})",
                    session.Status, session.Id, _pendingTimeout.TotalMinutes, session.UserId, session.StationId);
            }
        }

        // --- 2. Timeout InProgress sessions with offline station ---
        var activeSessions = await sessionRepo.GetListAsync(
            s => s.OcppTransactionId != null &&
                 (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Suspended));

        if (activeSessions.Count > 0)
        {
            var activeStationIds = activeSessions.Select(s => s.StationId).Distinct().ToList();
            var activeStations = (await (await stationRepo.GetQueryableAsync())
                .Where(s => activeStationIds.Contains(s.Id))
                .ToListAsync())
                .ToDictionary(s => s.Id);

            var activeTariffIds = activeSessions
                .Where(s => s.TariffPlanId.HasValue)
                .Select(s => s.TariffPlanId!.Value)
                .Distinct().ToList();
            var activeTariffs = activeTariffIds.Count > 0
                ? (await (await tariffRepo.GetQueryableAsync())
                    .Where(t => activeTariffIds.Contains(t.Id))
                    .ToListAsync())
                    .ToDictionary(t => t.Id)
                : new Dictionary<Guid, Tariffs.TariffPlan>();

            foreach (var session in activeSessions)
            {
                if (!activeStations.TryGetValue(session.StationId, out var station)) continue;
                if (station.Status != StationStatus.Offline) continue;
                if (station.LastHeartbeat.HasValue &&
                    DateTime.UtcNow - station.LastHeartbeat.Value < _offlineGracePeriod) continue;

                var offlineTariff = session.TariffPlanId.HasValue
                    ? activeTariffs.GetValueOrDefault(session.TariffPlanId.Value)
                    : null;
                session.MarkFailed("Station offline beyond grace period — no StopTransaction received", offlineTariff);
                await sessionRepo.UpdateAsync(session);
                totalCleaned++;

                _logger.LogWarning(
                    "InProgress session {SessionId} (txn={TransactionId}) failed — station offline. Last heartbeat: {LastHeartbeat}",
                    session.Id, session.OcppTransactionId, station.LastHeartbeat);
            }
        }

        // --- 3. Deduplicate: only keep the NEWEST InProgress session per connector ---
        var inProgressSessions = await sessionRepo.GetListAsync(
            s => s.Status == SessionStatus.InProgress && s.OcppTransactionId != null);

        var grouped = inProgressSessions
            .GroupBy(s => new { s.StationId, s.ConnectorNumber })
            .Where(g => g.Count() > 1);

        var staleSessions = grouped
            .SelectMany(g => g.OrderByDescending(s => s.StartTime).Skip(1))
            .ToList();

        if (staleSessions.Count > 0)
        {
            var dupTariffIds = staleSessions
                .Where(s => s.TariffPlanId.HasValue)
                .Select(s => s.TariffPlanId!.Value)
                .Distinct().ToList();
            var dupTariffs = dupTariffIds.Count > 0
                ? (await (await tariffRepo.GetQueryableAsync())
                    .Where(t => dupTariffIds.Contains(t.Id))
                    .ToListAsync())
                    .ToDictionary(t => t.Id)
                : new Dictionary<Guid, Tariffs.TariffPlan>();

            foreach (var stale in staleSessions)
            {
                var staleTariff = stale.TariffPlanId.HasValue
                    ? dupTariffs.GetValueOrDefault(stale.TariffPlanId.Value)
                    : null;
                stale.MarkFailed("Superseded by newer session on same connector", staleTariff);
                await sessionRepo.UpdateAsync(stale);
                totalCleaned++;

                _logger.LogInformation(
                    "Duplicate InProgress session {SessionId} failed — newer session exists on same connector",
                    stale.Id);
            }
        }

        // --- 4. Stale InProgress sessions — no meter values received recently ---
        var staleCutoff = DateTime.UtcNow - _staleInProgressTimeout;
        var remainingInProgress = await sessionRepo.GetListAsync(
            s => s.Status == SessionStatus.InProgress && s.OcppTransactionId != null);

        var staleInProgress = remainingInProgress
            .Where(s => (s.LastModificationTime ?? s.StartTime ?? s.CreationTime) < staleCutoff)
            .ToList();

        if (staleInProgress.Count > 0)
        {
            var staleMeterTariffIds = staleInProgress
                .Where(s => s.TariffPlanId.HasValue)
                .Select(s => s.TariffPlanId!.Value)
                .Distinct().ToList();
            var staleMeterTariffs = staleMeterTariffIds.Count > 0
                ? (await (await tariffRepo.GetQueryableAsync())
                    .Where(t => staleMeterTariffIds.Contains(t.Id))
                    .ToListAsync())
                    .ToDictionary(t => t.Id)
                : new Dictionary<Guid, Tariffs.TariffPlan>();

            var staleConnectorStationIds = staleInProgress.Select(s => s.StationId).Distinct().ToList();
            var staleConnectors = (await (await connectorRepo.GetQueryableAsync())
                .Where(c => staleConnectorStationIds.Contains(c.StationId))
                .ToListAsync())
                .ToDictionary(c => (c.StationId, c.ConnectorNumber));

            foreach (var session in staleInProgress)
            {
                var staleMeterTariff = session.TariffPlanId.HasValue
                    ? staleMeterTariffs.GetValueOrDefault(session.TariffPlanId.Value)
                    : null;
                session.MarkFailed($"No meter data received for {_staleInProgressTimeout.TotalMinutes:0} minutes", staleMeterTariff);
                await sessionRepo.UpdateAsync(session);

                if (staleConnectors.TryGetValue((session.StationId, session.ConnectorNumber), out var connector)
                    && connector.Status != ConnectorStatus.Available)
                {
                    connector.UpdateStatus(ConnectorStatus.Available);
                    await connectorRepo.UpdateAsync(connector);
                }

                totalCleaned++;
                _logger.LogWarning(
                    "Stale InProgress session {SessionId} failed — no meter data for {Minutes}min. Energy={Energy}kWh",
                    session.Id, _staleInProgressTimeout.TotalMinutes, session.TotalEnergyKwh);
            }
        }

        await uow.CompleteAsync();

        if (totalCleaned > 0)
        {
            _logger.LogInformation("OrphanedSessionCleanup: cleaned {Count} sessions", totalCleaned);
        }
    }
}
