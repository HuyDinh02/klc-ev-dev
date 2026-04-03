using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Sessions;
using KLC.Stations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace KLC.Services;

/// <summary>
/// Background service that cleans up orphaned sessions:
/// 1. Pending/Starting sessions that never got a charger response (5 min timeout)
/// 2. InProgress sessions whose station has been offline beyond grace period (10 min)
/// </summary>
public class OrphanedSessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrphanedSessionCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _pendingTimeout = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _offlineGracePeriod = TimeSpan.FromMinutes(10);

    public OrphanedSessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<OrphanedSessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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

        // --- 1. Timeout Pending/Starting sessions (user scanned QR but never plugged in) ---
        var cutoff = DateTime.UtcNow - _pendingTimeout;
        var pendingSessions = await sessionRepo.GetListAsync(
            s => s.OcppTransactionId == null &&
                 (s.Status == SessionStatus.Pending || s.Status == SessionStatus.Starting) &&
                 s.StartTime < cutoff);

        foreach (var session in pendingSessions)
        {
            session.MarkFailed($"Timed out after {_pendingTimeout.TotalMinutes:0} minutes — charger never started");
            await sessionRepo.UpdateAsync(session);

            // Reset connector to Available so other users can charge
            var connector = await connectorRepo.FirstOrDefaultAsync(
                c => c.StationId == session.StationId && c.ConnectorNumber == session.ConnectorNumber);
            if (connector != null && connector.Status == ConnectorStatus.Preparing)
            {
                connector.UpdateStatus(ConnectorStatus.Available);
                await connectorRepo.UpdateAsync(connector);
            }

            totalCleaned++;
            _logger.LogInformation(
                "Pending session {SessionId} timed out after {Minutes}min (user: {UserId}, station: {StationId})",
                session.Id, _pendingTimeout.TotalMinutes, session.UserId, session.StationId);
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

        await uow.CompleteAsync();

        if (totalCleaned > 0)
        {
            _logger.LogInformation("OrphanedSessionCleanup: cleaned {Count} sessions", totalCleaned);
        }
    }
}
