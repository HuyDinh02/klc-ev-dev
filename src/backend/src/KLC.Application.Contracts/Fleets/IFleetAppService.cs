using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.Fleets;

public interface IFleetAppService : IApplicationService
{
    Task<PagedResultDto<FleetDto>> GetListAsync(GetFleetListDto input);
    Task<FleetDetailDto> GetAsync(Guid id);
    Task<FleetDto> CreateAsync(CreateFleetDto input);
    Task<FleetDto> UpdateAsync(Guid id, UpdateFleetDto input);
    Task DeleteAsync(Guid id);

    Task<FleetVehicleDto> AddVehicleAsync(Guid fleetId, AddFleetVehicleDto input);
    Task RemoveVehicleAsync(Guid fleetId, Guid vehicleId);

    Task<List<FleetChargingScheduleDto>> GetSchedulesAsync(Guid fleetId);
    Task<FleetChargingScheduleDto> AddScheduleAsync(Guid fleetId, CreateFleetScheduleDto input);
    Task RemoveScheduleAsync(Guid fleetId, Guid scheduleId);

    Task<FleetAllowedStationGroupDto> AddAllowedStationGroupAsync(Guid fleetId, Guid stationGroupId);
    Task RemoveAllowedStationGroupAsync(Guid fleetId, Guid stationGroupId);

    Task<FleetAnalyticsDto> GetAnalyticsAsync(Guid fleetId, DateTime? from = null, DateTime? to = null);
}
