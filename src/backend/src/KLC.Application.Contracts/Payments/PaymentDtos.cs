using System;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;

namespace KLC.Payments;

// Payment Transaction DTOs
public class PaymentTransactionDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public PaymentGateway Gateway { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? ReferenceCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StationName { get; set; }
    public decimal? EnergyKwh { get; set; }
}

public class PaymentListDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public PaymentGateway Gateway { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ReferenceCode { get; set; }
    public DateTime CreationTime { get; set; }
    public string? StationName { get; set; }
}

public class ProcessPaymentDto
{
    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public PaymentGateway Gateway { get; set; }

    public Guid? PaymentMethodId { get; set; }
}

public class PaymentResultDto
{
    public Guid PaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? GatewayRedirectUrl { get; set; }
    public string? ReferenceCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class GetPaymentListDto
{
    public PaymentStatus? Status { get; set; }
    public PaymentGateway? Gateway { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? Cursor { get; set; }
    public int MaxResultCount { get; set; } = 20;
}

// Payment Method DTOs
public class PaymentMethodDto
{
    public Guid Id { get; set; }
    public PaymentGateway Gateway { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? LastFourDigits { get; set; }
    public DateTime CreationTime { get; set; }
}

public class CreatePaymentMethodDto
{
    [Required]
    public PaymentGateway Gateway { get; set; }

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string TokenReference { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

// Invoice DTOs
public class InvoiceDto
{
    public Guid Id { get; set; }
    public Guid PaymentTransactionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal EnergyKwh { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal RatePerKwh { get; set; }
    public DateTime IssuedAt { get; set; }
    public string? StationName { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public DateTime? SessionEndTime { get; set; }
}

// Callback DTOs
public class PaymentCallbackDto
{
    public string? TransactionId { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? Signature { get; set; }
    public string? RawData { get; set; }
}
