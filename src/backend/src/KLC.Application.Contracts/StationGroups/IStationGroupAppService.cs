using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.StationGroups;

public interface IStationGroupAppService : IApplicationService
{
    Task<StationGroupDto> CreateAsync(CreateStationGroupDto input);

    Task<PagedResultDto<StationGroupListDto>> GetListAsync(GetStationGroupListDto input);

    Task<StationGroupDetailDto> GetAsync(Guid id);

    Task<StationGroupDto> UpdateAsync(Guid id, UpdateStationGroupDto input);

    Task DeleteAsync(Guid id);

    Task AssignStationAsync(Guid groupId, AssignStationDto input);

    Task UnassignStationAsync(Guid groupId, Guid stationId);
}
