using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using KLC.Enums;
using KLC.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;

namespace KLC.Notifications;

/// <summary>
/// Firebase Cloud Messaging push notification service.
/// Sends real push notifications to mobile devices via FCM.
/// </summary>
public class FirebasePushNotificationService : IPushNotificationService, ITransientDependency
{
    private readonly IRepository<DeviceToken, Guid> _deviceTokenRepository;
    private readonly IRepository<NotificationPreference, Guid> _notificationPreferenceRepository;
    private readonly ILogger<FirebasePushNotificationService> _logger;
    private readonly ISettingProvider _settingProvider;

    public FirebasePushNotificationService(
        IRepository<DeviceToken, Guid> deviceTokenRepository,
        IRepository<NotificationPreference, Guid> notificationPreferenceRepository,
        ILogger<FirebasePushNotificationService> logger,
        IConfiguration configuration,
        ISettingProvider settingProvider)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _notificationPreferenceRepository = notificationPreferenceRepository;
        _logger = logger;
        _settingProvider = settingProvider;

        // Initialize Firebase App once
        if (FirebaseApp.DefaultInstance == null)
        {
            var credentialPath = configuration["Firebase:CredentialPath"];
            if (!string.IsNullOrEmpty(credentialPath) && System.IO.File.Exists(credentialPath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialPath)
                });
                _logger.LogInformation("Firebase initialized from credential file: {Path}", credentialPath);
            }
            else
            {
                // Try Application Default Credentials (works in GCP environments)
                try
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.GetApplicationDefault()
                    });
                    _logger.LogInformation("Firebase initialized with Application Default Credentials");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Firebase not initialized — push notifications will be logged only");
                }
            }
        }
    }

    public async Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, NotificationType? notificationType = null)
    {
        // Check user's notification preferences if a type is specified
        if (notificationType.HasValue && notificationType.Value != NotificationType.SystemAnnouncement)
        {
            if (!await IsNotificationEnabledForUserAsync(userId, notificationType.Value))
            {
                _logger.LogDebug(
                    "Push skipped for user {UserId}: notification type {Type} is disabled by preference",
                    userId, notificationType.Value);
                return;
            }
        }

        var tokens = await GetActiveTokensForUserAsync(userId);
        if (tokens.Count == 0)
        {
            _logger.LogDebug("No device tokens for user {UserId}, skipping push", userId);
            return;
        }

        await SendToDevicesAsync(tokens, title, body, data);
    }

    public async Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null)
    {
        var queryable = await _deviceTokenRepository.GetQueryableAsync();
        var userIdList = userIds.ToList();

        var tokens = queryable
            .Where(d => userIdList.Contains(d.UserId) && d.IsActive)
            .Select(d => d.Token)
            .ToList();

        if (tokens.Count == 0)
        {
            _logger.LogDebug("No device tokens for {Count} users, skipping push", userIdList.Count);
            return;
        }

        await SendToDevicesAsync(tokens, title, body, data);
    }

    public async Task SendToDevicesAsync(IEnumerable<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        // Check if push notifications are enabled in system settings
        var pushEnabled = await _settingProvider.GetOrNullAsync(Settings.KLCSettings.Notifications.PushEnabled);
        if (pushEnabled?.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger.LogDebug("[FCM] Push notifications disabled in settings. Skipping: Title={Title}", title);
            return;
        }

        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("[FCM] Firebase not initialized. Logging only: Title={Title}, Body={Body}", title, body);
            return;
        }

        var tokenList = deviceTokens.ToList();
        if (tokenList.Count == 0) return;

        var message = new MulticastMessage
        {
            Tokens = tokenList,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body
            },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    Sound = "default",
                    ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                }
            },
            Apns = new ApnsConfig
            {
                Aps = new Aps
                {
                    Sound = "default",
                    Badge = 1
                }
            }
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
            _logger.LogInformation(
                "[FCM] Sent: {Success}/{Total}, Title={Title}",
                response.SuccessCount, tokenList.Count, title);

            // Handle failed tokens (invalid/expired)
            if (response.FailureCount > 0)
            {
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        var error = response.Responses[i].Exception;
                        if (error?.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                        {
                            _logger.LogInformation("[FCM] Deactivating invalid token: {Token}", tokenList[i][..Math.Min(20, tokenList[i].Length)]);
                            await DeactivateTokenAsync(tokenList[i]);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FCM] Failed to send push notification: Title={Title}", title);
        }
    }

    /// <summary>
    /// Checks the user's notification preferences for the given notification type.
    /// Returns true if the notification should be sent (preference enabled or no preference record found).
    /// </summary>
    private async Task<bool> IsNotificationEnabledForUserAsync(Guid userId, NotificationType notificationType)
    {
        var queryable = await _notificationPreferenceRepository.GetQueryableAsync();
        var prefs = queryable.FirstOrDefault(p => p.UserId == userId);

        // No preference record means all defaults are enabled
        if (prefs == null)
            return true;

        return notificationType switch
        {
            NotificationType.ChargingStarted => prefs.ChargingComplete,
            NotificationType.ChargingCompleted => prefs.ChargingComplete,
            NotificationType.PaymentSuccess => prefs.PaymentAlerts,
            NotificationType.PaymentFailed => prefs.PaymentAlerts,
            NotificationType.WalletTopUp => prefs.PaymentAlerts,
            NotificationType.Promotion => prefs.Promotions,
            _ => true // EInvoiceReady, ChargingFailed, etc. — always send
        };
    }

    private async Task<List<string>> GetActiveTokensForUserAsync(Guid userId)
    {
        var queryable = await _deviceTokenRepository.GetQueryableAsync();
        return queryable
            .Where(d => d.UserId == userId && d.IsActive)
            .Select(d => d.Token)
            .ToList();
    }

    private async Task DeactivateTokenAsync(string token)
    {
        var queryable = await _deviceTokenRepository.GetQueryableAsync();
        var deviceToken = queryable.FirstOrDefault(d => d.Token == token);
        if (deviceToken != null)
        {
            deviceToken.Deactivate();
            await _deviceTokenRepository.UpdateAsync(deviceToken);
        }
    }
}
