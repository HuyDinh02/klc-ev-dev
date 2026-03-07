namespace KLC.Enums;

/// <summary>
/// Represents the status of a wallet transaction.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction initiated, awaiting gateway callback.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Transaction completed successfully.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Transaction failed.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Transaction cancelled by user or system.
    /// </summary>
    Cancelled = 3
}
