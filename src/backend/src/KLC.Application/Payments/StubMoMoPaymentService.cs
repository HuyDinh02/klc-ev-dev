using System;
using System.Threading.Tasks;
using KLC.Enums;
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

    public StubMoMoPaymentService(ILogger<StubMoMoPaymentService> logger)
    {
        _logger = logger;
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
        _logger.LogInformation("[MoMo] VerifyCallback: Data length={Length}", rawData.Length);

        // Stub always returns success
        return Task.FromResult(new PaymentCallbackResult
        {
            IsValid = true,
            IsSuccess = true,
            ReferenceCode = "STUB_REF",
            GatewayTransactionId = $"MOMO_{Guid.NewGuid():N}"
        });
    }
}
