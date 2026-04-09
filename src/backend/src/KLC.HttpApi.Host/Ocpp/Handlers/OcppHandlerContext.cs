using System.Text.Json;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Context passed to individual OCPP action handlers.
/// Contains all data needed to process the message.
/// </summary>
public record OcppHandlerContext
{
    /// <summary>
    /// The active OCPP WebSocket connection.
    /// </summary>
    public required OcppConnection Connection { get; init; }

    /// <summary>
    /// Unique message identifier for request/response correlation.
    /// </summary>
    public required string UniqueId { get; init; }

    /// <summary>
    /// Message payload as a JsonElement for lazy deserialization.
    /// </summary>
    public required JsonElement Payload { get; init; }

    /// <summary>
    /// Protocol-version-aware message parser for serialization.
    /// </summary>
    public required IOcppMessageParser Parser { get; init; }
}
