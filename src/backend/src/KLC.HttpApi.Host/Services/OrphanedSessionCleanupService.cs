using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Sessions;
using KLC.Stations;
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
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin();
        var totalCleaned = 0;

        // --- 1. Timeout Pending/Starting/Stopping sessions ---
        // Pending/Starting: user scanned QR but charger never responded
        // Stopping: user tapped Stop but charger never sent StopTransaction
        var cutoff = DateTime.UtcNow - _pendingTimeout;
        var stuckSessions = await sessionRepo.GetListAsync(
            s => (s.Status == SessionStatus.Pending ||
                  s.Status == SessionStatus.Starting ||
                  s.Status == SessionStatus.Stopping) &&
                 s.StartTime < cutoff);

        foreach (var session in stuckSessions)
        {
            var reason = session.Status == SessionStatus.Stopping
                ? $"Timed out after {_pendingTimeout.TotalMinutes:0} minutes — charger never confirmed stop"
                : $"Timed out after {_pendingTimeout.TotalMinutes:0} minutes — charger never started";
            session.MarkFailed(reason);
            await sessionRepo.UpdateAsync(session);

            // Reset connector to Available so other users can charge
            var connector = await connectorRepo.FirstOrDefaultAsync(
                c => c.StationId == session.StationId && c.ConnectorNumber == session.ConnectorNumber);
            if (connector != null && (connector.Status == ConnectorStatus.Preparing || connector.Status == ConnectorStatus.Finishing))
            {
                connector.UpdateStatus(ConnectorStatus.Available);
                await connectorRepo.UpdateAsync(connector);
            }

            totalCleaned++;
            _logger.LogInformation(
                "Stuck {Status} session {SessionId} timed out after {Minutes}min (user: {UserId}, station: {StationId})",
                session.Status, session.Id, _pendingTimeout.TotalMinutes, session.UserId, session.StationId);
        }

        // --- 2. Timeout InProgress sessions with offline station ---
        var activeSessions = await sessionRepo.GetListAsync(
            s => s.OcppTransactionId != null &&
                 (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Suspended));

        foreach (var session in activeSessions)
        {
            var station = await stationRepo.FirstOrDefaultAsync(s => s.Id == session.StationId);
            if (station == null) continue;
            if (station.Status != StationStatus.Offline) continue;
            if (station.LastHeartbeat.HasValue &&
                DateTime.UtcNow - station.LastHeartbeat.Value < _offlineGracePeriod) continue;

            session.MarkFailed("Station offline beyond grace period — no StopTransaction received");
            await sessionRepo.UpdateAsync(session);
            totalCleaned++;

            _logger.LogWarning(
                "InProgress session {SessionId} (txn={TransactionId}) failed — station offline. Last heartbeat: {LastHeartbeat}",
                session.Id, session.OcppTransactionId, station.LastHeartbeat);
        }

        // --- 3. Deduplicate: only keep the NEWEST InProgress session per connector ---
        // Multiple InProgress sessions on the same connector means older ones are orphaned
        // (simulator disconnected before sending StopTransaction)
        var inProgressSessions = await sessionRepo.GetListAsync(
            s => s.Status == SessionStatus.InProgress && s.OcppTransactionId != null);

        var grouped = inProgressSessions
            .GroupBy(s => new { s.StationId, s.ConnectorNumber })
            .Where(g => g.Count() > 1);

        foreach (var group in grouped)
        {
            // Keep the newest, fail all older ones
            var ordered = group.OrderByDescending(s => s.StartTime).ToList();
            foreach (var stale in ordered.Skip(1))
            {
                stale.MarkFailed("Superseded by newer session on same connector");
                await sessionRepo.UpdateAsync(stale);
                totalCleaned++;

                _logger.LogInformation(
                    "Duplicate InProgress session {SessionId} failed — newer session exists on same connector",
                    stale.Id);
            }
        }

        await uow.CompleteAsync();

        if (totalCleaned > 0)
        {
            _logger.LogInformation("OrphanedSessionCleanup: cleaned {Count} sessions", totalCleaned);
        }
    }
}
