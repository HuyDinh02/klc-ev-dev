namespace KLC.Enums;

/// <summary>
/// Type of OCPP identification tag.
/// </summary>
public enum IdTagType
{
    /// <summary>
    /// Physical RFID card or key fob.
    /// </summary>
    Rfid = 0,

    /// <summary>
    /// Mobile app-generated token.
    /// </summary>
    Mobile = 1,

    /// <summary>
    /// Virtual/system-assigned tag.
    /// </summary>
    Virtual = 2
}
