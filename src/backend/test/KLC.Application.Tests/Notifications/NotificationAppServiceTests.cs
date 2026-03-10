using System;
using KLC.Enums;
using KLC.Notifications;
using Shouldly;
using Xunit;

namespace KLC.Notifications;

/// <summary>
/// Tests for notification business logic exercised by NotificationAppService.
/// Validates domain rules for notification lifecycle and read/unread management.
/// </summary>
public class NotificationAppServiceTests
{
    private static Notification CreateTestNotification(
        Guid? userId = null,
        NotificationType type = NotificationType.ChargingCompleted,
        string title = "Test Notification",
        string body = "Test notification body")
    {
        return new Notification(
            Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            type,
            title,
            body);
    }

    [Fact]
    public void CreateNotification_Should_Set_Default_Values()
    {
        var userId = Guid.NewGuid();

        var notification = new Notification(
            Guid.NewGuid(),
            userId,
            NotificationType.ChargingStarted,
            "Charging Started",
            "Your charging session has started at KC-HN-001",
            data: """{"sessionId":"abc123"}""",
            actionUrl: "klc://session/abc123");

        notification.UserId.ShouldBe(userId);
        notification.Type.ShouldBe(NotificationType.ChargingStarted);
        notification.Title.ShouldBe("Charging Started");
        notification.Body.ShouldBe("Your charging session has started at KC-HN-001");
        notification.Data.ShouldBe("""{"sessionId":"abc123"}""");
        notification.ActionUrl.ShouldBe("klc://session/abc123");
        notification.IsRead.ShouldBeFalse();
        notification.ReadAt.ShouldBeNull();
        notification.IsPushSent.ShouldBeFalse();
        notification.PushSentAt.ShouldBeNull();
    }

    [Fact]
    public void MarkAsRead_Should_Set_IsRead_And_ReadAt()
    {
        var notification = CreateTestNotification();
        notification.IsRead.ShouldBeFalse();

        notification.MarkAsRead();

        notification.IsRead.ShouldBeTrue();
        notification.ReadAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkAsRead_Twice_Should_Not_Change_ReadAt()
    {
        var notification = CreateTestNotification();

        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;

        // Second call should not update ReadAt (idempotent)
        notification.MarkAsRead();
        notification.ReadAt.ShouldBe(firstReadAt);
    }

    [Fact]
    public void MarkAsUnread_Should_Clear_Read_Status()
    {
        var notification = CreateTestNotification();
        notification.MarkAsRead();
        notification.IsRead.ShouldBeTrue();

        notification.MarkAsUnread();

        notification.IsRead.ShouldBeFalse();
        notification.ReadAt.ShouldBeNull();
    }

    [Fact]
    public void RecordPushSent_Should_Set_PushSent_Fields()
    {
        var notification = CreateTestNotification();

        notification.RecordPushSent();

        notification.IsPushSent.ShouldBeTrue();
        notification.PushSentAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkAllAsRead_Simulated_BatchUpdate()
    {
        var userId = Guid.NewGuid();
        var notifications = new[]
        {
            CreateTestNotification(userId: userId, type: NotificationType.ChargingStarted),
            CreateTestNotification(userId: userId, type: NotificationType.ChargingCompleted),
            CreateTestNotification(userId: userId, type: NotificationType.PaymentSuccess)
        };

        // Simulate batch mark as read (as NotificationAppService.MarkAllAsReadAsync does)
        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        foreach (var notification in notifications)
        {
            notification.IsRead.ShouldBeTrue();
            notification.ReadAt.ShouldNotBeNull();
        }
    }

    [Fact]
    public void GetUnreadCount_Simulated_Filter()
    {
        var userId = Guid.NewGuid();
        var notifications = new[]
        {
            CreateTestNotification(userId: userId),
            CreateTestNotification(userId: userId),
            CreateTestNotification(userId: userId)
        };

        // Mark one as read
        notifications[0].MarkAsRead();

        // Simulate unread count filter
        var unreadCount = 0;
        foreach (var n in notifications)
        {
            if (!n.IsRead) unreadCount++;
        }
        unreadCount.ShouldBe(2);
    }

    [Fact]
    public void NotificationTypes_Should_Cover_All_Scenarios()
    {
        // Verify all notification types can be created
        var types = new[]
        {
            NotificationType.ChargingStarted,
            NotificationType.ChargingCompleted,
            NotificationType.ChargingFailed,
            NotificationType.PaymentSuccess,
            NotificationType.PaymentFailed,
            NotificationType.EInvoiceReady,
            NotificationType.WalletTopUp,
            NotificationType.Promotion,
            NotificationType.SystemAnnouncement
        };

        foreach (var type in types)
        {
            var notification = new Notification(
                Guid.NewGuid(),
                Guid.NewGuid(),
                type,
                $"Title for {type}",
                $"Body for {type}");

            notification.Type.ShouldBe(type);
        }
    }

    [Fact]
    public void Notification_Should_Preserve_UserId()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var notification1 = CreateTestNotification(userId: user1);
        var notification2 = CreateTestNotification(userId: user2);

        notification1.UserId.ShouldBe(user1);
        notification2.UserId.ShouldBe(user2);
        notification1.UserId.ShouldNotBe(notification2.UserId);
    }

    [Fact]
    public void Notification_Without_Optional_Fields_Should_Have_Nulls()
    {
        var notification = new Notification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationType.SystemAnnouncement,
            "System Update",
            "Scheduled maintenance tonight");

        notification.Data.ShouldBeNull();
        notification.ActionUrl.ShouldBeNull();
    }
}
