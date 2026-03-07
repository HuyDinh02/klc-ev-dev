using System;
using System.Threading.Tasks;
using KLC.MobileUsers;
using Volo.Abp.Application.Services;

namespace KLC.Feedback;

public interface IFeedbackAdminAppService : IApplicationService
{
    Task<CursorPagedResultDto<FeedbackListDto>> GetListAsync(GetFeedbackListDto input);
    Task<FeedbackDetailDto> GetAsync(Guid id);
    Task RespondAsync(Guid id, RespondToFeedbackDto input);
    Task CloseAsync(Guid id);
}
