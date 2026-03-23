namespace KLC.Ocpp;

/// <summary>
/// Exception thrown when attempting to send a command to a charge point that is not connected.
/// </summary>
public class ChargePointNotConnectedException : Exception
{
    public string ChargePointId { get; }

    public ChargePointNotConnectedException(string chargePointId)
        : base($"Charge point '{chargePointId}' is not connected")
    {
        ChargePointId = chargePointId;
    }
}
