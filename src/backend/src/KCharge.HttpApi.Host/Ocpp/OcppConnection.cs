using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace KCharge.Ocpp;

/// <summary>
/// Represents an active OCPP WebSocket connection to a Charge Point.
/// </summary>
public class OcppConnection
{
    /// <summary>
    /// Unique identifier of the Charge Point (station code).
    /// </summary>
    public string ChargePointId { get; }

    /// <summary>
    /// The WebSocket connection.
    /// </summary>
    public WebSocket WebSocket { get; }

    /// <summary>
    /// When the connection was established.
    /// </summary>
    public DateTime ConnectedAt { get; }

    /// <summary>
    /// Last heartbeat received from the Charge Point.
    /// </summary>
    public DateTime LastHeartbeat { get; private set; }

    /// <summary>
    /// Whether the connection is authenticated/registered.
    /// </summary>
    public bool IsRegistered { get; private set; }

    /// <summary>
    /// Station ID in the database (set after BootNotification).
    /// </summary>
    public Guid? StationId { get; private set; }

    /// <summary>
    /// Pending requests awaiting response from Charge Point.
    /// </summary>
    private readonly Dictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
    private readonly object _lock = new();

    public OcppConnection(string chargePointId, WebSocket webSocket)
    {
        ChargePointId = chargePointId;
        WebSocket = webSocket;
        ConnectedAt = DateTime.UtcNow;
        LastHeartbeat = DateTime.UtcNow;
        IsRegistered = false;
    }

    public void RecordHeartbeat()
    {
        LastHeartbeat = DateTime.UtcNow;
    }

    public void SetRegistered(Guid stationId)
    {
        IsRegistered = true;
        StationId = stationId;
    }

    /// <summary>
    /// Register a pending request to track response.
    /// </summary>
    public TaskCompletionSource<string> RegisterPendingRequest(string uniqueId)
    {
        var tcs = new TaskCompletionSource<string>();
        lock (_lock)
        {
            _pendingRequests[uniqueId] = tcs;
        }
        return tcs;
    }

    /// <summary>
    /// Complete a pending request with a response.
    /// </summary>
    public bool TryCompletePendingRequest(string uniqueId, string response)
    {
        TaskCompletionSource<string>? tcs;
        lock (_lock)
        {
            if (!_pendingRequests.TryGetValue(uniqueId, out tcs))
                return false;
            _pendingRequests.Remove(uniqueId);
        }
        tcs.TrySetResult(response);
        return true;
    }

    /// <summary>
    /// Cancel all pending requests (on disconnect).
    /// </summary>
    public void CancelAllPendingRequests()
    {
        lock (_lock)
        {
            foreach (var tcs in _pendingRequests.Values)
            {
                tcs.TrySetCanceled();
            }
            _pendingRequests.Clear();
        }
    }
}
