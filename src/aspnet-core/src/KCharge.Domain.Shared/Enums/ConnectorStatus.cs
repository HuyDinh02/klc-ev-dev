namespace KCharge.Enums;

/// <summary>
/// Represents the status of a charging connector (OCPP 1.6J ChargePointStatus).
/// </summary>
public enum ConnectorStatus
{
    /// <summary>
    /// Connector is available for a new charging session.
    /// </summary>
    Available = 0,

    /// <summary>
    /// Connector is preparing for charging (cable connected but not yet charging).
    /// </summary>
    Preparing = 1,

    /// <summary>
    /// Connector is actively charging a vehicle.
    /// </summary>
    Charging = 2,

    /// <summary>
    /// Charging is suspended by EV (e.g., battery full).
    /// </summary>
    SuspendedEV = 3,

    /// <summary>
    /// Charging is suspended by EVSE (e.g., max power limit).
    /// </summary>
    SuspendedEVSE = 4,

    /// <summary>
    /// Charging session is finishing (vehicle fully charged).
    /// </summary>
    Finishing = 5,

    /// <summary>
    /// Connector is reserved for a specific user.
    /// </summary>
    Reserved = 6,

    /// <summary>
    /// Connector is unavailable (out of service).
    /// </summary>
    Unavailable = 7,

    /// <summary>
    /// Connector has a fault.
    /// </summary>
    Faulted = 8
}
