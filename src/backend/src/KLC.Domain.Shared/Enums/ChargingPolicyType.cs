namespace KLC.Enums;

/// <summary>
/// Defines the charging policy enforced on a fleet's vehicles.
/// </summary>
public enum ChargingPolicyType
{
    /// <summary>
    /// No restrictions — vehicles can charge at any time and any station.
    /// </summary>
    AnytimeAnywhere = 0,

    /// <summary>
    /// Vehicles can only charge during defined schedule windows.
    /// </summary>
    ScheduledOnly = 1,

    /// <summary>
    /// Vehicles can only charge at stations belonging to approved station groups.
    /// </summary>
    ApprovedStationsOnly = 2,

    /// <summary>
    /// Vehicles are limited to a daily energy consumption limit (kWh).
    /// </summary>
    DailyEnergyLimit = 3
}
