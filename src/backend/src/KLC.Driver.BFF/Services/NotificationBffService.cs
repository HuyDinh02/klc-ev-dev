using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Notifications;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Services;

public interface INotificationBffService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, Guid? cursor, int pageSize);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid userId, Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
    Task RegisterDeviceAsync(Guid userId, string fcmToken, DevicePlatform platform);
    Task UnregisterDeviceAsync(Guid userId, string token);
    Task<NotificationPreferenceResultDto> GetPreferencesAsync(Guid userId);
    Task<NotificationPreferenceResultDto> UpdatePreferencesAsync(Guid userId, Endpoints.UpdateNotificationPreferenceRequest request);
}

public class NotificationBffService : INotificationBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<NotificationBffService> _logger;

    public NotificationBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<NotificationBffService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, Guid? cursor, int pageSize)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId )
            .OrderByDescending(n => n.CreationTime);

        if (cursor.HasValue)
        {
            var cursorNotification = await _dbContext.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == cursor.Value);

            if (cursorNotification != null)
            {
                query = (IOrderedQueryable<Notification>)query
                    .Where(n => n.CreationTime < cursorNotification.CreationTime);
            }
        }

        var notifications = await query
            .Take(pageSize + 1)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Body = n.Body,
                Data = n.Data,
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreationTime
            })
            .ToListAsync();

        var hasMore = notifications.Count > pageSize;
        var items = hasMore ? notifications.Take(pageSize).ToList() : notifications;
        var nextCursor = hasMore && items.Any() ? items.Last().Id : (Guid?)null;

        return new PagedResult<NotificationDto>
        {
            Data = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        var cacheKey = CacheKeys.UserUnreadNotifications(userId);

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            return await _dbContext.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead );
        }, TimeSpan.FromMinutes(1));
    }

    public async Task MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null && !notification.IsRead)
        {
            notification.MarkAsRead();
            await _dbContext.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKeys.UserUnreadNotifications(userId));
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead )
            .ToListAsync();

        foreach (var notification in unread)
        {
            notification.MarkAsRead();
        }

        await _dbContext.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.UserUnreadNotifications(userId));
    }

    public async Task RegisterDeviceAsync(Guid userId, string fcmToken, DevicePlatform platform)
    {
        try
        {
            // Store in DeviceToken table for multi-device support
            var existing = await _dbContext.DeviceTokens
                .FirstOrDefaultAsync(d => d.Token == fcmToken);

            if (existing != null)
            {
                existing.UpdateToken(fcmToken);
            }
            else
            {
                var deviceToken = new Users.DeviceToken(
                    Guid.NewGuid(), userId, fcmToken, platform);
                await _dbContext.DeviceTokens.AddAsync(deviceToken);
            }

            // Also update legacy FcmToken on AppUser
            var user = await _dbContext.AppUsers
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);
            if (user != null)
            {
                user.UpdateFcmToken(fcmToken);
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device for user {UserId}", userId);
        }
    }

    public async Task UnregisterDeviceAsync(Guid userId, string token)
    {
        var deviceToken = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(d => d.Token == token && d.UserId == userId);

        if (deviceToken != null)
        {
            deviceToken.Deactivate();
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<NotificationPreferenceResultDto> GetPreferencesAsync(Guid userId)
    {
        var prefs = await _dbContext.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs == null)
        {
            // Return defaults
            return new NotificationPreferenceResultDto
            {
                ChargingComplete = true,
                PaymentAlerts = true,
                FaultAlerts = true,
                Promotions = true
            };
        }

        return new NotificationPreferenceResultDto
        {
            ChargingComplete = prefs.ChargingComplete,
            PaymentAlerts = prefs.PaymentAlerts,
            FaultAlerts = prefs.FaultAlerts,
            Promotions = prefs.Promotions
        };
    }

    public async Task<NotificationPreferenceResultDto> UpdatePreferencesAsync(
        Guid userId, Endpoints.UpdateNotificationPreferenceRequest request)
    {
        var prefs = await _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs == null)
        {
            prefs = new Notifications.NotificationPreference(Guid.NewGuid(), userId);
            await _dbContext.NotificationPreferences.AddAsync(prefs);
        }

        prefs.Update(request.ChargingComplete, request.PaymentAlerts, request.FaultAlerts, request.Promotions);
        await _dbContext.SaveChangesAsync();

        return new NotificationPreferenceResultDto
        {
            ChargingComplete = prefs.ChargingComplete,
            PaymentAlerts = prefs.PaymentAlerts,
            FaultAlerts = prefs.FaultAlerts,
            Promotions = prefs.Promotions
        };
    }
}

// DTOs
public record NotificationDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? Data { get; init; }
    public string? ActionUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record NotificationPreferenceResultDto
{
    public bool ChargingComplete { get; init; }
    public bool PaymentAlerts { get; init; }
    public bool FaultAlerts { get; init; }
    public bool Promotions { get; init; }
}
