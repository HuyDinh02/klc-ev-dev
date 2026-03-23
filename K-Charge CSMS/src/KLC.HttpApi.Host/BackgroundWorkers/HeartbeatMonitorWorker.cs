using System.Net.WebSockets;
using KLC.ChargingStations;
using KLC.Ocpp;
using KLC.Ocpp.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Uow;

namespace KLC.BackgroundWorkers;

/// <summary>
/// Background worker that monitors charge point heartbeats.
/// Detects and disconnects timed-out charge points every 60 seconds.
/// Timeout threshold: HeartbeatInterval × 2 + 120s buffer = 720s (12 minutes).
/// </summary>
public class HeartbeatMonitorWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly OcppConnectionManager _connectionManager;
    private readonly ILogger<HeartbeatMonitorWorker> _logger;
    private const int TimeoutThresholdSeconds = 720; // 300*2 + 120

    public HeartbeatMonitorWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        OcppConnectionManager connectionManager,
        ILogger<HeartbeatMonitorWorker> logger)
        : base(timer, serviceScopeFactory)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        Timer.Period = 60_000; // Run every 60 seconds
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var chargePointIds = _connectionManager.GetConnectedChargePoints().ToList();
        if (chargePointIds.Count == 0) return;

        _logger.LogDebug("Heartbeat monitor: checking {Count} connections", chargePointIds.Count);
        var now = DateTime.UtcNow;

        foreach (var cpId in chargePointIds)
        {
            var conn = _connectionManager.GetConnection(cpId);
            if (conn == null) continue;

            var elapsed = (now - conn.LastHeartbeat).TotalSeconds;
            if (elapsed <= TimeoutThresholdSeconds) continue;

            _logger.LogWarning(
                "Heartbeat timeout for {ChargePointId}: last heartbeat {Elapsed:F0}s ago",
                cpId, elapsed);

            try
            {
                // Close WebSocket gracefully
                if (conn.WebSocket.State == WebSocketState.Open)
                {
                    conn.Cts.Cancel();
                    try
                    {
                        await conn.WebSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Heartbeat timeout",
                            CancellationToken.None);
                    }
                    catch { /* Best effort close */ }
                }

                _connectionManager.TryRemove(cpId, out _);

                // Update station in database
                var stationRepo = workerContext.ServiceProvider
                    .GetRequiredService<IChargingStationRepository>();
                var eventBus = workerContext.ServiceProvider
                    .GetRequiredService<IDistributedEventBus>();
                var uowManager = workerContext.ServiceProvider
                    .GetRequiredService<IUnitOfWorkManager>();

                using var uow = uowManager.Begin();

                var station = await stationRepo.FindByChargePointIdAsync(cpId, ct: default);
                if (station != null)
                {
                    station.SetOffline();
                    await stationRepo.UpdateAsync(station);
                }

                // Notify dashboard
                await eventBus.PublishAsync(new ConnectorStatusChangedEto
                {
                    ChargePointId = cpId,
                    ConnectorId = 0,
                    Status = "Unavailable",
                    ErrorCode = "NoError",
                    Timestamp = now
                });

                await uow.CompleteAsync();

                _logger.LogInformation("Disconnected timed-out charge point {ChargePointId}", cpId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling heartbeat timeout for {ChargePointId}", cpId);
            }
        }
    }
}
