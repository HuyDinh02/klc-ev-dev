using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KCharge.Ocpp;

/// <summary>
/// Background service that monitors Charge Point connections and marks offline stations.
/// </summary>
public class HeartbeatMonitorService : BackgroundService
{
    private readonly OcppConnectionManager _connectionManager;
    private readonly ILogger<HeartbeatMonitorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromMinutes(6); // 5 min interval + 1 min grace

    public HeartbeatMonitorService(
        OcppConnectionManager connectionManager,
        ILogger<HeartbeatMonitorService> logger)
    {
        _connectionManager = connectionManager;
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
                CheckStaleConnections();
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

    private void CheckStaleConnections()
    {
        var staleConnections = _connectionManager.GetStaleConnections(_heartbeatTimeout).ToList();

        foreach (var connection in staleConnections)
        {
            _logger.LogWarning(
                "ChargePoint {ChargePointId} heartbeat timeout. Last heartbeat: {LastHeartbeat}",
                connection.ChargePointId,
                connection.LastHeartbeat);

            // TODO: Mark station as Offline in database
            // TODO: Create HeartbeatTimeout alert

            // The connection will be removed when WebSocket closes or on next check
        }

        if (_connectionManager.ConnectionCount > 0)
        {
            _logger.LogDebug("Active OCPP connections: {Count}", _connectionManager.ConnectionCount);
        }
    }
}
