namespace KLC.Enums;

/// <summary>
/// Represents the type of voucher discount.
/// </summary>
public enum VoucherType
{
    /// <summary>
    /// Fixed amount discount in VND.
    /// </summary>
    FixedAmount = 0,

    /// <summary>
    /// Percentage discount.
    /// </summary>
    Percentage = 1,

    /// <summary>
    /// Free charging session.
    /// </summary>
    FreeCharging = 2
}
