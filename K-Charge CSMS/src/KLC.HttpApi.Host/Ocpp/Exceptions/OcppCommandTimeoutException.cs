namespace KLC.Ocpp;

/// <summary>
/// Exception thrown when an OCPP command times out while waiting for a response.
/// </summary>
public class OcppCommandTimeoutException : Exception
{
    public string ChargePointId { get; }
    public string Action { get; }
    public TimeSpan Timeout { get; }

    public OcppCommandTimeoutException(string chargePointId, string action, TimeSpan timeout)
        : base($"OCPP command '{action}' to charge point '{chargePointId}' timed out after {timeout.TotalSeconds}s")
    {
        ChargePointId = chargePointId;
        Action = action;
        Timeout = timeout;
    }
}
