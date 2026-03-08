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
/// Stub MoMo payment gateway service for development.
/// Replace with real MoMo API integration for production.
/// </summary>
public class StubMoMoPaymentService : IPaymentGatewayService, ITransientDependency
{
    private readonly ILogger<StubMoMoPaymentService> _logger;
    private readonly IConfiguration _configuration;

    public StubMoMoPaymentService(
        ILogger<StubMoMoPaymentService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public PaymentGateway Gateway => PaymentGateway.MoMo;

    public Task<PaymentGatewayResult> CreateTopUpAsync(CreateTopUpRequest request)
    {
        _logger.LogInformation(
            "[MoMo] CreateTopUp: Ref={ReferenceCode}, Amount={Amount}",
            request.ReferenceCode, request.Amount);

        var fakeRedirectUrl = $"https://test-payment.momo.vn/v2/gateway/pay?ref={request.ReferenceCode}";
        var fakeTxId = $"MOMO_{Guid.NewGuid():N}";

        return Task.FromResult(PaymentGatewayResult.Ok(fakeRedirectUrl, fakeTxId));
    }

    public Task<PaymentCallbackResult> VerifyCallbackAsync(string rawData, string? signature)
    {
        var secretKey = _configuration["Payment:MoMo:SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogWarning(
                "[MoMo] Payment:MoMo:SecretKey is not configured. " +
                "Accepting callback without signature verification (development mode)");

            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = true,
                IsSuccess = true
            });
        }

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("[MoMo] Callback rejected: missing signature");
            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = false,
                ErrorMessage = "Missing signature"
            });
        }

        var expectedSignature = ComputeHmacSha256(rawData, secretKey);

        if (!string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "[MoMo] Callback rejected: signature mismatch for data length={Length}",
                rawData.Length);

            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = false,
                ErrorMessage = "Invalid signature"
            });
        }

        _logger.LogInformation("[MoMo] Callback signature verified successfully");

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
