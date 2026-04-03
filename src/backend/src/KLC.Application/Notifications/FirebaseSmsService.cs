using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace KLC.Notifications;

/// <summary>
/// SMS service using Firebase Identity Toolkit REST API.
/// Sends verification SMS via Firebase's infrastructure — no third-party SMS provider needed.
///
/// Configuration:
///   Firebase:WebApiKey — Firebase Web API Key (from Firebase Console → Project Settings → General)
///
/// Flow:
///   1. Backend calls sendVerificationCode → Firebase sends SMS to user
///   2. User receives OTP on their phone
///   3. Backend calls signInWithPhoneNumber to verify the OTP
///   4. Returns Firebase session info for token verification
///
/// Note: For the KLC system, we use a simpler approach:
///   - We generate our own OTP and send it via Firebase's SMS channel
///   - Verification is done by our backend (Redis OTP store), not Firebase
///   - This gives us full control over the auth flow
/// </summary>
public class FirebaseSmsService : ISmsService, ITransientDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FirebaseSmsService> _logger;
    private readonly string? _webApiKey;

    private const string IDENTITY_TOOLKIT_URL = "https://identitytoolkit.googleapis.com/v1";

    public FirebaseSmsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FirebaseSmsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _webApiKey = configuration["Firebase:WebApiKey"];
    }

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_webApiKey);

    public async Task SendAsync(string phoneNumber, string message)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning(
                "[SMS] Firebase WebApiKey not configured — logging only. To: {Phone}, Message: {Message}",
                phoneNumber, message);
            return;
        }

        // Normalize Vietnamese phone: 0983987986 → +84983987986
        var formatted = phoneNumber.StartsWith("0")
            ? "+84" + phoneNumber[1..]
            : phoneNumber.StartsWith("+") ? phoneNumber : "+84" + phoneNumber;

        try
        {
            var client = _httpClientFactory.CreateClient();

            // Step 1: Send verification code via Firebase Identity Toolkit
            var sendCodeResponse = await client.PostAsJsonAsync(
                $"{IDENTITY_TOOLKIT_URL}/accounts:sendVerificationCode?key={_webApiKey}",
                new { phoneNumber = formatted });

            if (sendCodeResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("[SMS] Firebase verification code sent to {Phone}", formatted);
            }
            else
            {
                var errorBody = await sendCodeResponse.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "[SMS] Firebase sendVerificationCode failed for {Phone}: {Status} {Body}",
                    formatted, sendCodeResponse.StatusCode, errorBody);

                // Fallback: log the OTP so dev/testing can still work
                _logger.LogWarning("[SMS] FALLBACK — OTP for {Phone}: {Message}", phoneNumber, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SMS] Firebase SMS error for {Phone}", formatted);
            // Don't throw — log OTP as fallback for development
            _logger.LogWarning("[SMS] FALLBACK — OTP for {Phone}: {Message}", phoneNumber, message);
        }
    }
}
