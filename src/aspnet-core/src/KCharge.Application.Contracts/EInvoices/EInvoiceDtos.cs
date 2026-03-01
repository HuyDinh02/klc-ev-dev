using System;
using KCharge.Enums;

namespace KCharge.EInvoices;

public class EInvoiceDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public EInvoiceProvider Provider { get; set; }
    public string? ExternalInvoiceId { get; set; }
    public string? EInvoiceNumber { get; set; }
    public EInvoiceStatus Status { get; set; }
    public string? ViewUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? SignatureHash { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreationTime { get; set; }

    // Related invoice info
    public string? InvoiceNumber { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? StationName { get; set; }
}

public class EInvoiceListDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public EInvoiceProvider Provider { get; set; }
    public string? EInvoiceNumber { get; set; }
    public EInvoiceStatus Status { get; set; }
    public DateTime? IssuedAt { get; set; }
    public int RetryCount { get; set; }
    public decimal? TotalAmount { get; set; }
    public DateTime CreationTime { get; set; }
}

public class EInvoiceDetailDto : EInvoiceDto
{
    // Buyer info
    public string? BuyerName { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }

    // Session info
    public Guid? SessionId { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public DateTime? SessionEndTime { get; set; }
    public decimal? EnergyKwh { get; set; }
    public decimal? RatePerKwh { get; set; }
    public decimal? BaseAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TaxRatePercent { get; set; }
}

public class GetEInvoiceListDto
{
    public EInvoiceStatus? Status { get; set; }
    public EInvoiceProvider? Provider { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public Guid? Cursor { get; set; }
    public int MaxResultCount { get; set; } = 20;
}

public class CreateEInvoiceDto
{
    public Guid InvoiceId { get; set; }
    public EInvoiceProvider Provider { get; set; }
}

public class EInvoiceResultDto
{
    public Guid EInvoiceId { get; set; }
    public EInvoiceStatus Status { get; set; }
    public string? EInvoiceNumber { get; set; }
    public string? ViewUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
