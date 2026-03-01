namespace KCharge.Enums;

/// <summary>
/// Represents the type of user notification.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Charging session started.
    /// </summary>
    ChargingStarted = 0,

    /// <summary>
    /// Charging session completed.
    /// </summary>
    ChargingCompleted = 1,

    /// <summary>
    /// Charging session failed.
    /// </summary>
    ChargingFailed = 2,

    /// <summary>
    /// Payment successful.
    /// </summary>
    PaymentSuccess = 3,

    /// <summary>
    /// Payment failed.
    /// </summary>
    PaymentFailed = 4,

    /// <summary>
    /// E-invoice ready.
    /// </summary>
    EInvoiceReady = 5,

    /// <summary>
    /// Wallet top-up successful.
    /// </summary>
    WalletTopUp = 6,

    /// <summary>
    /// Promotional message.
    /// </summary>
    Promotion = 7,

    /// <summary>
    /// System announcement.
    /// </summary>
    SystemAnnouncement = 8
}
