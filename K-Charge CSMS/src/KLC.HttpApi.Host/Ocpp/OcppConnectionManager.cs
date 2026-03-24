using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp;

/// <summary>
/// Thread-safe registry for all active OCPP WebSocket connections.
/// Registered as singleton in DI.
/// </summary>
public class OcppConnectionManager
{
    private readonly ConcurrentDictionary<string, OcppConnection> _connections = new();
    private readonly ILogger<OcppConnectionManager> _logger;

    public OcppConnectionManager(ILogger<OcppConnectionManager> logger)
    {
        _logger = logger;
    }

    public bool TryAdd(string chargePointId, OcppConnection connection)
    {
        // If an existing connection exists, remove it first (charger reconnected)
        if (_connections.TryRemove(chargePointId, out var existing))
        {
            _logger.LogWarning(
                "Replacing existing connection for {ChargePointId} (connected since {ConnectedAt})",
                chargePointId, existing.ConnectedAt);
            existing.Cts.Cancel();
        }

        var result = _connections.TryAdd(chargePointId, connection);
        if (result)
        {
            _logger.LogInformation(
                "Charge point connected: {ChargePointId}. Total connections: {Count}",
                chargePointId, _connections.Count);
        }
        return result;
    }

    public bool TryRemove(string chargePointId, out OcppConnection? connection)
    {
        var result = _connections.TryRemove(chargePointId, out connection);
        if (result)
        {
            _logger.LogInformation(
                "Charge point disconnected: {ChargePointId}. Total connections: {Count}",
                chargePointId, _connections.Count);
        }
        return result;
    }

    public OcppConnection? GetConnection(string chargePointId)
    {
        _connections.TryGetValue(chargePointId, out var connection);
        return connection;
    }

    public IEnumerable<string> GetConnectedChargePoints()
    {
        return _connections.Keys.ToList();
    }

    public bool IsConnected(string chargePointId)
    {
        return _connections.ContainsKey(chargePointId);
    }

    public int GetConnectionCount()
    {
        return _connections.Count;
    }
}
