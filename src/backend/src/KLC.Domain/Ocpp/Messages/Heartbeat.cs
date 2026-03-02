using System.Text.Json.Serialization;

namespace KLC.Ocpp.Messages;

/// <summary>
/// OCPP 1.6J Heartbeat.req - Sent by Charge Point to indicate it is still connected.
/// </summary>
public class HeartbeatRequest
{
    // Heartbeat request has no fields
}

/// <summary>
/// OCPP 1.6J Heartbeat.conf - Response to Heartbeat.
/// </summary>
public class HeartbeatResponse
{
    /// <summary>
    /// Current time of Central System.
    /// </summary>
    [JsonPropertyName("currentTime")]
    public string CurrentTime { get; set; } = string.Empty;
}
