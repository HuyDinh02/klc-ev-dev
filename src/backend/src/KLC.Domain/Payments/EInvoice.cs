using System;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Payments;

/// <summary>
/// Represents an electronic invoice for Vietnamese tax compliance.
/// </summary>
public class EInvoice : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the invoice.
    /// </summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>
    /// E-invoice provider (MISA, Viettel, VNPT).
    /// </summary>
    public EInvoiceProvider Provider { get; private set; }

    /// <summary>
    /// External invoice ID from the provider.
    /// </summary>
    public string? ExternalInvoiceId { get; private set; }

    /// <summary>
    /// E-invoice number from the provider.
    /// </summary>
    public string? EInvoiceNumber { get; private set; }

    /// <summary>
    /// Current status.
    /// </summary>
    public EInvoiceStatus Status { get; private set; }

    /// <summary>
    /// URL to view/download the e-invoice.
    /// </summary>
    public string? ViewUrl { get; private set; }

    /// <summary>
    /// URL to download the PDF version.
    /// </summary>
    public string? PdfUrl { get; private set; }

    /// <summary>
    /// Digital signature hash.
    /// </summary>
    public string? SignatureHash { get; private set; }

    /// <summary>
    /// When the e-invoice was issued.
    /// </summary>
    public DateTime? IssuedAt { get; private set; }

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Navigation property to invoice.
    /// </summary>
    public Invoice? Invoice { get; private set; }

    protected EInvoice()
    {
        // Required by EF Core
    }

    public EInvoice(
        Guid id,
        Guid invoiceId,
        EInvoiceProvider provider)
        : base(id)
    {
        InvoiceId = invoiceId;
        Provider = provider;
        Status = EInvoiceStatus.Pending;
        RetryCount = 0;
    }

    public void MarkProcessing()
    {
        Status = EInvoiceStatus.Processing;
    }

    public void MarkIssued(
        string externalInvoiceId,
        string eInvoiceNumber,
        string? viewUrl = null,
        string? pdfUrl = null,
        string? signatureHash = null)
    {
        ExternalInvoiceId = externalInvoiceId;
        EInvoiceNumber = eInvoiceNumber;
        ViewUrl = viewUrl;
        PdfUrl = pdfUrl;
        SignatureHash = signatureHash;
        IssuedAt = DateTime.UtcNow;
        Status = EInvoiceStatus.Issued;
    }

    public void MarkFailed(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = EInvoiceStatus.Failed;
        RetryCount++;
    }

    public void MarkCancelled()
    {
        Status = EInvoiceStatus.Cancelled;
    }

    public bool CanRetry()
    {
        return Status == EInvoiceStatus.Failed && RetryCount < 3;
    }

    public void ResetForRetry()
    {
        if (!CanRetry())
            throw new BusinessException(KLCDomainErrorCodes.EInvoiceCannotRetry)
                .WithData("RetryCount", RetryCount);
        Status = EInvoiceStatus.Pending;
        ErrorMessage = null;
    }
}
