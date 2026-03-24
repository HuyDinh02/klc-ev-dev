using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Handles Heartbeat messages from charge points.
/// Updates the last heartbeat timestamp on the connection.
/// </summary>
public class HeartbeatHandler : IOcppMessageHandler
{
    private readonly OcppConnectionManager _connectionManager;
    private readonly ILogger<HeartbeatHandler> _logger;

    public HeartbeatHandler(
        OcppConnectionManager connectionManager,
        ILogger<HeartbeatHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public Task<JObject> HandleAsync(
        string chargePointId,
        JObject payload,
        CancellationToken cancellationToken)
    {
        // Update heartbeat timestamp
        var connection = _connectionManager.GetConnection(chargePointId);
        if (connection != null)
        {
            connection.LastHeartbeat = DateTime.UtcNow;
            connection.LastMessageReceived = DateTime.UtcNow;
            _logger.LogDebug("Heartbeat from {ChargePointId}", chargePointId);
        }

        return Task.FromResult(new JObject
        {
            ["currentTime"] = DateTime.UtcNow.ToString("O")
        });
    }
}
