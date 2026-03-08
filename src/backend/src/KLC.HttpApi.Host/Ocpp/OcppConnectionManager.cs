using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using KLC.Enums;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp;

/// <summary>
/// Manages active OCPP WebSocket connections.
/// </summary>
public class OcppConnectionManager
{
    private readonly ConcurrentDictionary<string, OcppConnection> _connections = new();
    private readonly ILogger<OcppConnectionManager> _logger;

    public OcppConnectionManager(ILogger<OcppConnectionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get the number of active connections.
    /// </summary>
    public int ConnectionCount => _connections.Count;

    /// <summary>
    /// Add a new connection.
    /// </summary>
    public OcppConnection AddConnection(string chargePointId, WebSocket webSocket, OcppProtocolVersion ocppVersion = OcppProtocolVersion.Ocpp16J)
    {
        var connection = new OcppConnection(chargePointId, webSocket, ocppVersion);

        // Remove existing connection if any
        if (_connections.TryRemove(chargePointId, out var existing))
        {
            _logger.LogWarning("Replacing existing connection for ChargePoint {ChargePointId}", chargePointId);
            existing.CancelAllPendingRequests();
        }

        _connections[chargePointId] = connection;
        _logger.LogInformation("ChargePoint {ChargePointId} connected. Total connections: {Count}",
            chargePointId, _connections.Count);

        return connection;
    }

    /// <summary>
    /// Remove a connection.
    /// </summary>
    public void RemoveConnection(string chargePointId)
    {
        if (_connections.TryRemove(chargePointId, out var connection))
        {
            connection.CancelAllPendingRequests();
            _logger.LogInformation("ChargePoint {ChargePointId} disconnected. Total connections: {Count}",
                chargePointId, _connections.Count);
        }
    }

    /// <summary>
    /// Get a connection by ChargePoint ID.
    /// </summary>
    public OcppConnection? GetConnection(string chargePointId)
    {
        _connections.TryGetValue(chargePointId, out var connection);
        return connection;
    }

    /// <summary>
    /// Check if a ChargePoint is connected.
    /// </summary>
    public bool IsConnected(string chargePointId)
    {
        return _connections.ContainsKey(chargePointId);
    }

    /// <summary>
    /// Get all active connections.
    /// </summary>
    public IEnumerable<OcppConnection> GetAllConnections()
    {
        return _connections.Values;
    }

    /// <summary>
    /// Get connections that haven't sent heartbeat within timeout.
    /// </summary>
    public IEnumerable<OcppConnection> GetStaleConnections(TimeSpan timeout)
    {
        var threshold = DateTime.UtcNow - timeout;
        return _connections.Values.Where(c => c.LastHeartbeat < threshold);
    }
}
