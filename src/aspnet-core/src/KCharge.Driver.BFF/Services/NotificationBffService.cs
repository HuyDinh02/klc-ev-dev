using KCharge.EntityFrameworkCore;
using KCharge.Notifications;
using Microsoft.EntityFrameworkCore;

namespace KCharge.Driver.Services;

public interface INotificationBffService
{
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, Guid? cursor, int pageSize);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid userId, Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
    Task RegisterDeviceAsync(Guid userId, string fcmToken);
}

public class NotificationBffService : INotificationBffService
{
    private readonly KChargeDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<NotificationBffService> _logger;

    public NotificationBffService(
        KChargeDbContext dbContext,
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
        var cacheKey = $"user:{userId}:unread-notifications";

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
            await _cache.RemoveAsync($"user:{userId}:unread-notifications");
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
        await _cache.RemoveAsync($"user:{userId}:unread-notifications");
    }

    public async Task RegisterDeviceAsync(Guid userId, string fcmToken)
    {
        var user = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

        if (user != null)
        {
            user.UpdateFcmToken(fcmToken);
            await _dbContext.SaveChangesAsync();
        }
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
