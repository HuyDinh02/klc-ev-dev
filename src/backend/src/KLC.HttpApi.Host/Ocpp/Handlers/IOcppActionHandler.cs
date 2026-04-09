using System.Threading.Tasks;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Interface for individual OCPP action handlers (Strategy Pattern).
/// Each handler processes a specific OCPP action (e.g., BootNotification, Heartbeat).
/// </summary>
public interface IOcppActionHandler
{
    /// <summary>
    /// The OCPP action name this handler processes (e.g., "BootNotification").
    /// </summary>
    string Action { get; }

    /// <summary>
    /// Handle the OCPP action and return the serialized response.
    /// </summary>
    Task<string> HandleAsync(OcppHandlerContext context);
}
