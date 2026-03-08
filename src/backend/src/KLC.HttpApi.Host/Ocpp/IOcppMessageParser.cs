using System.Text.Json;
using KLC.Enums;

namespace KLC.Ocpp;

/// <summary>
/// Parses raw OCPP WebSocket messages into a protocol-independent structure.
/// Implementations handle version-specific framing (1.6J JSON array vs 2.0.1 JSON).
/// </summary>
public interface IOcppMessageParser
{
    /// <summary>
    /// The OCPP protocol version this parser handles.
    /// </summary>
    OcppProtocolVersion Version { get; }

    /// <summary>
    /// Parse a raw WebSocket message into a structured OCPP message.
    /// Returns null if the message is malformed.
    /// </summary>
    ParsedOcppMessage? Parse(string rawMessage);

    /// <summary>
    /// Serialize a CallResult response back to wire format.
    /// </summary>
    string SerializeCallResult(string uniqueId, object payload);

    /// <summary>
    /// Serialize a CallError response back to wire format.
    /// </summary>
    string SerializeCallError(string uniqueId, string errorCode, string errorDescription);

    /// <summary>
    /// Serialize a Call request (CSMS → Charge Point) back to wire format.
    /// </summary>
    string SerializeCall(string uniqueId, string action, object payload);
}

/// <summary>
/// Protocol-independent representation of a parsed OCPP message.
/// </summary>
public class ParsedOcppMessage
{
    /// <summary>
    /// Message type: 2 = Call, 3 = CallResult, 4 = CallError.
    /// </summary>
    public required int MessageType { get; init; }

    /// <summary>
    /// Unique message identifier for request/response correlation.
    /// </summary>
    public required string UniqueId { get; init; }

    /// <summary>
    /// Action name (only for Call messages, e.g., "BootNotification").
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Message payload as a JsonElement for lazy deserialization.
    /// </summary>
    public JsonElement Payload { get; init; }

    /// <summary>
    /// Error code (only for CallError messages).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error description (only for CallError messages).
    /// </summary>
    public string? ErrorDescription { get; init; }
}
