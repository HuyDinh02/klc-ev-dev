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
/// Background service that fails InProgress sessions whose station has been offline
/// beyond the grace period. This handles real charger failures (power outage, hardware crash)
/// where StopTransaction is never sent.
///
/// Grace period allows transient disconnects (network blips, Cloud Run scaling) to recover
/// without falsely failing sessions.
/// </summary>
public class OrphanedSessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrphanedSessionCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _gracePeriod = TimeSpan.FromMinutes(10);

    public OrphanedSessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<OrphanedSessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrphanedSessionCleanupService started (grace period: {GracePeriod})", _gracePeriod);

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
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin();

        // Find InProgress/Suspended sessions with an OCPP transaction
        var activeSessions = (await sessionRepo.GetListAsync(
            s => s.OcppTransactionId != null &&
                 (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Suspended)));

        var failedCount = 0;

        foreach (var session in activeSessions)
        {
            var station = await stationRepo.FirstOrDefaultAsync(s => s.Id == session.StationId);
            if (station == null) continue;

            // Only fail sessions whose station is Offline beyond the grace period
            if (station.Status != StationStatus.Offline) continue;
            if (station.LastHeartbeat.HasValue &&
                DateTime.UtcNow - station.LastHeartbeat.Value < _gracePeriod) continue;

            // Grace period expired — fail the session and calculate billing from last meter value
            session.MarkFailed("Station offline beyond grace period — no StopTransaction received");
            await sessionRepo.UpdateAsync(session);
            failedCount++;

            _logger.LogWarning(
                "Orphaned session {SessionId} (txn={TransactionId}) failed after grace period. " +
                "Station {StationCode} last heartbeat: {LastHeartbeat}",
                session.Id, session.OcppTransactionId, station.StationCode, station.LastHeartbeat);
        }

        await uow.CompleteAsync();

        if (failedCount > 0)
        {
            _logger.LogInformation("OrphanedSessionCleanup: failed {Count} sessions beyond grace period", failedCount);
        }
    }
}
