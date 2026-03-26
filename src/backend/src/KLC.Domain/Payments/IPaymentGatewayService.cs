using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Enums;

namespace KLC.Payments;

/// <summary>
/// Interface for processing payments through external payment gateways.
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// The payment gateway this service handles.
    /// </summary>
    PaymentGateway Gateway { get; }

    /// <summary>
    /// Create a top-up payment request and return a redirect URL.
    /// </summary>
    Task<PaymentGatewayResult> CreateTopUpAsync(CreateTopUpRequest request);

    /// <summary>
    /// Verify a callback/webhook from the payment gateway.
    /// </summary>
    Task<PaymentCallbackResult> VerifyCallbackAsync(string rawData, string? signature);

    /// <summary>
    /// Verify the HMAC signature of a payment gateway callback.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    /// <param name="parameters">The callback parameters (excluding signature field).</param>
    /// <param name="signature">The signature provided by the gateway.</param>
    /// <returns>True if the signature is valid.</returns>
    bool VerifyCallbackSignature(Dictionary<string, string> parameters, string signature);

    /// <summary>
    /// Query transaction status from the payment gateway (server-to-server).
    /// </summary>
    Task<PaymentCallbackResult> QueryTransactionAsync(QueryTransactionRequest request)
        => Task.FromResult(new PaymentCallbackResult { IsValid = false, ErrorMessage = "Not supported" });

    /// <summary>
    /// Request a refund through the payment gateway.
    /// </summary>
    Task<PaymentGatewayResult> RefundAsync(RefundGatewayRequest request)
        => Task.FromResult(PaymentGatewayResult.Fail("Refund not supported"));
}

public class CreateTopUpRequest
{
    public string ReferenceCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public string? ClientIpAddress { get; set; }
    public string? BankCode { get; set; }
}

public class PaymentGatewayResult
{
    public bool Success { get; set; }
    public string? RedirectUrl { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? ErrorMessage { get; set; }

    public static PaymentGatewayResult Ok(string redirectUrl, string? gatewayTxId = null)
        => new() { Success = true, RedirectUrl = redirectUrl, GatewayTransactionId = gatewayTxId };

    public static PaymentGatewayResult Fail(string error)
        => new() { Success = false, ErrorMessage = error };
}

public class PaymentCallbackResult
{
    public bool IsValid { get; set; }
    public bool IsSuccess { get; set; }
    public string? ReferenceCode { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Amount reported by the gateway (converted to VND). Used for amount verification.
    /// </summary>
    public decimal? CallbackAmount { get; set; }
}

public class QueryTransactionRequest
{
    public string TxnRef { get; set; } = string.Empty;
    /// <summary>Original transaction date in yyyyMMddHHmmss format (GMT+7).</summary>
    public string TransactionDate { get; set; } = string.Empty;
    public string? GatewayTransactionId { get; set; }
    public string ClientIpAddress { get; set; } = "127.0.0.1";
    public string OrderInfo { get; set; } = string.Empty;
}

public class RefundGatewayRequest
{
    public string TxnRef { get; set; } = string.Empty;
    public string GatewayTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>Original transaction date in yyyyMMddHHmmss format (GMT+7).</summary>
    public string TransactionDate { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string ClientIpAddress { get; set; } = "127.0.0.1";
    public string OrderInfo { get; set; } = string.Empty;
    public bool IsFullRefund { get; set; } = true;
}
