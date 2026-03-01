using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace KCharge.Payments;

/// <summary>
/// Represents an invoice for a completed payment.
/// </summary>
public class Invoice : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the payment transaction.
    /// </summary>
    public Guid PaymentTransactionId { get; private set; }

    /// <summary>
    /// Unique invoice number (e.g., "INV-2026-000001").
    /// </summary>
    public string InvoiceNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Energy consumed in kWh.
    /// </summary>
    public decimal EnergyKwh { get; private set; }

    /// <summary>
    /// Base amount before tax in VND.
    /// </summary>
    public decimal BaseAmount { get; private set; }

    /// <summary>
    /// Tax amount in VND.
    /// </summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>
    /// Total amount including tax in VND.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Tax rate percentage applied.
    /// </summary>
    public decimal TaxRatePercent { get; private set; }

    /// <summary>
    /// Rate per kWh applied.
    /// </summary>
    public decimal RatePerKwh { get; private set; }

    /// <summary>
    /// When the invoice was issued.
    /// </summary>
    public DateTime IssuedAt { get; private set; }

    /// <summary>
    /// Navigation property to payment transaction.
    /// </summary>
    public PaymentTransaction? PaymentTransaction { get; private set; }

    /// <summary>
    /// Navigation property to e-invoice.
    /// </summary>
    public EInvoice? EInvoice { get; private set; }

    protected Invoice()
    {
        // Required by EF Core
    }

    public Invoice(
        Guid id,
        Guid paymentTransactionId,
        string invoiceNumber,
        decimal energyKwh,
        decimal ratePerKwh,
        decimal taxRatePercent)
        : base(id)
    {
        PaymentTransactionId = paymentTransactionId;
        InvoiceNumber = invoiceNumber;
        EnergyKwh = energyKwh;
        RatePerKwh = ratePerKwh;
        TaxRatePercent = taxRatePercent;

        // Calculate amounts
        BaseAmount = Math.Round(energyKwh * ratePerKwh, 0);
        TaxAmount = Math.Round(BaseAmount * (taxRatePercent / 100), 0);
        TotalAmount = BaseAmount + TaxAmount;
        IssuedAt = DateTime.UtcNow;
    }

    public static string GenerateInvoiceNumber(int sequenceNumber)
    {
        return $"INV-{DateTime.UtcNow:yyyy}-{sequenceNumber:D6}";
    }
}
