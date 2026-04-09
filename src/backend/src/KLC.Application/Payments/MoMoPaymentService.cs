using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using KLC.Configuration;
using KLC.Enums;
using KLC.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace KLC.Payments;

/// <summary>
/// MoMo Payment Gateway integration using MoMo Payment API v2.
/// Falls back to stub behavior when PartnerCode/AccessKey are not configured.
/// </summary>
public class MoMoPaymentService : IPaymentGatewayService, ITransientDependency
{
    private const string DefaultSandboxUrl = "https://test-payment.momo.vn";
    private const string CreatePaymentEndpoint = "/v2/gateway/api/create";
    private const string RequestType = "captureWallet";

    private readonly ILogger<MoMoPaymentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly MoMoSettings _momoSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    public MoMoPaymentService(
        ILogger<MoMoPaymentService> logger,
        IConfiguration configuration,
        IOptions<MoMoSettings> momoSettings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _momoSettings = momoSettings.Value;
        _httpClientFactory = httpClientFactory;
    }

    public PaymentGateway Gateway => PaymentGateway.MoMo;

    public async Task<PaymentGatewayResult> CreateTopUpAsync(CreateTopUpRequest request)
    {
        var partnerCode = _momoSettings.PartnerCode;
        var accessKey = _momoSettings.AccessKey;
        var secretKey = _momoSettings.SecretKey;
        var baseUrl = !string.IsNullOrEmpty(_momoSettings.BaseUrl) ? _momoSettings.BaseUrl : DefaultSandboxUrl;

        if (string.IsNullOrEmpty(partnerCode) || string.IsNullOrEmpty(accessKey))
        {
            _logger.LogWarning(
                "[MoMo] Payment:MoMo:PartnerCode or AccessKey is not configured. " +
                "Falling back to stub behavior (development mode)");

            var fakeRedirectUrl = $"https://test-payment.momo.vn/v2/gateway/pay?ref={request.ReferenceCode}";
            var fakeTxId = $"MOMO_{Guid.NewGuid():N}";

            return PaymentGatewayResult.Ok(fakeRedirectUrl, fakeTxId);
        }

        var requestId = Guid.NewGuid().ToString();
        var amount = (long)request.Amount;
        var orderId = request.ReferenceCode;
        var orderInfo = request.Description;
        var redirectUrl = request.ReturnUrl;
        var ipnUrl = request.NotifyUrl;
        var extraData = string.Empty;

        // Build signature according to MoMo v2 spec
        var rawSignature = $"accessKey={accessKey}" +
                           $"&amount={amount}" +
                           $"&extraData={extraData}" +
                           $"&ipnUrl={ipnUrl}" +
                           $"&orderId={orderId}" +
                           $"&orderInfo={orderInfo}" +
                           $"&partnerCode={partnerCode}" +
                           $"&redirectUrl={redirectUrl}" +
                           $"&requestId={requestId}" +
                           $"&requestType={RequestType}";

        var signature = CryptoService.HmacSha256(secretKey!, rawSignature);

        var requestBody = new MoMoCreateRequest
        {
            PartnerCode = partnerCode,
            AccessKey = accessKey,
            RequestId = requestId,
            Amount = amount,
            OrderId = orderId,
            OrderInfo = orderInfo,
            RedirectUrl = redirectUrl,
            IpnUrl = ipnUrl,
            ExtraData = extraData,
            RequestType = RequestType,
            Signature = signature,
            Lang = "vi"
        };

        var jsonContent = JsonSerializer.Serialize(requestBody, MoMoJsonContext.Default.MoMoCreateRequest);

        _logger.LogInformation(
            "[MoMo] CreateTopUp: Ref={ReferenceCode}, Amount={Amount}, RequestId={RequestId}, Endpoint={Endpoint}",
            orderId, amount, requestId, $"{baseUrl}{CreatePaymentEndpoint}");

        try
        {
            var httpClient = _httpClientFactory.CreateClient("MoMo");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{CreatePaymentEndpoint}")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            var response = await httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "[MoMo] Response: StatusCode={StatusCode}, Body={Body}",
                (int)response.StatusCode, responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[MoMo] HTTP error from MoMo API: StatusCode={StatusCode}",
                    (int)response.StatusCode);

                return PaymentGatewayResult.Fail($"MoMo API returned HTTP {(int)response.StatusCode}");
            }

            var momoResponse = JsonSerializer.Deserialize(responseBody, MoMoJsonContext.Default.MoMoCreateResponse);

            if (momoResponse is null)
            {
                _logger.LogError("[MoMo] Failed to deserialize MoMo response");
                return PaymentGatewayResult.Fail("Failed to parse MoMo response");
            }

            if (momoResponse.ResultCode == 0)
            {
                _logger.LogInformation(
                    "[MoMo] Payment created successfully: OrderId={OrderId}, PayUrl={PayUrl}",
                    orderId, momoResponse.PayUrl);

                return PaymentGatewayResult.Ok(momoResponse.PayUrl ?? string.Empty, momoResponse.RequestId);
            }

            _logger.LogWarning(
                "[MoMo] Payment creation failed: ResultCode={ResultCode}, Message={Message}",
                momoResponse.ResultCode, momoResponse.Message);

            return PaymentGatewayResult.Fail(momoResponse.Message ?? $"MoMo error code: {momoResponse.ResultCode}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "[MoMo] Request timed out for OrderId={OrderId}", orderId);
            return PaymentGatewayResult.Fail("MoMo payment request timed out");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[MoMo] Network error for OrderId={OrderId}", orderId);
            return PaymentGatewayResult.Fail($"Network error communicating with MoMo: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MoMo] Unexpected error for OrderId={OrderId}", orderId);
            return PaymentGatewayResult.Fail($"Unexpected error: {ex.Message}");
        }
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

        var expectedSignature = CryptoService.HmacSha256(secretKey, rawData);

        if (!CryptoService.ConstantTimeEquals(expectedSignature, signature))
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

        // Parse the callback data to extract transaction details
        try
        {
            var callbackData = JsonSerializer.Deserialize(rawData, MoMoJsonContext.Default.MoMoCallbackData);

            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = true,
                IsSuccess = callbackData?.ResultCode == 0,
                ReferenceCode = callbackData?.OrderId,
                GatewayTransactionId = callbackData?.TransId?.ToString(),
                ErrorMessage = callbackData?.ResultCode != 0 ? callbackData?.Message : null
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[MoMo] Failed to parse callback data, returning valid with success");

            return Task.FromResult(new PaymentCallbackResult
            {
                IsValid = true,
                IsSuccess = true
            });
        }
    }

    /// <summary>
    /// Verify HMAC-SHA256 signature over sorted callback parameters.
    /// MoMo signature = HMAC_SHA256(secretKey, sorted key=value pairs joined by &amp;, excluding 'signature').
    /// </summary>
    public bool VerifyCallbackSignature(Dictionary<string, string> parameters, string signature)
    {
        var secretKey = _configuration["Payment:MoMo:SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogWarning(
                "[MoMo] Payment:MoMo:SecretKey is not configured. " +
                "Skipping signature verification (development mode)");
            return true;
        }

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("[MoMo] VerifyCallbackSignature rejected: empty or null signature");
            return false;
        }

        // Build raw data from sorted parameters, excluding 'signature'
        var sorted = new SortedDictionary<string, string>(parameters, StringComparer.Ordinal);
        sorted.Remove("signature");

        var rawData = string.Join("&", sorted.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var expectedSignature = CryptoService.HmacSha256(secretKey, rawData);

        return CryptoService.ConstantTimeEquals(expectedSignature, signature);
    }

}

#region MoMo API Models

internal class MoMoCreateRequest
{
    [JsonPropertyName("partnerCode")]
    public string PartnerCode { get; set; } = string.Empty;

    [JsonPropertyName("accessKey")]
    public string AccessKey { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("orderInfo")]
    public string OrderInfo { get; set; } = string.Empty;

    [JsonPropertyName("redirectUrl")]
    public string RedirectUrl { get; set; } = string.Empty;

    [JsonPropertyName("ipnUrl")]
    public string IpnUrl { get; set; } = string.Empty;

    [JsonPropertyName("extraData")]
    public string ExtraData { get; set; } = string.Empty;

    [JsonPropertyName("requestType")]
    public string RequestType { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "vi";
}

internal class MoMoCreateResponse
{
    [JsonPropertyName("partnerCode")]
    public string? PartnerCode { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("payUrl")]
    public string? PayUrl { get; set; }

    [JsonPropertyName("deeplink")]
    public string? Deeplink { get; set; }

    [JsonPropertyName("qrCodeUrl")]
    public string? QrCodeUrl { get; set; }
}

internal class MoMoCallbackData
{
    [JsonPropertyName("partnerCode")]
    public string? PartnerCode { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("transId")]
    public long? TransId { get; set; }

    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }

    [JsonPropertyName("extraData")]
    public string? ExtraData { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

[JsonSerializable(typeof(MoMoCreateRequest))]
[JsonSerializable(typeof(MoMoCreateResponse))]
[JsonSerializable(typeof(MoMoCallbackData))]
internal partial class MoMoJsonContext : JsonSerializerContext
{
}

#endregion
