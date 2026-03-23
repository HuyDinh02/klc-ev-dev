namespace KLC.Enums;

/// <summary>
/// Represents the operational status of a charging station.
/// Simplified to 4 states — connector-level detail comes from ConnectorStatus.
/// </summary>
public enum StationStatus
{
    /// <summary>
    /// Station is offline (WebSocket disconnected, heartbeat timeout).
    /// Auto-set by system. Recovers automatically on reconnect.
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Station is online (WebSocket connected, BootNotification accepted).
    /// Auto-set by system on successful connection.
    /// </summary>
    Online = 1,

    /// <summary>
    /// Station is disabled by admin. No charging allowed.
    /// Manually set by admin. Requires admin to re-enable.
    /// </summary>
    Disabled = 2,

    /// <summary>
    /// Station has been decommissioned and retired from service.
    /// Terminal state — cannot be re-enabled.
    /// </summary>
    Decommissioned = 3
}
