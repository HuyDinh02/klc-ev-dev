using Newtonsoft.Json.Linq;

namespace KLC.Ocpp;

/// <summary>
/// Exception thrown when an OCPP CALLERROR response is received.
/// </summary>
public class OcppCallErrorException : Exception
{
    public string ErrorCode { get; }
    public string ErrorDescription { get; }
    public JObject? ErrorDetails { get; }

    public OcppCallErrorException(string errorCode, string errorDescription, JObject? errorDetails = null)
        : base($"OCPP error: {errorCode} - {errorDescription}")
    {
        ErrorCode = errorCode;
        ErrorDescription = errorDescription;
        ErrorDetails = errorDetails;
    }
}
