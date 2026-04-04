using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using Microsoft.Extensions.Logging;

namespace KLC.Payments;

/// <summary>
/// Shared payment callback verification logic.
/// Eliminates 3x duplication across PaymentAppService and WalletBffService.
/// </summary>
public interface IPaymentCallbackValidator
{
    /// <summary>
    /// Verify a VnPay IPN callback: validate signature, extract txnRef and amount.
    /// </summary>
    Task<VnPayCallbackValidation> ValidateVnPayIpnAsync(
        Dictionary<string, string> queryParams,
        decimal? expectedAmount = null);

    /// <summary>
    /// Verify a generic gateway callback: validate signature and optionally verify amount.
    /// </summary>
    Task<GatewayCallbackValidation> ValidateGatewayCallbackAsync(
        PaymentGateway gateway,
        string rawData,
        string? signature,
        decimal? expectedAmount = null);
}

public record VnPayCallbackValidation(
    bool IsValid,
    string? TxnRef,
    string? ResponseCode,
    decimal? Amount,
    string? GatewayTransactionId,
    string? ErrorMessage)
{
    public bool IsPaymentSuccess => ResponseCode == "00";

    public static VnPayCallbackValidation Invalid(string error)
        => new(false, null, null, null, null, error);

    public static VnPayCallbackValidation Valid(
        string txnRef, string responseCode, decimal? amount, string? gatewayTxnId)
        => new(true, txnRef, responseCode, amount, gatewayTxnId, null);
}

public record GatewayCallbackValidation(
    bool IsValid,
    decimal? CallbackAmount,
    string? GatewayTransactionId,
    string? ErrorMessage);

public class PaymentCallbackValidator : IPaymentCallbackValidator, Volo.Abp.DependencyInjection.ITransientDependency
{
    private readonly IEnumerable<IPaymentGatewayService> _gateways;
    private readonly ILogger<PaymentCallbackValidator> _logger;

    public PaymentCallbackValidator(
        IEnumerable<IPaymentGatewayService> gateways,
        ILogger<PaymentCallbackValidator> logger)
    {
        _gateways = gateways;
        _logger = logger;
    }

    public async Task<VnPayCallbackValidation> ValidateVnPayIpnAsync(
        Dictionary<string, string> queryParams,
        decimal? expectedAmount = null)
    {
        var rawData = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var vnpayGateway = _gateways.FirstOrDefault(g => g.Gateway == PaymentGateway.VnPay);
        if (vnpayGateway == null)
        {
            _logger.LogWarning("[VnPay IPN] VnPay gateway service not registered");
            return VnPayCallbackValidation.Invalid("VnPay gateway not registered");
        }

        var verifyResult = await vnpayGateway.VerifyCallbackAsync(rawData, null);
        if (!verifyResult.IsValid)
        {
            _logger.LogWarning("[VnPay IPN] Invalid signature");
            return VnPayCallbackValidation.Invalid("Invalid signature");
        }

        if (expectedAmount.HasValue && verifyResult.CallbackAmount.HasValue
            && verifyResult.CallbackAmount.Value != expectedAmount.Value)
        {
            _logger.LogWarning(
                "[VnPay IPN] Amount mismatch: Expected={Expected}, Got={Got}",
                expectedAmount.Value, verifyResult.CallbackAmount.Value);
            return VnPayCallbackValidation.Invalid("Amount mismatch");
        }

        var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef");
        var responseCode = queryParams.GetValueOrDefault("vnp_ResponseCode");

        return VnPayCallbackValidation.Valid(
            txnRef ?? "",
            responseCode ?? "",
            verifyResult.CallbackAmount,
            verifyResult.GatewayTransactionId);
    }

    public async Task<GatewayCallbackValidation> ValidateGatewayCallbackAsync(
        PaymentGateway gateway,
        string rawData,
        string? signature,
        decimal? expectedAmount = null)
    {
        var gatewayService = _gateways.FirstOrDefault(g => g.Gateway == gateway);
        if (gatewayService == null)
        {
            _logger.LogWarning("No gateway service found for {Gateway}", gateway);
            return new GatewayCallbackValidation(true, null, null, null); // Skip verification
        }

        var verifyResult = await gatewayService.VerifyCallbackAsync(rawData, signature);
        if (!verifyResult.IsValid)
        {
            return new GatewayCallbackValidation(false, null, null, verifyResult.ErrorMessage);
        }

        if (expectedAmount.HasValue && verifyResult.CallbackAmount.HasValue
            && verifyResult.CallbackAmount.Value != expectedAmount.Value)
        {
            return new GatewayCallbackValidation(false, verifyResult.CallbackAmount,
                verifyResult.GatewayTransactionId, "Amount mismatch");
        }

        return new GatewayCallbackValidation(true, verifyResult.CallbackAmount,
            verifyResult.GatewayTransactionId, null);
    }
}
