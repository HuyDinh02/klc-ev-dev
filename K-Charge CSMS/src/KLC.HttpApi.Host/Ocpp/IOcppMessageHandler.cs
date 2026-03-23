using Newtonsoft.Json.Linq;

namespace KLC.Ocpp;

/// <summary>
/// Handler for processing incoming OCPP messages from charge points.
/// Each CP-initiated action (Authorize, BootNotification, etc.) has a dedicated handler implementation.
/// </summary>
public interface IOcppMessageHandler
{
    /// <summary>
    /// Process an incoming OCPP message and return a response payload.
    /// </summary>
    /// <param name="chargePointId">The charge point identifier sending the message</param>
    /// <param name="payload">The message payload as JObject</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response payload to send back in CALLRESULT</returns>
    Task<JObject> HandleAsync(string chargePointId, JObject payload, CancellationToken cancellationToken);
}
