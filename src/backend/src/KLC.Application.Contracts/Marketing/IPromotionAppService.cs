using System;
using System.Threading.Tasks;
using KLC.MobileUsers;
using Volo.Abp.Application.Services;

namespace KLC.Marketing;

public interface IPromotionAppService : IApplicationService
{
    Task<CursorPagedResultDto<PromotionListDto>> GetListAsync(GetPromotionListDto input);
    Task<PromotionDetailDto> GetAsync(Guid id);
    Task<CreatePromotionResultDto> CreateAsync(CreatePromotionDto input);
    Task UpdateAsync(Guid id, UpdatePromotionDto input);
    Task DeleteAsync(Guid id);
}
