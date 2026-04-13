using System;
using KLC.Enums;

namespace KLC.Payments;

/// <summary>
/// Request to initiate a wallet top-up via a payment gateway.
/// </summary>
public class InitiateTopUpInput
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public PaymentGateway Gateway { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? BankCode { get; set; }
}

/// <summary>
/// Result of a top-up initiation. Contains the redirect URL for the payment gateway.
/// </summary>
public class TopUpInitiationResultDto
{
    public bool Success { get; set; }
    public Guid? TransactionId { get; set; }
    public string? ReferenceCode { get; set; }
    public string? RedirectUrl { get; set; }
    public TransactionStatus? Status { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of processing a top-up callback (generic gateway callback or VnPay IPN).
/// Contains completion data the BFF needs for notifications.
/// </summary>
public class TopUpCompletionResultDto
{
    public bool Success { get; set; }
    public Guid? TransactionId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public decimal NewBalance { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Input for processing a generic top-up callback from a payment gateway.
/// </summary>
public class ProcessTopUpCallbackInput
{
    public string ReferenceCode { get; set; } = string.Empty;
    public string? GatewayTransactionId { get; set; }
    public TransactionStatus Status { get; set; }
    public PaymentGateway? Gateway { get; set; }
}

/// <summary>
/// Result of a generic top-up callback processing.
/// </summary>
public class TopUpCallbackResultAppDto
{
    public bool Success { get; set; }
    public Guid? TransactionId { get; set; }
    public decimal? NewBalance { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of VnPay IPN processing. Includes the IPN response and optional completion data.
/// </summary>
public class VnPayIpnProcessingResult
{
    /// <summary>
    /// The VnPay IPN response to return to VnPay servers.
    /// </summary>
    public VnPayIpnResponse IpnResponse { get; set; } = VnPayIpnResponse.UnknownError();

    /// <summary>
    /// If the IPN resulted in a successful wallet credit, this contains the completion data.
    /// Null if the IPN was a validation error, duplicate, or payment failure.
    /// </summary>
    public TopUpCompletionResultDto? Completion { get; set; }

    /// <summary>
    /// If the IPN resulted in a failed payment, this contains the failure data for notifications.
    /// </summary>
    public VnPayFailureInfo? Failure { get; set; }
}

/// <summary>
/// Information about a failed VnPay payment, for the BFF to send failure notifications.
/// </summary>
public class VnPayFailureInfo
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public string? ResponseCode { get; set; }
}
