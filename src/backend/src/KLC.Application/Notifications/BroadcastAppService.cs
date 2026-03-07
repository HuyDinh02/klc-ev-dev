using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Permissions;
using KLC.Users;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;

namespace KLC.Notifications;

[Authorize(KLCPermissions.Notifications.Default)]
public class BroadcastAppService : KLCAppService, IBroadcastAppService
{
    private readonly IRepository<Notification, Guid> _notificationRepository;
    private readonly IRepository<AppUser, Guid> _userRepository;
    private readonly IPushNotificationService _pushNotificationService;

    public BroadcastAppService(
        IRepository<Notification, Guid> notificationRepository,
        IRepository<AppUser, Guid> userRepository,
        IPushNotificationService pushNotificationService)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _pushNotificationService = pushNotificationService;
    }

    [Authorize(KLCPermissions.Notifications.Broadcast)]
    public async Task<BroadcastResultDto> BroadcastAsync(BroadcastNotificationDto input)
    {
        var userQuery = await _userRepository.GetQueryableAsync();
        var userIds = await AsyncExecuter.ToListAsync(
            userQuery
                .Where(u => u.IsActive && !u.IsDeleted)
                .Select(u => u.IdentityUserId));

        var notifications = userIds.Select(userId => new Notification(
            GuidGenerator.Create(),
            userId,
            input.Type,
            input.Title,
            input.Body,
            input.Data,
            input.ActionUrl
        )).ToList();

        await _notificationRepository.InsertManyAsync(notifications);

        // Send push notifications to all recipients
        await _pushNotificationService.SendToUsersAsync(
            userIds,
            input.Title,
            input.Body,
            input.Data != null ? new Dictionary<string, string> { ["data"] = input.Data } : null);

        return new BroadcastResultDto
        {
            Message = "Broadcast sent",
            RecipientCount = notifications.Count
        };
    }

    public async Task<List<BroadcastHistoryDto>> GetBroadcastHistoryAsync(GetBroadcastHistoryDto input)
    {
        var pageSize = input.PageSize;
        if (pageSize <= 0 || pageSize > 50) pageSize = 20;

        var query = await _notificationRepository.GetQueryableAsync();
        var notifications = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(n => n.CreationTime).Take(pageSize * 10));

        var broadcasts = notifications
            .GroupBy(n => new { n.Title, Minute = n.CreationTime.ToString("yyyyMMddHHmmss") })
            .Select(g => new BroadcastHistoryDto
            {
                Title = g.Key.Title,
                Body = g.First().Body,
                Type = g.First().Type,
                RecipientCount = g.Count(),
                SentAt = g.First().CreationTime
            })
            .Take(pageSize)
            .ToList();

        return broadcasts;
    }
}
