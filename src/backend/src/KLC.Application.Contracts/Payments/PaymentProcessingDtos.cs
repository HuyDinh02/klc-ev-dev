using System;
using KLC.Enums;

namespace KLC.Payments;

/// <summary>
/// Input for processing a session payment via the Application layer.
/// </summary>
public class ProcessSessionPaymentInput
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public PaymentGateway Gateway { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public string? VoucherCode { get; set; }
    public string? ClientIpAddress { get; set; }
}

/// <summary>
/// Result of processing a session payment. Contains all data the BFF needs
/// for cache invalidation and mobile API response mapping.
/// </summary>
public class SessionPaymentResultDto
{
    public bool Success { get; set; }
    public Guid? PaymentId { get; set; }
    public PaymentStatus? Status { get; set; }
    public string? RedirectUrl { get; set; }
    public decimal? VoucherDiscount { get; set; }
    public string? Error { get; set; }
}
