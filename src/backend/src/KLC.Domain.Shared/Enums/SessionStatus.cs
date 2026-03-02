namespace KLC.Enums;

/// <summary>
/// Represents the status of a charging session.
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// Session has been created but charging has not started.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Remote start command sent, waiting for charger response.
    /// </summary>
    Starting = 1,

    /// <summary>
    /// Charging is in progress.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Charging is suspended (by EV or EVSE).
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Stop command sent, waiting for charger response.
    /// </summary>
    Stopping = 4,

    /// <summary>
    /// Charging completed successfully.
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Session failed or was cancelled.
    /// </summary>
    Failed = 6
}
