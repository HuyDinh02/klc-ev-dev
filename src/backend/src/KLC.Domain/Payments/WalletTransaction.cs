using System;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Payments;

/// <summary>
/// Audit trail for all wallet balance changes.
/// </summary>
public class WalletTransaction : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the AppUser.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Type of wallet transaction.
    /// </summary>
    public WalletTransactionType Type { get; private set; }

    /// <summary>
    /// Transaction amount in VND (positive for credit, negative for debit).
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Wallet balance after this transaction.
    /// </summary>
    public decimal BalanceAfter { get; private set; }

    /// <summary>
    /// Payment gateway used (for top-ups).
    /// </summary>
    public PaymentGateway? PaymentGateway { get; private set; }

    /// <summary>
    /// Transaction ID from external gateway.
    /// </summary>
    public string? GatewayTransactionId { get; private set; }

    /// <summary>
    /// Related charging session (for session payments/refunds).
    /// </summary>
    public Guid? RelatedSessionId { get; private set; }

    /// <summary>
    /// Current transaction status.
    /// </summary>
    public TransactionStatus Status { get; private set; }

    /// <summary>
    /// Description/note for this transaction.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Reference code for tracking.
    /// </summary>
    public string ReferenceCode { get; private set; } = string.Empty;

    protected WalletTransaction()
    {
        // Required by EF Core
    }

    public WalletTransaction(
        Guid id,
        Guid userId,
        WalletTransactionType type,
        decimal amount,
        decimal balanceAfter,
        PaymentGateway? paymentGateway = null,
        string? gatewayTransactionId = null,
        Guid? relatedSessionId = null,
        string? description = null)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Amount = amount;
        BalanceAfter = balanceAfter;
        PaymentGateway = paymentGateway;
        GatewayTransactionId = gatewayTransactionId;
        RelatedSessionId = relatedSessionId;
        Description = description;
        Status = TransactionStatus.Pending;
        ReferenceCode = GenerateReferenceCode();
    }

    private static string GenerateReferenceCode()
    {
        return $"WTX{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }

    public void MarkCompleted(string? gatewayTransactionId = null)
    {
        Status = TransactionStatus.Completed;
        if (gatewayTransactionId != null)
            GatewayTransactionId = gatewayTransactionId;
    }

    public void MarkFailed()
    {
        Status = TransactionStatus.Failed;
    }

    public void MarkCancelled()
    {
        if (Status == TransactionStatus.Completed)
            throw new BusinessException(KLCDomainErrorCodes.Wallet.TransactionAlreadyCompleted);
        Status = TransactionStatus.Cancelled;
    }
}
