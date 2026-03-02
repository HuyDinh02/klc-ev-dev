namespace KLC.Enums;

/// <summary>
/// Represents the type of charging connector.
/// </summary>
public enum ConnectorType
{
    /// <summary>
    /// Type 2 connector (IEC 62196-2) - AC charging, common in Europe/Asia.
    /// </summary>
    Type2 = 0,

    /// <summary>
    /// CCS Combo 2 connector (Combined Charging System) - DC fast charging.
    /// </summary>
    CCS2 = 1,

    /// <summary>
    /// CHAdeMO connector - DC fast charging, common for Japanese EVs.
    /// </summary>
    CHAdeMO = 2,

    /// <summary>
    /// GB/T connector - Chinese standard for AC and DC charging.
    /// </summary>
    GBT = 3,

    /// <summary>
    /// Type 1 connector (SAE J1772) - AC charging, common in North America.
    /// </summary>
    Type1 = 4
}
