using System;
using KLC.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Payments;

/// <summary>
/// Represents a payment transaction for a charging session.
/// </summary>
public class PaymentTransaction : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the charging session.
    /// </summary>
    public Guid SessionId { get; private set; }

    /// <summary>
    /// Reference to the user who made the payment.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Payment gateway used.
    /// </summary>
    public PaymentGateway Gateway { get; private set; }

    /// <summary>
    /// Amount in VND.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Current payment status.
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// Transaction ID from the payment gateway.
    /// </summary>
    public string? GatewayTransactionId { get; private set; }

    /// <summary>
    /// Reference code for the transaction.
    /// </summary>
    public string? ReferenceCode { get; private set; }

    /// <summary>
    /// Error message if payment failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// When payment was completed.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Navigation property to invoice.
    /// </summary>
    public Invoice? Invoice { get; private set; }

    protected PaymentTransaction()
    {
        // Required by EF Core
    }

    public PaymentTransaction(
        Guid id,
        Guid sessionId,
        Guid userId,
        PaymentGateway gateway,
        decimal amount)
        : base(id)
    {
        SessionId = sessionId;
        UserId = userId;
        Gateway = gateway;
        Amount = amount;
        Status = PaymentStatus.Pending;
        ReferenceCode = GenerateReferenceCode();
    }

    private static string GenerateReferenceCode()
    {
        return $"KC{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }

    public void MarkProcessing(string? gatewayTransactionId = null)
    {
        Status = PaymentStatus.Processing;
        GatewayTransactionId = gatewayTransactionId;
    }

    public void MarkCompleted(string gatewayTransactionId)
    {
        Status = PaymentStatus.Completed;
        GatewayTransactionId = gatewayTransactionId;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = PaymentStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException("Only completed payments can be refunded");
        Status = PaymentStatus.Refunded;
    }

    public void MarkCancelled()
    {
        if (Status == PaymentStatus.Completed)
            throw new InvalidOperationException("Completed payments cannot be cancelled");
        Status = PaymentStatus.Cancelled;
    }

    public void UpdateAmount(decimal amount)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Can only update amount for pending payments");
        Amount = amount;
    }
}
