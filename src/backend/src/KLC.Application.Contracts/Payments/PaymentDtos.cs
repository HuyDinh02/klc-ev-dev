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
    public string? UserName { get; set; }
}

public class ProcessPaymentDto
{
    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public PaymentGateway Gateway { get; set; }

    public Guid? PaymentMethodId { get; set; }

    /// <summary>
    /// Client IP address for payment gateway (set by controller, not by client).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ClientIpAddress { get; set; }
}

/// <summary>
/// VNPay IPN response format. VNPay expects exactly this JSON shape.
/// </summary>
public class VnPayIpnResponse
{
    public string RspCode { get; set; } = "99";
    public string Message { get; set; } = "Unknown error";

    public static VnPayIpnResponse Success() => new() { RspCode = "00", Message = "Confirm Success" };
    public static VnPayIpnResponse OrderNotFound() => new() { RspCode = "01", Message = "Order not found" };
    public static VnPayIpnResponse AlreadyConfirmed() => new() { RspCode = "02", Message = "Order already confirmed" };
    public static VnPayIpnResponse InvalidAmount() => new() { RspCode = "04", Message = "Invalid amount" };
    public static VnPayIpnResponse InvalidSignature() => new() { RspCode = "97", Message = "Invalid signature" };
    public static VnPayIpnResponse UnknownError() => new() { RspCode = "99", Message = "Unknown error" };
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

    [Range(1, 100)]
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

// Refund DTOs
public class RefundInput
{
    public string? Reason { get; set; }
}

public class RefundResultDto
{
    public Guid PaymentId { get; set; }
    public Guid WalletTransactionId { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal NewWalletBalance { get; set; }
    public PaymentStatus NewStatus { get; set; }
}

// Callback DTOs
public class PaymentCallbackDto
{
    public string? TransactionId { get; set; }

    [Required]
    public string ReferenceCode { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }
    public string? Signature { get; set; }
    public string? RawData { get; set; }
}
