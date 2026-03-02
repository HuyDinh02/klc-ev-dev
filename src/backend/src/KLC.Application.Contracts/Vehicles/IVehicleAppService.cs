using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Vehicles;

public interface IVehicleAppService : IApplicationService
{
    Task<VehicleDto> CreateAsync(CreateVehicleDto input);
    Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleDto input);
    Task<VehicleDto> GetAsync(Guid id);
    Task<List<VehicleDto>> GetMyVehiclesAsync();
    Task DeleteAsync(Guid id);
    Task SetAsDefaultAsync(Guid id);
    Task<VehicleDto?> GetDefaultVehicleAsync();
}
