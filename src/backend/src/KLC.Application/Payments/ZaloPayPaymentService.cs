using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Configuration;
using KLC.Enums;
using KLC.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace KLC.Payments;

/// <summary>
/// ZaloPay payment gateway service.
/// Docs: https://docs.zalopay.vn/vi/docs/guides/integration-guide/intro
/// key1: sign requests TO ZaloPay (create, query, refund)
/// key2: verify callbacks FROM ZaloPay (IPN)
/// </summary>
public class ZaloPayPaymentService : IPaymentGatewayService, ITransientDependency
{
    private readonly ILogger<ZaloPayPaymentService> _logger;
    private readonly ZaloPaySettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    public ZaloPayPaymentService(
        ILogger<ZaloPayPaymentService> logger,
        IOptions<ZaloPaySettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public PaymentGateway Gateway => PaymentGateway.ZaloPay;

    public async Task<PaymentGatewayResult> CreateTopUpAsync(CreateTopUpRequest request)
    {
        if (_settings.AppId == 0 || string.IsNullOrEmpty(_settings.Key1))
        {
            _logger.LogWarning("[ZaloPay] AppId or Key1 not configured — stub mode");
            return PaymentGatewayResult.Ok(
                $"https://sbgateway.zalopay.vn/openinapp?order=stub_{request.ReferenceCode}",
                $"ZLP_{Guid.NewGuid():N}");
        }

        var now = DateTime.UtcNow.AddHours(7);
        var appTransId = $"{now:yyMMdd}_{request.ReferenceCode}";
        var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var amount = (long)request.Amount;
        var appUser = request.ReferenceCode; // Use reference code as app_user
        var embedData = JsonSerializer.Serialize(new
        {
            redirecturl = request.ReturnUrl,
            preferred_payment_method = new[] { "zalopay_wallet", "domestic_card", "vietqr" }
        });
        var item = "[]";
        var description = SanitizeDescription(request.Description);

        // MAC = HMAC-SHA256(key1, app_id|app_trans_id|app_user|amount|app_time|embed_data|item)
        var macInput = $"{_settings.AppId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{item}";
        var mac = CryptoService.HmacSha256(_settings.Key1, macInput);

        var payload = new Dictionary<string, string>
        {
            { "app_id", _settings.AppId.ToString() },
            { "app_user", appUser },
            { "app_trans_id", appTransId },
            { "app_time", appTime.ToString() },
            { "amount", amount.ToString() },
            { "description", description },
            { "item", item },
            { "embed_data", embedData },
            { "mac", mac },
            { "expire_duration_seconds", "900" }, // 15 minutes
        };

        if (!string.IsNullOrEmpty(_settings.CallbackUrl))
        {
            payload["callback_url"] = _settings.CallbackUrl;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(payload);
            var response = await client.PostAsync($"{_settings.BaseUrl}/v2/create", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[ZaloPay] CreateOrder response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var returnCode = result.GetProperty("return_code").GetInt32();

            if (returnCode == 1)
            {
                var orderUrl = result.GetProperty("order_url").GetString() ?? "";
                var zpTransToken = result.TryGetProperty("zp_trans_token", out var token)
                    ? token.GetString() : null;

                _logger.LogInformation(
                    "[ZaloPay] Order created: AppTransId={AppTransId}, OrderUrl={OrderUrl}",
                    appTransId, orderUrl);

                return PaymentGatewayResult.Ok(orderUrl, appTransId);
            }

            var message = result.TryGetProperty("sub_return_message", out var msg)
                ? msg.GetString() : result.GetProperty("return_message").GetString();

            _logger.LogWarning("[ZaloPay] CreateOrder failed: {Code} {Message}", returnCode, message);
            return PaymentGatewayResult.Fail($"ZaloPay error: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ZaloPay] CreateOrder failed for {Ref}", request.ReferenceCode);
            return PaymentGatewayResult.Fail(ex.Message);
        }
    }

    public Task<PaymentCallbackResult> VerifyCallbackAsync(string rawData, string? signature)
    {
        if (string.IsNullOrEmpty(_settings.Key2))
        {
            _logger.LogWarning("[ZaloPay] Key2 not configured — accepting callback without verification");
            return Task.FromResult(new PaymentCallbackResult { IsValid = true, IsSuccess = true });
        }

        // ZaloPay callback body: { "data": "json_string", "mac": "hmac", "type": 1 }
        try
        {
            var callback = JsonSerializer.Deserialize<JsonElement>(rawData);
            var dataStr = callback.GetProperty("data").GetString() ?? "";
            var mac = callback.GetProperty("mac").GetString() ?? "";

            // Verify: HMAC-SHA256(key2, data_string)
            var expectedMac = CryptoService.HmacSha256(_settings.Key2, dataStr);
            if (!CryptoService.ConstantTimeEquals(expectedMac, mac))
            {
                _logger.LogWarning("[ZaloPay] Callback rejected: invalid MAC");
                return Task.FromResult(new PaymentCallbackResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid signature"
                });
            }

            // Parse data
            var data = JsonSerializer.Deserialize<JsonElement>(dataStr);
            var appTransId = data.GetProperty("app_trans_id").GetString();
            var zpTransId = data.GetProperty("zp_trans_id").GetInt64().ToString();
            var amount = data.GetProperty("amount").GetInt64();

            // Extract reference code from app_trans_id (format: yymmdd_WTX...)
            var referenceCode = appTransId?.Contains('_') == true
                ? appTransId[(appTransId.IndexOf('_') + 1)..]
                : appTransId;

            _logger.LogInformation(
                "[ZaloPay] Callback verified: AppTransId={AppTransId}, ZpTransId={ZpTransId}, Amount={Amount}",
                appTransId, zpTransId, amount);

            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = true,
                IsSuccess = true, // ZaloPay only sends callback on success
                ReferenceCode = referenceCode,
                GatewayTransactionId = zpTransId,
                CallbackAmount = amount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ZaloPay] Callback parsing failed");
            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public bool VerifyCallbackSignature(Dictionary<string, string> parameters, string signature)
    {
        if (string.IsNullOrEmpty(_settings.Key2))
        {
            _logger.LogWarning("[ZaloPay] Key2 not configured — skipping signature verification");
            return true;
        }

        // For redirect verification: HMAC-SHA256(key2, appid|apptransid|pmcid|bankcode|amount|discountamount|status)
        var dataStr = parameters.GetValueOrDefault("data", "");
        if (!string.IsNullOrEmpty(dataStr))
        {
            var expectedMac = CryptoService.HmacSha256(_settings.Key2, dataStr);
            return CryptoService.ConstantTimeEquals(expectedMac, signature);
        }

        return false;
    }

    public async Task<PaymentCallbackResult> QueryTransactionAsync(QueryTransactionRequest request)
    {
        if (_settings.AppId == 0 || string.IsNullOrEmpty(_settings.Key1))
        {
            _logger.LogWarning("[ZaloPay] QueryTransaction: credentials not configured");
            return new PaymentCallbackResult { IsValid = false, ErrorMessage = "ZaloPay not configured" };
        }

        // app_trans_id format: yymmdd_WTX...
        var appTransId = request.TxnRef.Contains('_') ? request.TxnRef : $"{DateTime.UtcNow.AddHours(7):yyMMdd}_{request.TxnRef}";

        // MAC = HMAC-SHA256(key1, app_id|app_trans_id|key1)
        var macInput = $"{_settings.AppId}|{appTransId}|{_settings.Key1}";
        var mac = CryptoService.HmacSha256(_settings.Key1, macInput);

        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "app_id", _settings.AppId.ToString() },
                { "app_trans_id", appTransId },
                { "mac", mac }
            });

            var response = await client.PostAsync($"{_settings.BaseUrl}/v2/query", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[ZaloPay] QueryTransaction response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var returnCode = result.GetProperty("return_code").GetInt32();
            var zpTransId = result.TryGetProperty("zp_trans_id", out var zt) ? zt.GetInt64().ToString() : null;

            return new PaymentCallbackResult
            {
                IsValid = true,
                IsSuccess = returnCode == 1,
                ReferenceCode = request.TxnRef,
                GatewayTransactionId = zpTransId,
                ErrorMessage = returnCode != 1
                    ? (result.TryGetProperty("sub_return_message", out var msg) ? msg.GetString() : $"return_code: {returnCode}")
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ZaloPay] QueryTransaction failed for {TxnRef}", request.TxnRef);
            return new PaymentCallbackResult { IsValid = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<PaymentGatewayResult> RefundAsync(RefundGatewayRequest request)
    {
        if (_settings.AppId == 0 || string.IsNullOrEmpty(_settings.Key1))
        {
            return PaymentGatewayResult.Fail("ZaloPay not configured");
        }

        var now = DateTime.UtcNow.AddHours(7);
        var mRefundId = $"{now:yyMMdd}_{_settings.AppId}_{Guid.NewGuid():N}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var amount = (long)request.Amount;

        // MAC = HMAC-SHA256(key1, app_id|zp_trans_id|amount|description|timestamp)
        var macInput = $"{_settings.AppId}|{request.GatewayTransactionId}|{amount}|{request.OrderInfo}|{timestamp}";
        var mac = CryptoService.HmacSha256(_settings.Key1, macInput);

        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "app_id", _settings.AppId.ToString() },
                { "m_refund_id", mRefundId },
                { "zp_trans_id", request.GatewayTransactionId },
                { "amount", amount.ToString() },
                { "timestamp", timestamp.ToString() },
                { "description", request.OrderInfo },
                { "mac", mac }
            });

            var response = await client.PostAsync($"{_settings.BaseUrl}/v2/refund", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[ZaloPay] Refund response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var returnCode = result.GetProperty("return_code").GetInt32();

            if (returnCode == 1)
            {
                var refundId = result.TryGetProperty("refund_id", out var ri) ? ri.GetInt64().ToString() : mRefundId;
                return PaymentGatewayResult.Ok(string.Empty, refundId);
            }

            var message = result.TryGetProperty("sub_return_message", out var msg) ? msg.GetString() : "Refund failed";
            return PaymentGatewayResult.Fail($"ZaloPay refund error: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ZaloPay] Refund failed for {TxnRef}", request.TxnRef);
            return PaymentGatewayResult.Fail(ex.Message);
        }
    }

    private static string SanitizeDescription(string input)
    {
        if (string.IsNullOrEmpty(input)) return "Top-up wallet";
        return input.Length > 256 ? input[..256] : input;
    }
}
