using Newtonsoft.Json.Linq;

namespace KLC.Ocpp;

/// <summary>
/// Interface for sending OCPP commands from CSMS to Charge Points.
/// Implemented in the HttpApi.Host layer where WebSocket connections are managed.
/// </summary>
public interface IOcppMessageDispatcher
{
    /// <summary>
    /// Send an OCPP CALL command to a charge point and wait for the response.
    /// </summary>
    /// <returns>The CALLRESULT payload as JObject, or throws on error/timeout.</returns>
    Task<JObject> SendCommandAsync(
        string chargePointId,
        string action,
        JObject payload,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a charge point is currently connected via WebSocket.
    /// </summary>
    bool IsConnected(string chargePointId);

    /// <summary>
    /// Get all currently connected charge point IDs.
    /// </summary>
    IEnumerable<string> GetConnectedChargePoints();
}
