namespace KLC.Enums;

/// <summary>
/// Represents the status of a fault/error reported by a charging station.
/// </summary>
public enum FaultStatus
{
    /// <summary>
    /// Fault is newly reported and not yet addressed.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Fault is being investigated by operations team.
    /// </summary>
    Investigating = 1,

    /// <summary>
    /// Fault has been resolved.
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// Fault was closed without resolution (e.g., false alarm).
    /// </summary>
    Closed = 3
}
