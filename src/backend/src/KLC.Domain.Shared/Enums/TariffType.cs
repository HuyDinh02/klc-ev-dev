namespace KLC.Enums;

/// <summary>
/// Tariff pricing model type.
/// </summary>
public enum TariffType
{
    /// <summary>
    /// Single flat rate per kWh, regardless of time.
    /// </summary>
    Flat = 0,

    /// <summary>
    /// Time-of-Use pricing with off-peak/normal/peak tiers.
    /// Based on Vietnam EVN 3-tier TOU schedule.
    /// </summary>
    TimeOfUse = 1
}
