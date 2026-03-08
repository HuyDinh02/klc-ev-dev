using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using KLC.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace KLC.Payments;

/// <summary>
/// Stub VnPay payment gateway service for development.
/// Replace with real VnPay API integration for production.
/// </summary>
public class StubVnPayPaymentService : IPaymentGatewayService, ITransientDependency
{
    private readonly ILogger<StubVnPayPaymentService> _logger;
    private readonly IConfiguration _configuration;

    public StubVnPayPaymentService(
        ILogger<StubVnPayPaymentService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public PaymentGateway Gateway => PaymentGateway.VnPay;

    public Task<PaymentGatewayResult> CreateTopUpAsync(CreateTopUpRequest request)
    {
        _logger.LogInformation(
            "[VnPay] CreateTopUp: Ref={ReferenceCode}, Amount={Amount}",
            request.ReferenceCode, request.Amount);

        var fakeRedirectUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?ref={request.ReferenceCode}";
        var fakeTxId = $"VNPAY_{Guid.NewGuid():N}";

        return Task.FromResult(PaymentGatewayResult.Ok(fakeRedirectUrl, fakeTxId));
    }

    public Task<PaymentCallbackResult> VerifyCallbackAsync(string rawData, string? signature)
    {
        var hashSecret = _configuration["Payment:VnPay:HashSecret"];

        if (string.IsNullOrEmpty(hashSecret))
        {
            _logger.LogWarning(
                "[VnPay] Payment:VnPay:HashSecret is not configured. " +
                "Accepting callback without signature verification (development mode)");

            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = true,
                IsSuccess = true
            });
        }

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("[VnPay] Callback rejected: missing signature");
            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = false,
                ErrorMessage = "Missing signature"
            });
        }

        var expectedSignature = ComputeHmacSha256(rawData, hashSecret);

        if (!string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "[VnPay] Callback rejected: signature mismatch for data length={Length}",
                rawData.Length);

            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = false,
                ErrorMessage = "Invalid signature"
            });
        }

        _logger.LogInformation("[VnPay] Callback signature verified successfully");

        return Task.FromResult(new PaymentCallbackResult
        {
            IsValid = true,
            IsSuccess = true
        });
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
