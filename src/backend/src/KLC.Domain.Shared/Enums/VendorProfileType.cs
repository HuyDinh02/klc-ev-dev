namespace KLC.Enums;

/// <summary>
/// Identifies the vendor profile used for OCPP protocol normalization.
/// Each vendor may have quirks in meter reporting, timestamps, or unit conventions.
/// </summary>
public enum VendorProfileType
{
    /// <summary>Default OCPP 1.6J standard behavior.</summary>
    Generic = 0,

    /// <summary>Chargecore Global chargers (AU/APAC vendor).</summary>
    ChargecoreGlobal = 1,

    /// <summary>JUHANG chargers (Chinese vendor, common in Vietnam).</summary>
    Juhang = 2,

    /// <summary>Custom vendor profile configured per-station.</summary>
    Custom = 99
}
