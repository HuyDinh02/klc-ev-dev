using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using KLC.Configuration;
using KLC.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace KLC.Payments;

public class MoMoPaymentServiceTests
{
    private readonly ILogger<MoMoPaymentService> _logger = NullLogger<MoMoPaymentService>.Instance;

    [Fact]
    public async Task CreateTopUp_Without_Credentials_Returns_Stub_Url()
    {
        var config = BuildConfig(new() { { "Payment:MoMo:PartnerCode", "" } });
        var factory = new StubHttpClientFactory();
        var svc = CreateMoMoService(config, factory);

        var result = await svc.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF001",
            Amount = 50_000,
            Description = "Test",
            ReturnUrl = "https://example.com/return",
            NotifyUrl = "https://example.com/notify"
        });

        result.Success.ShouldBeTrue();
        result.RedirectUrl.ShouldContain("test-payment.momo.vn");
        result.GatewayTransactionId.ShouldStartWith("MOMO_");
    }

    [Fact]
    public async Task CreateTopUp_With_Credentials_Calls_MoMo_API()
    {
        var config = BuildConfig(new()
        {
            { "Payment:MoMo:PartnerCode", "PARTNER001" },
            { "Payment:MoMo:AccessKey", "ACCESS001" },
            { "Payment:MoMo:SecretKey", "SECRET001" },
            { "Payment:MoMo:BaseUrl", "https://test-payment.momo.vn" }
        });

        var responseJson = """{"resultCode":0,"payUrl":"https://test-payment.momo.vn/pay/123","requestId":"req-1","message":"Success"}""";
        var factory = new StubHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        });

        var svc = CreateMoMoService(config, factory);
        var result = await svc.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF002",
            Amount = 100_000,
            Description = "Top-up",
            ReturnUrl = "klc://callback",
            NotifyUrl = "/api/v1/payments/callback"
        });

        result.Success.ShouldBeTrue();
        result.RedirectUrl.ShouldBe("https://test-payment.momo.vn/pay/123");
    }

    [Fact]
    public async Task CreateTopUp_MoMo_Error_Returns_Fail()
    {
        var config = BuildConfig(new()
        {
            { "Payment:MoMo:PartnerCode", "PARTNER001" },
            { "Payment:MoMo:AccessKey", "ACCESS001" },
            { "Payment:MoMo:SecretKey", "SECRET001" }
        });

        var responseJson = """{"resultCode":11,"message":"Access denied","requestId":"req-1"}""";
        var factory = new StubHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        });

        var svc = CreateMoMoService(config, factory);
        var result = await svc.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF003",
            Amount = 50_000,
            Description = "Test",
            ReturnUrl = "https://example.com",
            NotifyUrl = "https://example.com/notify"
        });

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Access denied");
    }

    [Fact]
    public async Task CreateTopUp_Http_Error_Returns_Fail()
    {
        var config = BuildConfig(new()
        {
            { "Payment:MoMo:PartnerCode", "PARTNER001" },
            { "Payment:MoMo:AccessKey", "ACCESS001" },
            { "Payment:MoMo:SecretKey", "SECRET001" }
        });

        var factory = new StubHttpClientFactory(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server Error")
        });

        var svc = CreateMoMoService(config, factory);
        var result = await svc.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF004",
            Amount = 50_000,
            Description = "Test",
            ReturnUrl = "https://example.com",
            NotifyUrl = "https://example.com/notify"
        });

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("500");
    }

    [Fact]
    public async Task VerifyCallback_Without_SecretKey_Accepts_All()
    {
        var config = BuildConfig(new() { { "Payment:MoMo:SecretKey", "" } });
        var factory = new StubHttpClientFactory();
        var svc = CreateMoMoService(config, factory);

        var result = await svc.VerifyCallbackAsync("some data", "any-signature");

        result.IsValid.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyCallback_With_Missing_Signature_Rejects()
    {
        var config = BuildConfig(new() { { "Payment:MoMo:SecretKey", "secret123" } });
        var factory = new StubHttpClientFactory();
        var svc = CreateMoMoService(config, factory);

        var result = await svc.VerifyCallbackAsync("some data", null);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Missing signature");
    }

    [Fact]
    public async Task VerifyCallback_With_Wrong_Signature_Rejects()
    {
        var config = BuildConfig(new() { { "Payment:MoMo:SecretKey", "secret123" } });
        var factory = new StubHttpClientFactory();
        var svc = CreateMoMoService(config, factory);

        var result = await svc.VerifyCallbackAsync("some data", "wrong-sig");

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Invalid signature");
    }

    [Fact]
    public void Gateway_Should_Be_MoMo()
    {
        var config = BuildConfig(new());
        var factory = new StubHttpClientFactory();
        var svc = CreateMoMoService(config, factory);

        svc.Gateway.ShouldBe(PaymentGateway.MoMo);
    }

    private MoMoPaymentService CreateMoMoService(IConfiguration config, IHttpClientFactory factory)
    {
        var settings = new MoMoSettings();
        config.GetSection(MoMoSettings.Section).Bind(settings);
        return new MoMoPaymentService(_logger, config, Options.Create(settings), factory);
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values!).Build();
}

public class VnPayPaymentServiceTests
{
    private readonly ILogger<VnPayPaymentService> _logger = NullLogger<VnPayPaymentService>.Instance;

    [Fact]
    public async Task CreateTopUp_Without_Credentials_Returns_Stub_Url()
    {
        var config = BuildConfig(new() { { "Payment:VnPay:TmnCode", "" } });
        var svc = CreateVnPayService(config);

        var result = await svc.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF001",
            Amount = 50_000,
            Description = "Test",
            ReturnUrl = "https://example.com/return",
            NotifyUrl = "https://example.com/notify"
        });

        result.Success.ShouldBeTrue();
        result.RedirectUrl.ShouldContain("sandbox.vnpayment.vn");
    }

    [Fact]
    public async Task CreateTopUp_With_Credentials_Returns_Payment_Url()
    {
        var config = BuildConfig(new()
        {
            { "Payment:VnPay:TmnCode", "TMN001" },
            { "Payment:VnPay:HashSecret", "HASH_SECRET_123" },
            { "Payment:VnPay:BaseUrl", "https://sandbox.vnpayment.vn" },
            { "Payment:VnPay:Version", "2.1.0" }
        });

        var svc = CreateVnPayService(config);
        var result = await svc.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF002",
            Amount = 100_000,
            Description = "Top-up test",
            ReturnUrl = "klc://callback",
            NotifyUrl = "/api/v1/payments/callback"
        });

        result.Success.ShouldBeTrue();
        result.RedirectUrl.ShouldNotBeNull();
        result.RedirectUrl.ShouldContain("sandbox.vnpayment.vn/paymentv2/vpcpay.html");
        result.RedirectUrl.ShouldContain("vnp_TxnRef=REF002");
        result.RedirectUrl.ShouldContain("vnp_Amount=10000000"); // 100,000 * 100
        result.RedirectUrl.ShouldContain("vnp_SecureHash=");
        result.GatewayTransactionId.ShouldBe("REF002");
    }

    [Fact]
    public async Task CreateTopUp_Url_Contains_Required_Params()
    {
        var config = BuildConfig(new()
        {
            { "Payment:VnPay:TmnCode", "TMN001" },
            { "Payment:VnPay:HashSecret", "SECRET" },
            { "Payment:VnPay:Version", "2.1.0" }
        });

        var svc = CreateVnPayService(config);
        var result = await svc.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF003",
            Amount = 50_000,
            Description = "Session payment",
            ReturnUrl = "https://example.com/return",
            NotifyUrl = "https://example.com/notify"
        });

        result.Success.ShouldBeTrue();
        var url = result.RedirectUrl!;
        url.ShouldContain("vnp_Command=pay");
        url.ShouldContain("vnp_CurrCode=VND");
        url.ShouldContain("vnp_Locale=vn");
        url.ShouldContain("vnp_OrderType=topup");
        url.ShouldContain("vnp_TmnCode=TMN001");
        url.ShouldContain("vnp_Version=2.1.0");
    }

    [Fact]
    public async Task VerifyCallback_Without_HashSecret_Accepts_All()
    {
        var config = BuildConfig(new() { { "Payment:VnPay:HashSecret", "" } });
        var svc = CreateVnPayService(config);

        var result = await svc.VerifyCallbackAsync("vnp_TxnRef=REF001&vnp_ResponseCode=00", null);

        result.IsValid.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyCallback_With_Missing_Signature_Rejects()
    {
        var config = BuildConfig(new() { { "Payment:VnPay:HashSecret", "secret123" } });
        var svc = CreateVnPayService(config);

        var result = await svc.VerifyCallbackAsync("vnp_TxnRef=REF001", null);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Missing signature");
    }

    [Fact]
    public async Task VerifyCallback_With_Invalid_Signature_Rejects()
    {
        var config = BuildConfig(new() { { "Payment:VnPay:HashSecret", "secret123" } });
        var svc = CreateVnPayService(config);

        var rawData = "vnp_Amount=5000000&vnp_TxnRef=REF001&vnp_SecureHash=invalid_hash";
        var result = await svc.VerifyCallbackAsync(rawData, null);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Invalid signature");
    }

    [Fact]
    public void Gateway_Should_Be_VnPay()
    {
        var config = BuildConfig(new());
        var svc = CreateVnPayService(config);

        svc.Gateway.ShouldBe(PaymentGateway.VnPay);
    }

    private VnPayPaymentService CreateVnPayService(IConfiguration config)
    {
        var settings = new VnPaySettings();
        config.GetSection(VnPaySettings.Section).Bind(settings);
        return new VnPayPaymentService(_logger, config, Options.Create(settings), new StubHttpClientFactory());
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values!).Build();
}

/// <summary>
/// Stub IHttpClientFactory for unit testing payment services.
/// </summary>
internal class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpResponseMessage? _response;

    public StubHttpClientFactory(HttpResponseMessage? response = null)
    {
        _response = response;
    }

    public HttpClient CreateClient(string name)
    {
        if (_response != null)
        {
            return new HttpClient(new StubHttpHandler(_response));
        }
        return new HttpClient();
    }
}

internal class StubHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public StubHttpHandler(HttpResponseMessage response) => _response = response;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(_response);
}
