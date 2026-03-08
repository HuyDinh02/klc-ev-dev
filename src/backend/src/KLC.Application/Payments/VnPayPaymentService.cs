using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using KLC.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace KLC.Payments;

/// <summary>
/// Real VnPay payment gateway service using VnPay redirect-based payment flow.
/// VnPay docs: https://sandbox.vnpayment.vn/apis/
/// Falls back to stub behavior when TmnCode or HashSecret are not configured.
/// </summary>
public class VnPayPaymentService : IPaymentGatewayService, ITransientDependency
{
    private readonly ILogger<VnPayPaymentService> _logger;
    private readonly IConfiguration _configuration;

    public VnPayPaymentService(
        ILogger<VnPayPaymentService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public PaymentGateway Gateway => PaymentGateway.VnPay;

    public Task<PaymentGatewayResult> CreateTopUpAsync(CreateTopUpRequest request)
    {
        var tmnCode = _configuration["Payment:VnPay:TmnCode"];
        var hashSecret = _configuration["Payment:VnPay:HashSecret"];
        var baseUrl = _configuration["Payment:VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn";
        var version = _configuration["Payment:VnPay:Version"] ?? "2.1.0";

        if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(hashSecret))
        {
            _logger.LogWarning(
                "[VnPay] Payment:VnPay:TmnCode or HashSecret not configured. " +
                "Falling back to stub behavior (development mode)");

            var fakeRedirectUrl = $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?ref={request.ReferenceCode}";
            var fakeTxId = $"VNPAY_{Guid.NewGuid():N}";
            return Task.FromResult(PaymentGatewayResult.Ok(fakeRedirectUrl, fakeTxId));
        }

        _logger.LogInformation(
            "[VnPay] CreateTopUp: Ref={ReferenceCode}, Amount={Amount}",
            request.ReferenceCode, request.Amount);

        // VnPay uses smallest currency unit (VND * 100)
        var vnpAmount = ((long)(request.Amount * 100)).ToString();
        var createDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");

        // Build sorted parameter dictionary (VnPay requires alphabetical order)
        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "vnp_Amount", vnpAmount },
            { "vnp_Command", "pay" },
            { "vnp_CreateDate", createDate },
            { "vnp_CurrCode", "VND" },
            { "vnp_IpAddr", "127.0.0.1" },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", request.Description },
            { "vnp_OrderType", "topup" },
            { "vnp_ReturnUrl", request.ReturnUrl },
            { "vnp_TmnCode", tmnCode },
            { "vnp_TxnRef", request.ReferenceCode },
            { "vnp_Version", version }
        };

        // Build query string from sorted params
        var queryString = BuildQueryString(vnpParams);

        // Compute HMAC-SHA512 signature over the sorted query string
        var secureHash = ComputeHmacSha512(queryString, hashSecret);

        // Append signature to the query string
        var fullQueryString = $"{queryString}&vnp_SecureHash={secureHash}";
        var paymentUrl = $"{baseUrl}/paymentv2/vpcpay.html?{fullQueryString}";

        _logger.LogInformation(
            "[VnPay] Payment URL generated for Ref={ReferenceCode}, TxnRef={TxnRef}",
            request.ReferenceCode, request.ReferenceCode);

        return Task.FromResult(PaymentGatewayResult.Ok(paymentUrl, request.ReferenceCode));
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

        // Parse the callback query string to extract parameters
        var queryParams = HttpUtility.ParseQueryString(rawData);

        // Extract the secure hash sent by VnPay
        var vnpSecureHash = queryParams["vnp_SecureHash"];
        if (string.IsNullOrEmpty(vnpSecureHash) && !string.IsNullOrEmpty(signature))
        {
            vnpSecureHash = signature;
        }

        if (string.IsNullOrEmpty(vnpSecureHash))
        {
            _logger.LogWarning("[VnPay] Callback rejected: missing vnp_SecureHash");
            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = false,
                ErrorMessage = "Missing signature"
            });
        }

        // Rebuild sorted params excluding vnp_SecureHash and vnp_SecureHashType
        var sortedParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (string? key in queryParams.AllKeys)
        {
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = queryParams[key];
            if (value != null)
            {
                sortedParams[key] = value;
            }
        }

        var dataToSign = BuildQueryString(sortedParams);
        var expectedHash = ComputeHmacSha512(dataToSign, hashSecret);

        if (!string.Equals(expectedHash, vnpSecureHash, StringComparison.OrdinalIgnoreCase))
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

        // Extract transaction details from callback
        var responseCode = queryParams["vnp_ResponseCode"];
        var txnRef = queryParams["vnp_TxnRef"];
        var gatewayTxId = queryParams["vnp_TransactionNo"];
        var isSuccess = responseCode == "00";

        _logger.LogInformation(
            "[VnPay] Callback verified: TxnRef={TxnRef}, ResponseCode={ResponseCode}, Success={IsSuccess}",
            txnRef, responseCode, isSuccess);

        return Task.FromResult(new PaymentCallbackResult
        {
            IsValid = true,
            IsSuccess = isSuccess,
            ReferenceCode = txnRef,
            GatewayTransactionId = gatewayTxId,
            ErrorMessage = isSuccess ? null : $"VnPay response code: {responseCode}"
        });
    }

    /// <summary>
    /// Build a URL-encoded query string from sorted parameters.
    /// Format: key1=value1&amp;key2=value2 (values are URL-encoded).
    /// </summary>
    private static string BuildQueryString(SortedDictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        var first = true;

        foreach (var kvp in parameters)
        {
            if (!first)
            {
                sb.Append('&');
            }

            sb.Append(HttpUtility.UrlEncode(kvp.Key));
            sb.Append('=');
            sb.Append(HttpUtility.UrlEncode(kvp.Value));
            first = false;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Compute HMAC-SHA512 hash. VnPay requires SHA512 (not SHA256).
    /// </summary>
    private static string ComputeHmacSha512(string data, string key)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
