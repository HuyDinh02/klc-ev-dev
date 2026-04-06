using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Ocpp.Messages;

namespace KLC.Ocpp;

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
    /// OCPP protocol version negotiated during WebSocket handshake.
    /// </summary>
    public OcppProtocolVersion OcppVersion { get; }

    /// <summary>
    /// Detected vendor profile type for this connection.
    /// </summary>
    public VendorProfileType VendorProfileType { get; private set; } = VendorProfileType.Generic;

    /// <summary>
    /// Whether post-boot configuration needs to be sent after BootNotification response.
    /// </summary>
    public bool PendingPostBootConfig { get; set; }

    /// <summary>
    /// Pending requests awaiting response from Charge Point.
    /// </summary>
    private readonly Dictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
    private readonly object _lock = new();

    /// <summary>
    /// Serializes concurrent WebSocket send operations. WebSocket.SendAsync is NOT thread-safe;
    /// the receive loop and background tasks (post-boot config, SoC auto-stop) can race.
    /// </summary>
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public OcppConnection(string chargePointId, WebSocket webSocket, OcppProtocolVersion ocppVersion = OcppProtocolVersion.Ocpp16J)
    {
        ChargePointId = chargePointId;
        WebSocket = webSocket;
        OcppVersion = ocppVersion;
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

    public void SetVendorProfile(VendorProfileType vendorProfileType)
    {
        VendorProfileType = vendorProfileType;
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
            var pending = _pendingRequests.Values.ToList();
            _pendingRequests.Clear();
            foreach (var tcs in pending)
            {
                tcs.TrySetCanceled();
            }
        }
    }

    /// <summary>
    /// Send a raw text message over the WebSocket, serializing concurrent sends via _sendLock.
    /// </summary>
    public async Task SendTextAsync(string message, CancellationToken cancellationToken = default)
    {
        if (WebSocket.State != WebSocketState.Open)
            return;

        var bytes = Encoding.UTF8.GetBytes(message);
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Send an OCPP Call message and wait for a response with timeout.
    /// Uses the connection's negotiated protocol version for framing.
    /// </summary>
    public async Task<string?> SendCallAsync(string action, object payload, TimeSpan timeout, IOcppMessageParser? parser = null)
    {
        if (WebSocket.State != WebSocketState.Open)
            return null;

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var message = parser != null
            ? parser.SerializeCall(uniqueId, action, payload)
            : JsonSerializer.Serialize(new object[]
            {
                OcppMessageType.Call,
                uniqueId,
                action,
                payload
            });

        var tcs = RegisterPendingRequest(uniqueId);

        // Use _sendLock to serialize concurrent sends (receive loop vs background tasks)
        await SendTextAsync(message);

        using var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
