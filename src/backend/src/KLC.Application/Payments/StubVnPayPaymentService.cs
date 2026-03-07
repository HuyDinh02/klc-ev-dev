using System;
using System.Threading.Tasks;
using KLC.Enums;
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

    public StubVnPayPaymentService(ILogger<StubVnPayPaymentService> logger)
    {
        _logger = logger;
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
        _logger.LogInformation("[VnPay] VerifyCallback: Data length={Length}", rawData.Length);

        // Stub always returns success
        return Task.FromResult(new PaymentCallbackResult
        {
            IsValid = true,
            IsSuccess = true,
            ReferenceCode = "STUB_REF",
            GatewayTransactionId = $"VNPAY_{Guid.NewGuid():N}"
        });
    }
}
