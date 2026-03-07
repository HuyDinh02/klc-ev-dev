namespace KLC.Enums;

/// <summary>
/// Represents the type of wallet transaction.
/// </summary>
public enum WalletTransactionType
{
    /// <summary>
    /// Top-up to wallet from payment gateway.
    /// </summary>
    TopUp = 0,

    /// <summary>
    /// Payment for a charging session.
    /// </summary>
    SessionPayment = 1,

    /// <summary>
    /// Refund from a cancelled/failed session.
    /// </summary>
    Refund = 2,

    /// <summary>
    /// Manual adjustment by admin.
    /// </summary>
    Adjustment = 3,

    /// <summary>
    /// Credit from voucher redemption.
    /// </summary>
    VoucherCredit = 4
}
