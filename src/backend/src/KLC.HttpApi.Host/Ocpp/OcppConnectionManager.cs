using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
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
    /// Remove a connection only if it is still the active connection for that ChargePoint.
    /// Returns true if the connection was removed, false if it had already been replaced by a newer connection.
    /// </summary>
    public bool RemoveConnection(string chargePointId, OcppConnection connection)
    {
        // Only remove if the stored connection is still the same object we're closing.
        // Without this guard, an old receive loop's finally block would remove the NEW connection
        // when a charger reconnects before the old loop exits.
        if (_connections.TryGetValue(chargePointId, out var current) && ReferenceEquals(current, connection))
        {
            if (_connections.TryRemove(chargePointId, out _))
            {
                connection.CancelAllPendingRequests();
                _logger.LogInformation("ChargePoint {ChargePointId} disconnected. Total connections: {Count}",
                    chargePointId, _connections.Count);
                return true;
            }
        }
        else
        {
            // Connection was already replaced by a newer one — skip cleanup to avoid stomping on it
            _logger.LogInformation(
                "ChargePoint {ChargePointId} old receive loop exiting but connection already replaced — skipping disconnect cleanup",
                chargePointId);
            connection.CancelAllPendingRequests();
        }
        return false;
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

    /// <summary>
    /// Close all WebSocket connections gracefully on shutdown.
    /// Forces chargers to reconnect to the new instance after deploy.
    /// </summary>
    public async Task CloseAllConnectionsAsync()
    {
        var connections = _connections.Values.ToList();
        _logger.LogInformation("Closing {Count} WebSocket connections for graceful shutdown", connections.Count);

        foreach (var connection in connections)
        {
            try
            {
                if (connection.WebSocket.State == WebSocketState.Open)
                {
                    await connection.WebSocket.CloseAsync(
                        WebSocketCloseStatus.EndpointUnavailable,
                        "Server shutting down",
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing WebSocket for {ChargePointId}", connection.ChargePointId);
            }
        }

        _connections.Clear();
        _logger.LogInformation("All WebSocket connections closed");
    }
}
