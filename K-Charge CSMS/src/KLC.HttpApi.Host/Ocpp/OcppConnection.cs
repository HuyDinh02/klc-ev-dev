using System.Collections.Concurrent;
using System.Net.WebSockets;
using Newtonsoft.Json.Linq;

namespace KLC.Ocpp;

public class OcppConnection
{
    public string ChargePointId { get; set; } = null!;
    public WebSocket WebSocket { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public DateTime LastMessageReceived { get; set; }
    public CancellationTokenSource Cts { get; set; } = null!;
    public ConcurrentDictionary<string, TaskCompletionSource<JArray>> PendingRequests { get; } = new();
}
