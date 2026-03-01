namespace KCharge.Enums;

/// <summary>
/// Represents the status of a payment transaction.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending initiation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment is being processed by the gateway.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Payment was successful.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Payment failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Payment was refunded.
    /// </summary>
    Refunded = 4,

    /// <summary>
    /// Payment was cancelled.
    /// </summary>
    Cancelled = 5
}
