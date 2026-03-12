using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Notifications;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using NotificationDto = KLC.Driver.Services.NotificationDto;

namespace KLC.BffServices;

/// <summary>
/// Tests for NotificationBffService cache layer behavior.
/// Uses NSubstitute mocks for ICacheService to verify cache interactions.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class NotificationBffServiceCacheTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly NotificationBffService _service;

    public NotificationBffServiceCacheTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = Substitute.For<ICacheService>();
        var logger = Substitute.For<ILogger<NotificationBffService>>();
        _service = new NotificationBffService(_dbContext, _cache, logger);
    }

    [Fact]
    public async Task GetUnreadCount_Should_Return_Cached_Count_On_Hit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"user:{userId}:unread-notifications";

        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<int>>>(), Arg.Any<TimeSpan?>())
            .Returns(5);

        // Act
        var result = await _service.GetUnreadCountAsync(userId);

        // Assert
        result.ShouldBe(5);

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<int>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetUnreadCount_Should_Query_DB_On_Cache_Miss()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            // Create 3 unread and 1 read notification
            for (int i = 0; i < 3; i++)
            {
                var notification = new Notification(
                    Guid.NewGuid(), userId, NotificationType.ChargingCompleted,
                    $"Charging Complete {i}", $"Your session {i} is complete");
                await _dbContext.Notifications.AddAsync(notification);
            }

            var readNotification = new Notification(
                Guid.NewGuid(), userId, NotificationType.PaymentSuccess,
                "Payment Success", "Your payment was processed");
            readNotification.MarkAsRead();
            await _dbContext.Notifications.AddAsync(readNotification);
            await _dbContext.SaveChangesAsync();
        });

        var cacheKey = $"user:{userId}:unread-notifications";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<int>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<int>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetUnreadCountAsync(userId);

            // Assert
            result.ShouldBe(3);
        });
    }

    [Fact]
    public async Task MarkAsRead_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var notification = new Notification(
                notificationId, userId, NotificationType.ChargingCompleted,
                "Charging Complete", "Your session is complete");
            await _dbContext.Notifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            await _service.MarkAsReadAsync(userId, notificationId);
        });

        // Assert - unread count cache invalidated
        await _cache.Received(1).RemoveAsync($"user:{userId}:unread-notifications");
    }

    [Fact]
    public async Task MarkAllAsRead_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                var notification = new Notification(
                    Guid.NewGuid(), userId, NotificationType.ChargingCompleted,
                    $"Notification {i}", $"Body {i}");
                await _dbContext.Notifications.AddAsync(notification);
            }
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            await _service.MarkAllAsReadAsync(userId);
        });

        // Assert - unread count cache invalidated
        await _cache.Received(1).RemoveAsync($"user:{userId}:unread-notifications");
    }

    [Fact]
    public async Task GetNotifications_Should_Bypass_Cache()
    {
        // Arrange - GetNotifications uses cursor-based pagination, no cache
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                var notification = new Notification(
                    Guid.NewGuid(), userId, NotificationType.ChargingCompleted,
                    $"Notification {i}", $"Body {i}");
                await _dbContext.Notifications.AddAsync(notification);
            }
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetNotificationsAsync(userId, null, 10);

            // Assert
            result.ShouldNotBeNull();
            result.Data.Count.ShouldBe(3);
        });

        // Verify cache was NOT called for paginated notifications
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<PagedResult<NotificationDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetUnreadCount_Should_Use_Correct_Cache_Key_Format()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCacheKey = $"user:{userId}:unread-notifications";

        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<int>>>(), Arg.Any<TimeSpan?>())
            .Returns(0);

        // Act
        await _service.GetUnreadCountAsync(userId);

        // Assert
        await _cache.Received(1).GetOrSetAsync(
            expectedCacheKey,
            Arg.Any<Func<Task<int>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task MarkAsRead_Should_Not_Invalidate_Cache_When_Already_Read()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var notification = new Notification(
                notificationId, userId, NotificationType.ChargingCompleted,
                "Already Read", "This notification is already read");
            notification.MarkAsRead();
            await _dbContext.Notifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            await _service.MarkAsReadAsync(userId, notificationId);
        });

        // Assert - cache NOT invalidated because notification was already read
        await _cache.DidNotReceive().RemoveAsync($"user:{userId}:unread-notifications");
    }
}
