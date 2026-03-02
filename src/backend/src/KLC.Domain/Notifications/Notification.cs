using System;
using KLC.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Notifications;

/// <summary>
/// Represents a notification sent to a user.
/// </summary>
public class Notification : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the user.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Type of notification.
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// Notification title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Notification body/message.
    /// </summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the notification has been read.
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// When the notification was read.
    /// </summary>
    public DateTime? ReadAt { get; private set; }

    /// <summary>
    /// Additional data as JSON (e.g., session ID, station ID).
    /// </summary>
    public string? Data { get; private set; }

    /// <summary>
    /// Deep link action URL for the mobile app.
    /// </summary>
    public string? ActionUrl { get; private set; }

    /// <summary>
    /// Whether push notification was sent.
    /// </summary>
    public bool IsPushSent { get; private set; }

    /// <summary>
    /// When push notification was sent.
    /// </summary>
    public DateTime? PushSentAt { get; private set; }

    protected Notification()
    {
        // Required by EF Core
    }

    public Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? data = null,
        string? actionUrl = null)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
        Data = data;
        ActionUrl = actionUrl;
        IsRead = false;
        IsPushSent = false;
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

    public void MarkAsUnread()
    {
        IsRead = false;
        ReadAt = null;
    }

    public void RecordPushSent()
    {
        IsPushSent = true;
        PushSentAt = DateTime.UtcNow;
    }
}
