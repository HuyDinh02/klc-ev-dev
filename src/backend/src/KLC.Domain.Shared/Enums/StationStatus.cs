namespace KLC.Enums;

/// <summary>
/// Represents the operational status of a charging station.
///
/// Lifecycle:
///   Created → Offline → Online (automatic via OCPP)
///   Admin Disable → Disabled (no charging, can re-enable)
///   Admin Delete → IsDeleted=true (soft delete, data preserved)
///
/// Only 3 active states. Deletion uses ABP ISoftDelete, not a status.
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
    /// [Deprecated] Use Disabled + soft-delete instead.
    /// Kept for backward compatibility with existing DB records.
    /// </summary>
    Decommissioned = 3
}
