using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.Faults;

public interface IFaultAppService : IApplicationService
{
    Task<FaultDto> GetAsync(Guid id);
    Task<PagedResultDto<FaultListDto>> GetListAsync(GetFaultListDto input);
    Task<PagedResultDto<FaultListDto>> GetByStationAsync(Guid stationId, GetFaultListDto input);
    Task<FaultDto> UpdateStatusAsync(Guid id, UpdateFaultStatusDto input);
}
