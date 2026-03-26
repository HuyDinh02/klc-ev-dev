using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
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
    private readonly IHttpClientFactory _httpClientFactory;

    public VnPayPaymentService(
        ILogger<VnPayPaymentService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
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
        var now = DateTime.UtcNow.AddHours(7);
        var createDate = now.ToString("yyyyMMddHHmmss");
        var expireDate = now.AddMinutes(15).ToString("yyyyMMddHHmmss");

        // Build sorted parameter dictionary (VnPay requires alphabetical order)
        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "vnp_Amount", vnpAmount },
            { "vnp_Command", "pay" },
            { "vnp_CreateDate", createDate },
            { "vnp_CurrCode", "VND" },
            { "vnp_ExpireDate", expireDate },
            { "vnp_IpAddr", request.ClientIpAddress ?? "127.0.0.1" },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", SanitizeOrderInfo(request.Description) },
            { "vnp_OrderType", "topup" },
            { "vnp_ReturnUrl", request.ReturnUrl },
            { "vnp_TmnCode", tmnCode },
            { "vnp_TxnRef", request.ReferenceCode },
            { "vnp_Version", version }
        };

        // Optional: pre-select bank to skip bank selection page
        if (!string.IsNullOrEmpty(request.BankCode))
        {
            vnpParams["vnp_BankCode"] = request.BankCode;
        }

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

        if (!ConstantTimeEquals(expectedHash, vnpSecureHash))
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

        // Extract amount (VNPay stores amount * 100)
        decimal? callbackAmount = null;
        var vnpAmountStr = queryParams["vnp_Amount"];
        if (long.TryParse(vnpAmountStr, out var vnpAmountRaw))
        {
            callbackAmount = vnpAmountRaw / 100m;
        }

        _logger.LogInformation(
            "[VnPay] Callback verified: TxnRef={TxnRef}, ResponseCode={ResponseCode}, Amount={Amount}, Success={IsSuccess}",
            txnRef, responseCode, callbackAmount, isSuccess);

        return Task.FromResult(new PaymentCallbackResult
        {
            IsValid = true,
            IsSuccess = isSuccess,
            ReferenceCode = txnRef,
            GatewayTransactionId = gatewayTxId,
            CallbackAmount = callbackAmount,
            ErrorMessage = isSuccess ? null : $"VnPay response code: {responseCode}"
        });
    }

    /// <summary>
    /// Verify HMAC-SHA512 signature over sorted callback parameters.
    /// VnPay signature = HMAC_SHA512(hashSecret, sorted query params excluding vnp_SecureHash and vnp_SecureHashType).
    /// </summary>
    public bool VerifyCallbackSignature(Dictionary<string, string> parameters, string signature)
    {
        var hashSecret = _configuration["Payment:VnPay:HashSecret"];

        if (string.IsNullOrEmpty(hashSecret))
        {
            _logger.LogWarning(
                "[VnPay] Payment:VnPay:HashSecret is not configured. " +
                "Skipping signature verification (development mode)");
            return true;
        }

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("[VnPay] VerifyCallbackSignature rejected: empty or null signature");
            return false;
        }

        // Build sorted params excluding signature fields
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

        var dataToSign = BuildQueryString(sortedParams);
        var expectedHash = ComputeHmacSha512(dataToSign, hashSecret);

        return ConstantTimeEquals(expectedHash, signature);
    }

    public async Task<PaymentCallbackResult> QueryTransactionAsync(QueryTransactionRequest request)
    {
        var tmnCode = _configuration["Payment:VnPay:TmnCode"];
        var hashSecret = _configuration["Payment:VnPay:HashSecret"];
        var apiUrl = _configuration["Payment:VnPay:QueryApiUrl"]
                     ?? "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
        var version = _configuration["Payment:VnPay:Version"] ?? "2.1.0";

        if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(hashSecret))
        {
            _logger.LogWarning("[VnPay] QueryTransaction: credentials not configured (dev mode)");
            return new PaymentCallbackResult { IsValid = false, ErrorMessage = "VnPay not configured" };
        }

        var requestId = Guid.NewGuid().ToString("N");
        var createDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");
        var ipAddr = request.ClientIpAddress;
        var orderInfo = request.OrderInfo;

        // VNPay querydr uses pipe-delimited hash
        var hashData = $"{requestId}|{version}|querydr|{tmnCode}|{request.TxnRef}|{request.TransactionDate}|{createDate}|{ipAddr}|{orderInfo}";
        var secureHash = ComputeHmacSha512(hashData, hashSecret);

        var payload = new Dictionary<string, string>
        {
            { "vnp_RequestId", requestId },
            { "vnp_Version", version },
            { "vnp_Command", "querydr" },
            { "vnp_TmnCode", tmnCode },
            { "vnp_TxnRef", request.TxnRef },
            { "vnp_OrderInfo", orderInfo },
            { "vnp_TransactionDate", request.TransactionDate },
            { "vnp_CreateDate", createDate },
            { "vnp_IpAddr", ipAddr },
            { "vnp_SecureHash", secureHash }
        };

        if (!string.IsNullOrEmpty(request.GatewayTransactionId))
        {
            payload["vnp_TransactionNo"] = request.GatewayTransactionId;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("VnPay");
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[VnPay] QueryTransaction response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);
            if (result == null)
            {
                return new PaymentCallbackResult { IsValid = false, ErrorMessage = "Empty response" };
            }

            var responseCode = result.TryGetValue("vnp_ResponseCode", out var rc) ? rc.GetString() : null;
            var transactionStatus = result.TryGetValue("vnp_TransactionStatus", out var ts) ? ts.GetString() : null;
            var transactionNo = result.TryGetValue("vnp_TransactionNo", out var tn) ? tn.GetString() : null;

            decimal? amount = null;
            if (result.TryGetValue("vnp_Amount", out var amtEl))
            {
                var amtStr = amtEl.ValueKind == JsonValueKind.Number ? amtEl.GetInt64().ToString() : amtEl.GetString();
                if (long.TryParse(amtStr, out var amtRaw))
                {
                    amount = amtRaw / 100m;
                }
            }

            return new PaymentCallbackResult
            {
                IsValid = responseCode == "00",
                IsSuccess = transactionStatus == "00",
                ReferenceCode = request.TxnRef,
                GatewayTransactionId = transactionNo,
                CallbackAmount = amount,
                ErrorMessage = responseCode != "00" ? $"VnPay query response code: {responseCode}" : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VnPay] QueryTransaction failed for TxnRef={TxnRef}", request.TxnRef);
            return new PaymentCallbackResult { IsValid = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PaymentGatewayResult> RefundAsync(RefundGatewayRequest request)
    {
        var tmnCode = _configuration["Payment:VnPay:TmnCode"];
        var hashSecret = _configuration["Payment:VnPay:HashSecret"];
        var apiUrl = _configuration["Payment:VnPay:QueryApiUrl"]
                     ?? "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
        var version = _configuration["Payment:VnPay:Version"] ?? "2.1.0";

        if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(hashSecret))
        {
            _logger.LogWarning("[VnPay] Refund: credentials not configured (dev mode)");
            return PaymentGatewayResult.Fail("VnPay not configured");
        }

        var requestId = Guid.NewGuid().ToString("N");
        var createDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");
        var transactionType = request.IsFullRefund ? "02" : "03";
        var vnpAmount = ((long)(request.Amount * 100)).ToString();

        // VNPay refund uses pipe-delimited hash
        var hashData = $"{requestId}|{version}|refund|{tmnCode}|{transactionType}|{request.TxnRef}|{vnpAmount}|{request.GatewayTransactionId}|{request.TransactionDate}|{request.CreatedBy}|{createDate}|{request.ClientIpAddress}|{request.OrderInfo}";
        var secureHash = ComputeHmacSha512(hashData, hashSecret);

        var payload = new Dictionary<string, string>
        {
            { "vnp_RequestId", requestId },
            { "vnp_Version", version },
            { "vnp_Command", "refund" },
            { "vnp_TmnCode", tmnCode },
            { "vnp_TransactionType", transactionType },
            { "vnp_TxnRef", request.TxnRef },
            { "vnp_Amount", vnpAmount },
            { "vnp_TransactionNo", request.GatewayTransactionId },
            { "vnp_TransactionDate", request.TransactionDate },
            { "vnp_CreateBy", request.CreatedBy },
            { "vnp_CreateDate", createDate },
            { "vnp_IpAddr", request.ClientIpAddress },
            { "vnp_OrderInfo", request.OrderInfo },
            { "vnp_SecureHash", secureHash }
        };

        try
        {
            var client = _httpClientFactory.CreateClient("VnPay");
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[VnPay] Refund response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);
            if (result == null)
            {
                return PaymentGatewayResult.Fail("Empty response from VnPay");
            }

            var responseCode = result.TryGetValue("vnp_ResponseCode", out var rc) ? rc.GetString() : null;
            var refundTxnNo = result.TryGetValue("vnp_TransactionNo", out var tn) ? tn.GetString() : null;

            if (responseCode == "00")
            {
                _logger.LogInformation(
                    "[VnPay] Refund successful: TxnRef={TxnRef}, RefundTxnNo={RefundTxnNo}",
                    request.TxnRef, refundTxnNo);
                return PaymentGatewayResult.Ok(string.Empty, refundTxnNo);
            }

            var message = result.TryGetValue("vnp_Message", out var msg) ? msg.GetString() : "Unknown error";
            _logger.LogWarning(
                "[VnPay] Refund failed: TxnRef={TxnRef}, ResponseCode={Code}, Message={Message}",
                request.TxnRef, responseCode, message);
            return PaymentGatewayResult.Fail($"VnPay refund error {responseCode}: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VnPay] Refund failed for TxnRef={TxnRef}", request.TxnRef);
            return PaymentGatewayResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Build a URL-encoded query string from sorted parameters.
    /// Uses WebUtility.UrlEncode matching VNPay's official C# SDK (VnPayLibrary.cs).
    /// </summary>
    private static string BuildQueryString(SortedDictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        var first = true;

        foreach (var kvp in parameters)
        {
            if (string.IsNullOrEmpty(kvp.Value)) continue;

            if (!first)
            {
                sb.Append('&');
            }

            sb.Append(WebUtility.UrlEncode(kvp.Key));
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(kvp.Value));
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

    /// <summary>
    /// VNPay requires vnp_OrderInfo to be Vietnamese without diacritics and no special characters.
    /// Remove diacritics and strip non-alphanumeric chars (except spaces, hyphens, underscores).
    /// </summary>
    private static string SanitizeOrderInfo(string input)
    {
        if (string.IsNullOrEmpty(input)) return "Payment";

        // Normalize and remove diacritics
        var normalized = input.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var withoutDiacritics = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);

        // Replace special Vietnamese characters
        withoutDiacritics = withoutDiacritics.Replace("đ", "d").Replace("Đ", "D");

        // Keep only safe characters (alphanumeric, space, hyphen, colon, hash)
        var result = new StringBuilder();
        foreach (var c in withoutDiacritics)
        {
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == ':' || c == '#')
            {
                result.Append(c);
            }
        }

        var sanitized = result.ToString().Trim();
        return string.IsNullOrEmpty(sanitized) ? "Payment" : sanitized;
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks on HMAC signatures.
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a.ToLowerInvariant());
        var bBytes = Encoding.UTF8.GetBytes(b.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
