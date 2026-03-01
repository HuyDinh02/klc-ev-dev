using System;
using System.Linq;
using System.Threading.Tasks;
using KCharge.Permissions;
using KCharge.Stations;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace KCharge.Notifications;

[Authorize]
public class NotificationAppService : KChargeAppService, INotificationAppService
{
    private readonly IRepository<Notification, Guid> _notificationRepository;
    private readonly IRepository<Alert, Guid> _alertRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;

    public NotificationAppService(
        IRepository<Notification, Guid> notificationRepository,
        IRepository<Alert, Guid> alertRepository,
        IRepository<ChargingStation, Guid> stationRepository)
    {
        _notificationRepository = notificationRepository;
        _alertRepository = alertRepository;
        _stationRepository = stationRepository;
    }

    public async Task<PagedResultDto<NotificationListDto>> GetMyNotificationsAsync(GetNotificationListDto input)
    {
        var userId = CurrentUser.GetId();
        var query = await _notificationRepository.GetQueryableAsync();

        query = query.Where(n => n.UserId == userId);

        if (input.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == input.IsRead.Value);
        }

        if (input.Type.HasValue)
        {
            query = query.Where(n => n.Type == input.Type.Value);
        }

        if (input.Cursor.HasValue)
        {
            query = query.Where(n => n.Id.CompareTo(input.Cursor.Value) > 0);
        }

        query = query.OrderByDescending(n => n.CreationTime);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var notifications = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        var dtos = notifications.Select(n => new NotificationListDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            IsRead = n.IsRead,
            CreatedAt = n.CreationTime
        }).ToList();

        return new PagedResultDto<NotificationListDto>(totalCount, dtos);
    }

    public async Task<NotificationDto> GetAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var notification = await _notificationRepository.GetAsync(id);

        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException();
        }

        return MapToDto(notification);
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var notification = await _notificationRepository.GetAsync(id);

        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException();
        }

        notification.MarkAsRead();
        await _notificationRepository.UpdateAsync(notification);
    }

    public async Task MarkAllAsReadAsync()
    {
        var userId = CurrentUser.GetId();
        var unreadNotifications = await _notificationRepository.GetListAsync(
            n => n.UserId == userId && !n.IsRead);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await _notificationRepository.UpdateManyAsync(unreadNotifications);
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var userId = CurrentUser.GetId();
        var query = await _notificationRepository.GetQueryableAsync();
        return await AsyncExecuter.CountAsync(query.Where(n => n.UserId == userId && !n.IsRead));
    }

    public async Task RegisterDeviceAsync(RegisterDeviceDto input)
    {
        // TODO: Store FCM token for push notifications
        // This would typically involve storing the token in a UserDevice table
        // For now, just acknowledge the request
        await Task.CompletedTask;
    }

    [Authorize(KChargePermissions.Alerts.Default)]
    public async Task<PagedResultDto<AlertDto>> GetAlertsAsync(GetAlertListDto input)
    {
        var query = await _alertRepository.GetQueryableAsync();

        if (input.StationId.HasValue)
        {
            query = query.Where(a => a.StationId == input.StationId.Value);
        }

        if (input.Type.HasValue)
        {
            query = query.Where(a => a.Type == input.Type.Value);
        }

        if (input.Status.HasValue)
        {
            query = query.Where(a => a.Status == input.Status.Value);
        }

        if (input.Cursor.HasValue)
        {
            query = query.Where(a => a.Id.CompareTo(input.Cursor.Value) > 0);
        }

        query = query.OrderByDescending(a => a.CreationTime);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var alerts = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        var stationIds = alerts.Where(a => a.StationId.HasValue).Select(a => a.StationId!.Value).Distinct().ToList();
        var stations = stationIds.Any()
            ? await _stationRepository.GetListAsync(s => stationIds.Contains(s.Id))
            : new System.Collections.Generic.List<ChargingStation>();
        var stationMap = stations.ToDictionary(s => s.Id, s => s.Name);

        var dtos = alerts.Select(a => new AlertDto
        {
            Id = a.Id,
            StationId = a.StationId,
            StationName = a.StationId.HasValue && stationMap.TryGetValue(a.StationId.Value, out var sName) ? sName : null,
            Type = a.Type,
            Title = $"[{a.Type}] Alert",
            Message = a.Message,
            Status = a.Status,
            CreatedAt = a.CreationTime,
            AcknowledgedAt = a.AcknowledgedAt,
            AcknowledgedBy = a.AcknowledgedByUserId
        }).ToList();

        return new PagedResultDto<AlertDto>(totalCount, dtos);
    }

    [Authorize(KChargePermissions.Alerts.Acknowledge)]
    public async Task AcknowledgeAlertAsync(Guid alertId)
    {
        var userId = CurrentUser.GetId();
        var alert = await _alertRepository.GetAsync(alertId);

        alert.Acknowledge(userId);
        await _alertRepository.UpdateAsync(alert);
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type,
            Title = notification.Title,
            Body = notification.Body,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreationTime,
            ReferenceId = null // Could parse from Data JSON if needed
        };
    }
}
