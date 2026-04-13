using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Notifications;

public interface IBroadcastAppService : IApplicationService
{
    Task<BroadcastResultDto> BroadcastAsync(BroadcastNotificationDto input);
    Task<List<BroadcastHistoryDto>> GetBroadcastHistoryAsync(GetBroadcastHistoryDto input);
    Task<BroadcastRecipientsDto> GetBroadcastRecipientsAsync(string title, DateTime sentAt);
}
