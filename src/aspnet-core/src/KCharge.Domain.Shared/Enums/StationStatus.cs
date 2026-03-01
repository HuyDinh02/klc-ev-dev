namespace KCharge.Enums;

/// <summary>
/// Represents the operational status of a charging station.
/// </summary>
public enum StationStatus
{
    /// <summary>
    /// Station is offline and not communicating with CSMS.
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Station is online and available for charging.
    /// </summary>
    Available = 1,

    /// <summary>
    /// Station is online but at least one connector is occupied.
    /// </summary>
    Occupied = 2,

    /// <summary>
    /// Station is online but temporarily unavailable (maintenance, reserved, etc.).
    /// </summary>
    Unavailable = 3,

    /// <summary>
    /// Station has a fault and cannot be used.
    /// </summary>
    Faulted = 4
}
