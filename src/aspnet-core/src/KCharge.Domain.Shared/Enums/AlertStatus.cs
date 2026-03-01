namespace KCharge.Enums;

/// <summary>
/// Represents the status of an operational alert.
/// </summary>
public enum AlertStatus
{
    /// <summary>
    /// Alert is new and not yet acknowledged.
    /// </summary>
    New = 0,

    /// <summary>
    /// Alert has been acknowledged by an operator.
    /// </summary>
    Acknowledged = 1,

    /// <summary>
    /// Alert has been resolved.
    /// </summary>
    Resolved = 2
}
