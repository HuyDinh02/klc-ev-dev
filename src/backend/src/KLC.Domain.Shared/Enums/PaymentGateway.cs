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
    Wallet = 3,

    /// <summary>
    /// VnPay payment gateway.
    /// </summary>
    VnPay = 4,

    /// <summary>
    /// QR code payment.
    /// </summary>
    QrPayment = 5,

    /// <summary>
    /// Voucher/coupon code payment.
    /// </summary>
    Voucher = 6,

    /// <summary>
    /// Urbox gift card payment.
    /// </summary>
    Urbox = 7
}
