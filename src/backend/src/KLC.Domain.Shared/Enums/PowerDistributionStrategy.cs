namespace KLC.Enums;

/// <summary>
/// Strategy for distributing available power among active charging sessions.
/// </summary>
public enum PowerDistributionStrategy
{
    /// <summary>
    /// Equal distribution — Available power split evenly across all active connectors.
    /// </summary>
    Average = 0,

    /// <summary>
    /// Proportional distribution — Power allocated based on connector max capacity ratios.
    /// </summary>
    Proportional = 1,

    /// <summary>
    /// Dynamic distribution — Priority-based allocation considering session duration,
    /// SoC targets, and user membership tier.
    /// </summary>
    Dynamic = 2
}
