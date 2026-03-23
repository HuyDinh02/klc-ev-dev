using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using KLC.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace KLC.Payments;

public class VnPaySignatureTests
{
    private const string TestHashSecret = "SCZEOZ69HGQAGSRFHXU6XLUADEPNBIOI";

    private readonly ILogger<VnPayPaymentService> _logger = NullLogger<VnPayPaymentService>.Instance;

    private VnPayPaymentService CreateService(string? hashSecret = TestHashSecret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Payment:VnPay:TmnCode", "VNPAY_TMN" },
                { "Payment:VnPay:HashSecret", hashSecret },
                { "Payment:VnPay:Version", "2.1.0" }
            })
            .Build();

        return new VnPayPaymentService(_logger, config, new StubHttpClientFactory());
    }

    /// <summary>
    /// Compute the expected HMAC-SHA512 signature for VnPay callback parameters.
    /// Mirrors VnPay production algorithm: sorted params (excluding vnp_SecureHash/vnp_SecureHashType),
    /// URL-encoded, joined by &amp;, then HMAC-SHA512.
    /// </summary>
    private static string ComputeExpectedVnPaySignature(Dictionary<string, string> parameters, string hashSecret)
    {
        var sortedParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kvp in parameters)
        {
            if (kvp.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            sortedParams[kvp.Key] = kvp.Value;
        }

        var sb = new StringBuilder();
        var first = true;
        foreach (var kvp in sortedParams)
        {
            if (!first) sb.Append('&');
            sb.Append(HttpUtility.UrlEncode(kvp.Key));
            sb.Append('=');
            sb.Append(HttpUtility.UrlEncode(kvp.Value));
            first = false;
        }

        var dataToSign = sb.ToString();

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hashSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void VerifyCallbackSignature_ValidSignature_ReturnsTrue()
    {
        var service = CreateService();
        var parameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "5000000" },
            { "vnp_Command", "pay" },
            { "vnp_CurrCode", "VND" },
            { "vnp_OrderInfo", "Top-up test" },
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "VNPAY_TMN" },
            { "vnp_TransactionNo", "14257896" },
            { "vnp_TxnRef", "REF_001" }
        };

        var validSignature = ComputeExpectedVnPaySignature(parameters, TestHashSecret);

        var result = service.VerifyCallbackSignature(parameters, validSignature);

        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyCallbackSignature_TamperedAmount_ReturnsFalse()
    {
        var service = CreateService();

        var originalParameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "5000000" },
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "VNPAY_TMN" },
            { "vnp_TransactionNo", "14257896" },
            { "vnp_TxnRef", "REF_002" }
        };

        var validSignature = ComputeExpectedVnPaySignature(originalParameters, TestHashSecret);

        // Tamper with the amount
        var tamperedParameters = new Dictionary<string, string>(originalParameters)
        {
            ["vnp_Amount"] = "99999900"
        };

        var result = service.VerifyCallbackSignature(tamperedParameters, validSignature);

        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyCallbackSignature_TamperedTransactionRef_ReturnsFalse()
    {
        var service = CreateService();

        var originalParameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "5000000" },
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "VNPAY_TMN" },
            { "vnp_TransactionNo", "14257896" },
            { "vnp_TxnRef", "REF_003" }
        };

        var validSignature = ComputeExpectedVnPaySignature(originalParameters, TestHashSecret);

        // Tamper with the TxnRef
        var tamperedParameters = new Dictionary<string, string>(originalParameters)
        {
            ["vnp_TxnRef"] = "TAMPERED_REF"
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
            { "vnp_Amount", "5000000" },
            { "vnp_TxnRef", "REF_004" }
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
            { "vnp_Amount", "5000000" },
            { "vnp_TxnRef", "REF_005" }
        };

        var result = service.VerifyCallbackSignature(parameters, null!);

        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyCallbackSignature_WrongHashSecret_ReturnsFalse()
    {
        var service = CreateService();

        var parameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "5000000" },
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "VNPAY_TMN" },
            { "vnp_TransactionNo", "14257896" },
            { "vnp_TxnRef", "REF_006" }
        };

        // Sign with a different hash secret
        var wrongKeySignature = ComputeExpectedVnPaySignature(parameters, "COMPLETELY_WRONG_HASH_SECRET");

        var result = service.VerifyCallbackSignature(parameters, wrongKeySignature);

        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyCallbackSignature_SecureHashExcluded_ReturnsTrue()
    {
        // vnp_SecureHash and vnp_SecureHashType in parameters should be excluded from hash computation
        var service = CreateService();
        var parameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "5000000" },
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "VNPAY_TMN" },
            { "vnp_TxnRef", "REF_007" }
        };

        var validSignature = ComputeExpectedVnPaySignature(parameters, TestHashSecret);

        // Add vnp_SecureHash and vnp_SecureHashType — they should be excluded
        var paramsWithHash = new Dictionary<string, string>(parameters)
        {
            { "vnp_SecureHash", validSignature },
            { "vnp_SecureHashType", "SHA512" }
        };

        var result = service.VerifyCallbackSignature(paramsWithHash, validSignature);

        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyCallbackSignature_WithoutHashSecretConfig_ReturnsTrue()
    {
        // In dev mode (no hash secret configured), all signatures should be accepted
        var service = CreateService(hashSecret: "");

        var parameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "5000000" },
            { "vnp_TxnRef", "REF_008" }
        };

        var result = service.VerifyCallbackSignature(parameters, "any-signature-value");

        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyCallbackSignature_CaseInsensitiveSignature_ReturnsTrue()
    {
        var service = CreateService();
        var parameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "7500000" },
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "VNPAY_TMN" },
            { "vnp_TxnRef", "REF_009" }
        };

        var validSignature = ComputeExpectedVnPaySignature(parameters, TestHashSecret);

        // Submit signature in UPPERCASE — should still pass
        var result = service.VerifyCallbackSignature(parameters, validSignature.ToUpperInvariant());

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyCallbackAsync_ValidSignature_ReturnsValidResult()
    {
        var service = CreateService();

        // Build a valid callback query string
        var parameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "5000000" },
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "VNPAY_TMN" },
            { "vnp_TransactionNo", "14257896" },
            { "vnp_TxnRef", "REF_010" }
        };

        var validSignature = ComputeExpectedVnPaySignature(parameters, TestHashSecret);

        // Build raw query string
        var queryParts = new List<string>();
        foreach (var kvp in parameters)
        {
            queryParts.Add($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}");
        }
        queryParts.Add($"vnp_SecureHash={validSignature}");
        var rawData = string.Join("&", queryParts);

        var result = await service.VerifyCallbackAsync(rawData, null);

        result.IsValid.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        result.ReferenceCode.ShouldBe("REF_010");
        result.GatewayTransactionId.ShouldBe("14257896");
    }

    [Fact]
    public async Task VerifyCallbackAsync_InvalidSignature_ReturnsInvalid()
    {
        var service = CreateService();

        var rawData = "vnp_Amount=5000000&vnp_TxnRef=REF_011&vnp_SecureHash=totally_invalid_hash";

        var result = await service.VerifyCallbackAsync(rawData, null);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Invalid signature");
    }

    [Fact]
    public async Task VerifyCallbackAsync_FailedPayment_ReturnsSuccessFalse()
    {
        var service = CreateService();

        var parameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "5000000" },
            { "vnp_ResponseCode", "24" }, // Transaction cancelled by user
            { "vnp_TmnCode", "VNPAY_TMN" },
            { "vnp_TransactionNo", "14257899" },
            { "vnp_TxnRef", "REF_012" }
        };

        var validSignature = ComputeExpectedVnPaySignature(parameters, TestHashSecret);

        var queryParts = new List<string>();
        foreach (var kvp in parameters)
        {
            queryParts.Add($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}");
        }
        queryParts.Add($"vnp_SecureHash={validSignature}");
        var rawData = string.Join("&", queryParts);

        var result = await service.VerifyCallbackAsync(rawData, null);

        result.IsValid.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("24");
    }

    [Fact]
    public async Task VerifyCallbackAsync_ExtractsCallbackAmount()
    {
        var service = CreateService();

        var parameters = new Dictionary<string, string>
        {
            { "vnp_Amount", "5000000" },  // 50,000 VND * 100
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "VNPAY_TMN" },
            { "vnp_TransactionNo", "14257896" },
            { "vnp_TxnRef", "REF_013" }
        };

        var validSignature = ComputeExpectedVnPaySignature(parameters, TestHashSecret);

        var queryParts = new List<string>();
        foreach (var kvp in parameters)
        {
            queryParts.Add($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}");
        }
        queryParts.Add($"vnp_SecureHash={validSignature}");
        var rawData = string.Join("&", queryParts);

        var result = await service.VerifyCallbackAsync(rawData, null);

        result.IsValid.ShouldBeTrue();
        result.CallbackAmount.ShouldNotBeNull();
        result.CallbackAmount.Value.ShouldBe(50_000m); // 5000000 / 100
    }

    [Fact]
    public async Task CreateTopUp_IncludesExpireDate()
    {
        var service = CreateService();

        var result = await service.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF_014",
            Amount = 100_000,
            Description = "Test top-up",
            ReturnUrl = "https://example.com/return",
            NotifyUrl = "https://example.com/notify"
        });

        result.Success.ShouldBeTrue();
        result.RedirectUrl.ShouldContain("vnp_ExpireDate=");
    }

    [Fact]
    public async Task CreateTopUp_UsesClientIpAddress()
    {
        var service = CreateService();

        var result = await service.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF_015",
            Amount = 100_000,
            Description = "Test",
            ReturnUrl = "https://example.com/return",
            NotifyUrl = "https://example.com/notify",
            ClientIpAddress = "192.168.1.100"
        });

        result.Success.ShouldBeTrue();
        result.RedirectUrl.ShouldContain("vnp_IpAddr=192.168.1.100");
    }

    [Fact]
    public async Task CreateTopUp_DefaultsIpTo127001_WhenNotProvided()
    {
        var service = CreateService();

        var result = await service.CreateTopUpAsync(new CreateTopUpRequest
        {
            ReferenceCode = "REF_016",
            Amount = 100_000,
            Description = "Test",
            ReturnUrl = "https://example.com/return",
            NotifyUrl = "https://example.com/notify"
        });

        result.Success.ShouldBeTrue();
        result.RedirectUrl.ShouldContain("vnp_IpAddr=127.0.0.1");
    }
}
