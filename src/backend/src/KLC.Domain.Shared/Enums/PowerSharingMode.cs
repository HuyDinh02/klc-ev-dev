namespace KLC.Enums;

/// <summary>
/// Power sharing mode for a group of chargers.
/// Based on Chargecore LINK &amp; LOOP technology.
/// </summary>
public enum PowerSharingMode
{
    /// <summary>
    /// LINK mode — Power shared across up to 10 chargers at a single site.
    /// Total site capacity is distributed dynamically among active sessions.
    /// </summary>
    Link = 0,

    /// <summary>
    /// LOOP mode — Power shared across multiple sites in a regional loop.
    /// Enables cross-site load balancing with centralized control.
    /// </summary>
    Loop = 1
}
