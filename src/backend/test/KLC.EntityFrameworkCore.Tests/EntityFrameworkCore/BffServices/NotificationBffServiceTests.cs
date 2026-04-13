using System;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Notifications;
using KLC.TestDoubles;
using KLC.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class NotificationBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly NotificationBffService _service;

    public NotificationBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        var cache = new PassthroughCacheService();
        var logger = Substitute.For<ILogger<NotificationBffService>>();
        _service = new NotificationBffService(_dbContext, cache, logger);
    }

    [Fact]
    public async Task GetNotifications_Should_Return_User_Notifications()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _dbContext.Notifications.AddRangeAsync(
                new Notification(Guid.NewGuid(), userId, NotificationType.ChargingCompleted, "Charging done", "Your session is complete"),
                new Notification(Guid.NewGuid(), userId, NotificationType.PaymentSuccess, "Payment received", "50,000đ charged"),
                new Notification(Guid.NewGuid(), Guid.NewGuid(), NotificationType.SystemAnnouncement, "Other user", "Not mine"));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetNotificationsAsync(userId, null, 10);

            result.Data.Count.ShouldBe(2);
            result.HasMore.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetNotifications_Should_Return_Empty_When_None()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetNotificationsAsync(Guid.NewGuid(), null, 10);

            result.Data.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task GetNotifications_Should_Paginate()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            for (int i = 0; i < 5; i++)
            {
                await _dbContext.Notifications.AddAsync(
                    new Notification(Guid.NewGuid(), userId, NotificationType.SystemAnnouncement, $"Notification {i}", $"Body {i}"));
            }
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetNotificationsAsync(userId, null, 3);

            result.Data.Count.ShouldBe(3);
            result.HasMore.ShouldBeTrue();
            result.NextCursor.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task GetUnreadCount_Should_Return_Count()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var n1 = new Notification(Guid.NewGuid(), userId, NotificationType.ChargingCompleted, "N1", "Body 1");
            var n2 = new Notification(Guid.NewGuid(), userId, NotificationType.PaymentSuccess, "N2", "Body 2");
            var n3 = new Notification(Guid.NewGuid(), userId, NotificationType.SystemAnnouncement, "N3", "Body 3");
            n3.MarkAsRead();
            await _dbContext.Notifications.AddRangeAsync(n1, n2, n3);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var count = await _service.GetUnreadCountAsync(userId);
            count.ShouldBe(2);
        });
    }

    [Fact]
    public async Task MarkAsRead_Should_Mark_Single_Notification()
    {
        var userId = Guid.NewGuid();
        var notifId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _dbContext.Notifications.AddAsync(
                new Notification(notifId, userId, NotificationType.SystemAnnouncement, "Test", "Body"));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _service.MarkAsReadAsync(userId, notifId);
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var notif = await _dbContext.Notifications.FirstAsync(n => n.Id == notifId);
            notif.IsRead.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task MarkAllAsRead_Should_Mark_All_Unread()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _dbContext.Notifications.AddRangeAsync(
                new Notification(Guid.NewGuid(), userId, NotificationType.SystemAnnouncement, "N1", "B1"),
                new Notification(Guid.NewGuid(), userId, NotificationType.SystemAnnouncement, "N2", "B2"),
                new Notification(Guid.NewGuid(), userId, NotificationType.SystemAnnouncement, "N3", "B3"));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _service.MarkAllAsReadAsync(userId);
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var count = await _service.GetUnreadCountAsync(userId);
            count.ShouldBe(0);
        });
    }

    [Fact]
    public async Task RegisterDevice_Should_Create_Token()
    {
        var userId = Guid.NewGuid();

        // Create AppUser first (needed for UpdateFcmToken)
        await WithUnitOfWorkAsync(async () =>
        {
            var user = new AppUser(Guid.NewGuid(), userId, "Test User");
            await _dbContext.AppUsers.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _service.RegisterDeviceAsync(userId, "fcm_token_123", KLC.Enums.DevicePlatform.Android);
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var token = await _dbContext.DeviceTokens.FirstOrDefaultAsync(d => d.Token == "fcm_token_123");
            token.ShouldNotBeNull();
            token!.UserId.ShouldBe(userId);
            token.IsActive.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task UnregisterDevice_Should_Deactivate_Token()
    {
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            await _dbContext.DeviceTokens.AddAsync(
                new DeviceToken(tokenId, userId, "fcm_remove_me", DevicePlatform.Android));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _service.UnregisterDeviceAsync(userId, "fcm_remove_me");
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var token = await _dbContext.DeviceTokens.FirstAsync(d => d.Id == tokenId);
            token.IsActive.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task GetPreferences_Should_Return_Defaults_When_None()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetPreferencesAsync(Guid.NewGuid());

            result.ChargingComplete.ShouldBeTrue();
            result.PaymentAlerts.ShouldBeTrue();
            result.FaultAlerts.ShouldBeTrue();
            result.Promotions.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task UpdatePreferences_Should_Create_And_Update()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.UpdatePreferencesAsync(userId,
                new KLC.Driver.Endpoints.UpdateNotificationPreferenceRequest
                {
                    ChargingComplete = true,
                    PaymentAlerts = false,
                    FaultAlerts = true,
                    Promotions = false
                });

            result.PaymentAlerts.ShouldBeFalse();
            result.Promotions.ShouldBeFalse();
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var prefs = await _dbContext.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);
            prefs.ShouldNotBeNull();
            prefs!.PaymentAlerts.ShouldBeFalse();
            prefs.Promotions.ShouldBeFalse();
        });
    }

}
