using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.Notifications;

public interface INotificationAppService : IApplicationService
{
    Task<PagedResultDto<NotificationListDto>> GetMyNotificationsAsync(GetNotificationListDto input);
    Task<NotificationDto> GetAsync(Guid id);
    Task MarkAsReadAsync(Guid id);
    Task MarkAllAsReadAsync();
    Task<int> GetUnreadCountAsync();
    Task RegisterDeviceAsync(RegisterDeviceDto input);

    // Admin endpoints
    Task<PagedResultDto<AlertDto>> GetAlertsAsync(GetAlertListDto input);
    Task AcknowledgeAlertAsync(Guid alertId);
}
