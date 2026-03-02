namespace KLC.Enums;

/// <summary>
/// Represents the payment gateway used for transactions.
/// </summary>
public enum PaymentGateway
{
    /// <summary>
    /// ZaloPay payment gateway.
    /// </summary>
    ZaloPay = 0,

    /// <summary>
    /// MoMo payment gateway.
    /// </summary>
    MoMo = 1,

    /// <summary>
    /// OnePay payment gateway.
    /// </summary>
    OnePay = 2,

    /// <summary>
    /// Wallet balance (prepaid).
    /// </summary>
    Wallet = 3
}
