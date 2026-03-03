using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace KLC.Vehicles;

[Authorize]
public class VehicleAppService : KLCAppService, IVehicleAppService
{
    private readonly IRepository<Vehicle, Guid> _vehicleRepository;

    public VehicleAppService(IRepository<Vehicle, Guid> vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleDto input)
    {
        var userId = CurrentUser.GetId();

        var vehicle = new Vehicle(
            GuidGenerator.Create(),
            userId,
            input.Make,
            input.Model,
            input.LicensePlate,
            input.BatteryCapacityKwh,
            input.PreferredConnectorType
        );

        vehicle.SetDetails(input.Color, input.Year, input.Nickname);

        // If first vehicle, set as default
        var existingVehicles = await _vehicleRepository.GetListAsync(v => v.UserId == userId && v.IsActive);
        if (!existingVehicles.Any())
        {
            vehicle.SetAsDefault();
        }

        await _vehicleRepository.InsertAsync(vehicle);
        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleDto input)
    {
        var userId = CurrentUser.GetId();
        var vehicle = await _vehicleRepository.GetAsync(id);

        if (vehicle.UserId != userId)
        {
            throw new BusinessException(KLCDomainErrorCodes.Vehicle.NotOwned);
        }

        vehicle.SetMakeAndModel(input.Make, input.Model);
        vehicle.SetLicensePlate(input.LicensePlate);
        vehicle.SetDetails(input.Color, input.Year, input.Nickname);
        vehicle.SetBatteryCapacity(input.BatteryCapacityKwh);
        vehicle.SetPreferredConnectorType(input.PreferredConnectorType);

        await _vehicleRepository.UpdateAsync(vehicle);
        return MapToDto(vehicle);
    }

    public async Task<VehicleDto> GetAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var vehicle = await _vehicleRepository.GetAsync(id);

        if (vehicle.UserId != userId)
        {
            throw new BusinessException(KLCDomainErrorCodes.Vehicle.NotOwned);
        }

        return MapToDto(vehicle);
    }

    public async Task<List<VehicleDto>> GetMyVehiclesAsync()
    {
        var userId = CurrentUser.GetId();
        var vehicles = await _vehicleRepository.GetListAsync(v => v.UserId == userId && v.IsActive);

        return vehicles
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.Make)
            .Select(MapToDto)
            .ToList();
    }

    public async Task DeleteAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var vehicle = await _vehicleRepository.GetAsync(id);

        if (vehicle.UserId != userId)
        {
            throw new BusinessException(KLCDomainErrorCodes.Vehicle.NotOwned);
        }

        // TODO: Check for active charging session (MOD_009_002)

        vehicle.Deactivate();
        await _vehicleRepository.UpdateAsync(vehicle);
    }

    public async Task SetAsDefaultAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var vehicles = await _vehicleRepository.GetListAsync(v => v.UserId == userId && v.IsActive);

        foreach (var v in vehicles)
        {
            if (v.Id == id)
            {
                v.SetAsDefault();
            }
            else if (v.IsDefault)
            {
                v.RemoveDefault();
            }
        }

        await _vehicleRepository.UpdateManyAsync(vehicles);
    }

    public async Task<VehicleDto?> GetDefaultVehicleAsync()
    {
        var userId = CurrentUser.GetId();
        var vehicle = await _vehicleRepository.FirstOrDefaultAsync(
            v => v.UserId == userId && v.IsActive && v.IsDefault);

        return vehicle != null ? MapToDto(vehicle) : null;
    }

    private static VehicleDto MapToDto(Vehicle vehicle)
    {
        return new VehicleDto
        {
            Id = vehicle.Id,
            UserId = vehicle.UserId,
            Make = vehicle.Make,
            Model = vehicle.Model,
            LicensePlate = vehicle.LicensePlate,
            Color = vehicle.Color,
            Year = vehicle.Year,
            BatteryCapacityKwh = vehicle.BatteryCapacityKwh,
            PreferredConnectorType = vehicle.PreferredConnectorType,
            IsActive = vehicle.IsActive,
            IsDefault = vehicle.IsDefault,
            Nickname = vehicle.Nickname,
            CreationTime = vehicle.CreationTime,
            CreatorId = vehicle.CreatorId,
            LastModificationTime = vehicle.LastModificationTime,
            LastModifierId = vehicle.LastModifierId
        };
    }
}
