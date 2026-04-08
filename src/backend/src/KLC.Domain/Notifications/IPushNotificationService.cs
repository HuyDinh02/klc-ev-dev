using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Enums;

namespace KLC.Notifications;

/// <summary>
/// Service for sending push notifications to mobile devices via FCM.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Send a push notification to a specific user's devices.
    /// If notificationType is provided, the user's notification preferences are checked first.
    /// </summary>
    Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, NotificationType? notificationType = null);

    /// <summary>
    /// Send a push notification to multiple users (broadcast — always sends, no preference check).
    /// </summary>
    Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string body, Dictionary<string, string>? data = null);

    /// <summary>
    /// Send a push notification to specific device tokens.
    /// </summary>
    Task SendToDevicesAsync(IEnumerable<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null);
}
