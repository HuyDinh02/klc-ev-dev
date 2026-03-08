using System.Text.Json;
using KLC.Enums;
using KLC.Ocpp.Messages;

namespace KLC.Ocpp;

/// <summary>
/// OCPP 1.6J message parser.
/// Wire format: JSON array [MessageType, UniqueId, Action/Payload...]
/// </summary>
public class OcppV16MessageParser : IOcppMessageParser
{
    public OcppProtocolVersion Version => OcppProtocolVersion.Ocpp16J;

    public ParsedOcppMessage? Parse(string rawMessage)
    {
        var jsonArray = JsonSerializer.Deserialize<JsonElement[]>(rawMessage);
        if (jsonArray == null || jsonArray.Length < 3)
            return null;

        var messageType = jsonArray[0].GetInt32();
        var uniqueId = jsonArray[1].GetString() ?? string.Empty;

        return messageType switch
        {
            OcppMessageType.Call => new ParsedOcppMessage
            {
                MessageType = messageType,
                UniqueId = uniqueId,
                Action = jsonArray[2].GetString() ?? string.Empty,
                Payload = jsonArray.Length > 3 ? jsonArray[3] : default
            },
            OcppMessageType.CallResult => new ParsedOcppMessage
            {
                MessageType = messageType,
                UniqueId = uniqueId,
                Payload = jsonArray.Length > 2 ? jsonArray[2] : default
            },
            OcppMessageType.CallError => new ParsedOcppMessage
            {
                MessageType = messageType,
                UniqueId = uniqueId,
                ErrorCode = jsonArray.Length > 2 ? jsonArray[2].GetString() : "Unknown",
                ErrorDescription = jsonArray.Length > 3 ? jsonArray[3].GetString() : ""
            },
            _ => null
        };
    }

    public string SerializeCallResult(string uniqueId, object payload)
    {
        var response = new object[] { OcppMessageType.CallResult, uniqueId, payload };
        return JsonSerializer.Serialize(response);
    }

    public string SerializeCallError(string uniqueId, string errorCode, string errorDescription)
    {
        var response = new object[] { OcppMessageType.CallError, uniqueId, errorCode, errorDescription, new { } };
        return JsonSerializer.Serialize(response);
    }

    public string SerializeCall(string uniqueId, string action, object payload)
    {
        var message = new object[] { OcppMessageType.Call, uniqueId, action, payload };
        return JsonSerializer.Serialize(message);
    }
}
