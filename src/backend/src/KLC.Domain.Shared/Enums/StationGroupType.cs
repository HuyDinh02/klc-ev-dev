namespace KLC.Enums;

/// <summary>
/// Classification of station groups by purpose.
/// </summary>
public enum StationGroupType
{
    /// <summary>Geographic grouping (region, province, district).</summary>
    Geographic = 0,

    /// <summary>Operational grouping (maintenance team, equipment vendor).</summary>
    Operational = 1,

    /// <summary>Business grouping (franchise partner, pricing zone).</summary>
    Business = 2,

    /// <summary>Custom/user-defined grouping.</summary>
    Custom = 3
}
