using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace KLC.Notifications;

/// <summary>
/// SMS service with multi-provider support.
/// Configure via appsettings:
///   "Sms": {
///     "Provider": "eSMS",        // "eSMS" | "SpeedSMS" | "Log" (default)
///     "ApiKey": "...",
///     "SecretKey": "...",
///     "BrandName": "KLC Energy"  // Sender name (registered with provider)
///   }
///
/// For dev/testing: Provider="Log" (default) — OTP logged to Cloud Logging.
/// For production: Register at esms.vn or speedsms.vn, get API credentials.
/// </summary>
public class SmsService : ISmsService, ITransientDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmsService> _logger;
    private readonly string _provider;
    private readonly string? _apiKey;
    private readonly string? _secretKey;
    private readonly string? _brandName;

    public SmsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SmsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _provider = configuration["Sms:Provider"] ?? "Log";
        _apiKey = configuration["Sms:ApiKey"];
        _secretKey = configuration["Sms:SecretKey"];
        _brandName = configuration["Sms:BrandName"] ?? "KLC Energy";
    }

    public async Task SendAsync(string phoneNumber, string message)
    {
        // Normalize Vietnamese phone: 0983987986 → +84983987986 → 84983987986
        var normalized = phoneNumber.StartsWith("0")
            ? "84" + phoneNumber[1..]
            : phoneNumber.Replace("+", "");

        switch (_provider.ToLower())
        {
            case "esms":
                await SendViaEsmsAsync(normalized, message);
                break;
            case "speedsms":
                await SendViaSpeedSmsAsync(normalized, message);
                break;
            default:
                _logger.LogWarning(
                    "[SMS] Provider={Provider} — OTP logged only. To: {Phone}, Message: {Message}",
                    _provider, phoneNumber, message);
                break;
        }
    }

    /// <summary>
    /// eSMS.vn — Vietnamese SMS provider
    /// API: https://developers.esms.vn/en/esms-api/send-sms-api
    /// </summary>
    private async Task SendViaEsmsAsync(string phone, string message)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_secretKey))
        {
            _logger.LogWarning("[SMS] eSMS credentials not configured. OTP: {Message} → {Phone}", message, phone);
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(
                "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post",
                new
                {
                    ApiKey = _apiKey,
                    SecretKey = _secretKey,
                    Phone = phone,
                    Content = message,
                    SmsType = 2, // OTP type
                    Brandname = _brandName,
                    IsUnicode = 0
                });

            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[SMS] eSMS sent to {Phone}: {Response}", phone, body);
            }
            else
            {
                _logger.LogError("[SMS] eSMS failed for {Phone}: {Status} {Body}",
                    phone, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SMS] eSMS error for {Phone}", phone);
        }
    }

    /// <summary>
    /// SpeedSMS.vn — Vietnamese SMS provider
    /// API: https://speedsms.vn/sms-api-service/
    /// </summary>
    private async Task SendViaSpeedSmsAsync(string phone, string message)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("[SMS] SpeedSMS credentials not configured. OTP: {Message} → {Phone}", message, phone);
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization",
                "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_apiKey}:x")));

            var response = await client.PostAsJsonAsync(
                "https://api.speedsms.vn/index.php/sms/send",
                new
                {
                    to = new[] { phone },
                    content = message,
                    sms_type = 5, // OTP type
                    sender = _brandName
                });

            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[SMS] SpeedSMS sent to {Phone}: {Response}", phone, body);
            }
            else
            {
                _logger.LogError("[SMS] SpeedSMS failed for {Phone}: {Status} {Body}",
                    phone, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SMS] SpeedSMS error for {Phone}", phone);
        }
    }
}
