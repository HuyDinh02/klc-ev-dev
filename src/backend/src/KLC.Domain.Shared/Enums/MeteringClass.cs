namespace KLC.Enums;

/// <summary>
/// IEC 62053 metering accuracy class for energy measurement.
/// </summary>
public enum MeteringClass
{
    /// <summary>
    /// Unknown or not specified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Class B (±1%) — Standard billing accuracy for AC meters.
    /// </summary>
    ClassB = 1,

    /// <summary>
    /// Class A (±2%) — General purpose metering.
    /// </summary>
    ClassA = 2,

    /// <summary>
    /// Class 0.5S (±0.5%) — High accuracy for DC meters.
    /// </summary>
    Class05S = 3,

    /// <summary>
    /// Class 0.2S (±0.2%) — Revenue-grade precision metering.
    /// </summary>
    Class02S = 4
}
