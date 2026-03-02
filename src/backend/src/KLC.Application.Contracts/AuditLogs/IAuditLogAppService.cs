using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.AuditLogs;

public interface IAuditLogAppService : IApplicationService
{
    Task<PagedResultDto<AuditLogListDto>> GetListAsync(GetAuditLogListDto input);

    Task<AuditLogDto> GetAsync(Guid id);

    Task<PagedResultDto<EntityChangeDto>> GetEntityChangesAsync(GetEntityChangesDto input);

    Task<List<EntityPropertyChangeDto>> GetPropertyChangesAsync(Guid entityChangeId);

    Task<byte[]> ExportToCsvAsync(GetAuditLogListDto input);
}
