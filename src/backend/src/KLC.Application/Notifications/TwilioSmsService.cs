using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace KLC.Notifications;

/// <summary>
/// Production SMS service using Twilio REST API.
/// Configuration:
///   Twilio:AccountSid, Twilio:AuthToken, Twilio:FromNumber
/// Falls back to log-only when Twilio is not configured.
/// </summary>
public class TwilioSmsService : ISmsService, ITransientDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly string? _accountSid;
    private readonly string? _authToken;
    private readonly string? _fromNumber;

    public TwilioSmsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TwilioSmsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _accountSid = configuration["Twilio:AccountSid"];
        _authToken = configuration["Twilio:AuthToken"];
        _fromNumber = configuration["Twilio:FromNumber"];
    }

    private bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_accountSid) &&
        !string.IsNullOrWhiteSpace(_authToken) &&
        !string.IsNullOrWhiteSpace(_fromNumber);

    public async Task SendAsync(string phoneNumber, string message)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning(
                "[SMS] Twilio not configured — logging only. To: {Phone}, Message: {Message}",
                phoneNumber, message);
            return;
        }

        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json";

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var content = new FormUrlEncodedContent(
        [
            new("To", phoneNumber),
            new("From", _fromNumber!),
            new("Body", message),
        ]);

        try
        {
            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[SMS] Sent to {Phone} via Twilio", phoneNumber);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "[SMS] Twilio failed. Status: {Status}, Body: {Body}",
                    response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SMS] Failed to send to {Phone} via Twilio", phoneNumber);
            throw;
        }
    }
}
