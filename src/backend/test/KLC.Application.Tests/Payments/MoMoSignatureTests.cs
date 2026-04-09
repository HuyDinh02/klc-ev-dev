using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

public class MoMoSignatureTests
{
    private const string TestSecretKey = "K951B6PE1waDMi640xX08PD3vg6EkVlz";

    private readonly ILogger<MoMoPaymentService> _logger = NullLogger<MoMoPaymentService>.Instance;

    private MoMoPaymentService CreateService(string? secretKey = TestSecretKey)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Payment:MoMo:PartnerCode", "MOMO_PARTNER" },
                { "Payment:MoMo:AccessKey", "F8BBA842ECF85" },
                { "Payment:MoMo:SecretKey", secretKey }
            })
            .Build();

        var settings = new MoMoSettings();
        config.GetSection(MoMoSettings.Section).Bind(settings);
        return new MoMoPaymentService(_logger, config, Options.Create(settings), new StubHttpClientFactory());
    }

    /// <summary>
    /// Compute the expected HMAC-SHA256 signature for a set of sorted parameters.
    /// This mirrors the production algorithm so tests can verify round-trip correctness.
    /// </summary>
    private static string ComputeExpectedSignature(Dictionary<string, string> parameters, string secretKey)
    {
        var sorted = new SortedDictionary<string, string>(parameters, StringComparer.Ordinal);
        sorted.Remove("signature");

        var rawData = string.Join("&", sorted.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void VerifyCallbackSignature_ValidSignature_ReturnsTrue()
    {
        var service = CreateService();
        var parameters = new Dictionary<string, string>
        {
            { "accessKey", "F8BBA842ECF85" },
            { "amount", "50000" },
            { "extraData", "" },
            { "orderId", "ORDER_001" },
            { "partnerCode", "MOMO_PARTNER" },
            { "requestId", "REQ_001" }
        };

        var validSignature = ComputeExpectedSignature(parameters, TestSecretKey);

        var result = service.VerifyCallbackSignature(parameters, validSignature);

        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyCallbackSignature_TamperedAmount_ReturnsFalse()
    {
        var service = CreateService();

        // Original parameters used to generate the signature
        var originalParameters = new Dictionary<string, string>
        {
            { "accessKey", "F8BBA842ECF85" },
            { "amount", "50000" },
            { "extraData", "" },
            { "orderId", "ORDER_002" },
            { "partnerCode", "MOMO_PARTNER" },
            { "requestId", "REQ_002" }
        };

        var validSignature = ComputeExpectedSignature(originalParameters, TestSecretKey);

        // Tamper with the amount
        var tamperedParameters = new Dictionary<string, string>(originalParameters)
        {
            ["amount"] = "999999"
        };

        var result = service.VerifyCallbackSignature(tamperedParameters, validSignature);

        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyCallbackSignature_TamperedRequestId_ReturnsFalse()
    {
        var service = CreateService();

        var originalParameters = new Dictionary<string, string>
        {
            { "accessKey", "F8BBA842ECF85" },
            { "amount", "50000" },
            { "extraData", "" },
            { "orderId", "ORDER_003" },
            { "partnerCode", "MOMO_PARTNER" },
            { "requestId", "REQ_003" }
        };

        var validSignature = ComputeExpectedSignature(originalParameters, TestSecretKey);

        // Tamper with the requestId
        var tamperedParameters = new Dictionary<string, string>(originalParameters)
        {
            ["requestId"] = "TAMPERED_REQ"
        };

        var result = service.VerifyCallbackSignature(tamperedParameters, validSignature);

        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyCallbackSignature_EmptySignature_ReturnsFalse()
    {
        var service = CreateService();
        var parameters = new Dictionary<string, string>
        {
            { "amount", "50000" },
            { "orderId", "ORDER_004" }
        };

        var result = service.VerifyCallbackSignature(parameters, string.Empty);

        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyCallbackSignature_NullSignature_ReturnsFalse()
    {
        var service = CreateService();
        var parameters = new Dictionary<string, string>
        {
            { "amount", "50000" },
            { "orderId", "ORDER_005" }
        };

        var result = service.VerifyCallbackSignature(parameters, null!);

        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyCallbackSignature_WrongSecretKey_ReturnsFalse()
    {
        // Service configured with TestSecretKey
        var service = CreateService();

        var parameters = new Dictionary<string, string>
        {
            { "accessKey", "F8BBA842ECF85" },
            { "amount", "50000" },
            { "extraData", "" },
            { "orderId", "ORDER_006" },
            { "partnerCode", "MOMO_PARTNER" },
            { "requestId", "REQ_006" }
        };

        // Sign with a DIFFERENT secret key
        var wrongKeySignature = ComputeExpectedSignature(parameters, "COMPLETELY_WRONG_SECRET_KEY");

        var result = service.VerifyCallbackSignature(parameters, wrongKeySignature);

        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyCallbackSignature_SignatureParamExcluded_ReturnsTrue()
    {
        // The 'signature' key in the parameters dict should be excluded from hash computation
        var service = CreateService();
        var parameters = new Dictionary<string, string>
        {
            { "accessKey", "F8BBA842ECF85" },
            { "amount", "50000" },
            { "orderId", "ORDER_007" },
            { "partnerCode", "MOMO_PARTNER" },
            { "requestId", "REQ_007" }
        };

        var validSignature = ComputeExpectedSignature(parameters, TestSecretKey);

        // Add 'signature' to parameters — it should be excluded during verification
        var paramsWithSignature = new Dictionary<string, string>(parameters)
        {
            { "signature", validSignature }
        };

        var result = service.VerifyCallbackSignature(paramsWithSignature, validSignature);

        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyCallbackSignature_WithoutSecretKeyConfig_ReturnsTrue()
    {
        // In dev mode (no secret key configured), all signatures should be accepted
        var service = CreateService(secretKey: "");

        var parameters = new Dictionary<string, string>
        {
            { "amount", "50000" },
            { "orderId", "ORDER_008" }
        };

        var result = service.VerifyCallbackSignature(parameters, "any-signature");

        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyCallbackSignature_CaseInsensitiveSignature_ReturnsTrue()
    {
        var service = CreateService();
        var parameters = new Dictionary<string, string>
        {
            { "accessKey", "F8BBA842ECF85" },
            { "amount", "75000" },
            { "orderId", "ORDER_009" },
            { "partnerCode", "MOMO_PARTNER" },
            { "requestId", "REQ_009" }
        };

        var validSignature = ComputeExpectedSignature(parameters, TestSecretKey);

        // Submit the signature in UPPERCASE — should still pass
        var result = service.VerifyCallbackSignature(parameters, validSignature.ToUpperInvariant());

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyCallbackAsync_ValidSignature_ReturnsValidResult()
    {
        var service = CreateService();

        var rawData = """{"partnerCode":"MOMO_PARTNER","orderId":"ORDER_010","requestId":"REQ_010","amount":50000,"transId":12345,"resultCode":0,"message":"Success","responseTime":1234567890,"extraData":""}""";
        var signature = ComputeHmacSha256(rawData, TestSecretKey);

        var result = await service.VerifyCallbackAsync(rawData, signature);

        result.IsValid.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        result.ReferenceCode.ShouldBe("ORDER_010");
        result.GatewayTransactionId.ShouldBe("12345");
    }

    [Fact]
    public async Task VerifyCallbackAsync_InvalidSignature_ReturnsInvalid()
    {
        var service = CreateService();

        var rawData = """{"orderId":"ORDER_011","resultCode":0}""";

        var result = await service.VerifyCallbackAsync(rawData, "invalid_signature");

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Invalid signature");
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
