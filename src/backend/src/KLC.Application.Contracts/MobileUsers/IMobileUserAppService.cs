using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.MobileUsers;

public interface IMobileUserAppService : IApplicationService
{
    Task<CursorPagedResultDto<MobileUserListDto>> GetListAsync(GetMobileUserListDto input);
    Task<MobileUserDetailDto> GetAsync(Guid id);
    Task SuspendAsync(Guid id);
    Task UnsuspendAsync(Guid id);
    Task<CursorPagedResultDto<MobileUserSessionDto>> GetSessionsAsync(Guid id, GetMobileUserSessionsDto input);
    Task<CursorPagedResultDto<MobileUserTransactionDto>> GetTransactionsAsync(Guid id, GetMobileUserTransactionsDto input);
    Task<WalletAdjustResultDto> AdjustWalletAsync(Guid id, WalletAdjustDto input);
    Task<MobileUserStatisticsDto> GetStatisticsAsync();
}
