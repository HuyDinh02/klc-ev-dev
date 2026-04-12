using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.MobileUsers;
using Volo.Abp.Application.Services;

namespace KLC.Marketing;

public interface IVoucherAppService : IApplicationService
{
    Task<CursorPagedResultDto<VoucherListDto>> GetListAsync(GetVoucherListDto input);
    Task<VoucherDetailDto> GetAsync(Guid id);
    Task<CreateVoucherResultDto> CreateAsync(CreateVoucherDto input);
    Task UpdateAsync(Guid id, UpdateVoucherDto input);
    Task DeleteAsync(Guid id);
    Task<VoucherUsageResultDto> GetUsageAsync(Guid id);
    Task<BulkCreateVoucherResultDto> BulkCreateAsync(BulkCreateVoucherDto input);
    Task<List<ExportVoucherDto>> ExportAsync();
}
