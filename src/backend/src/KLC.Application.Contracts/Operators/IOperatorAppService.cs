using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Operators;

public interface IOperatorAppService : IApplicationService
{
    Task<List<OperatorDto>> GetListAsync(GetOperatorListDto input);
    Task<OperatorDetailDto> GetAsync(Guid id);
    Task<CreateOperatorResultDto> CreateAsync(CreateOperatorDto input);
    Task<OperatorDetailDto> UpdateAsync(Guid id, UpdateOperatorDto input);
    Task DeleteAsync(Guid id);
    Task<OperatorApiKeyDto> RegenerateApiKeyAsync(Guid id);
    Task AddStationAsync(Guid operatorId, Guid stationId);
    Task RemoveStationAsync(Guid operatorId, Guid stationId);
    Task<List<OperatorWebhookLogDto>> GetWebhookLogsAsync(Guid operatorId, GetWebhookLogsDto input);
}
