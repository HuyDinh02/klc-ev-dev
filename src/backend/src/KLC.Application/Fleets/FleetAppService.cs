using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Permissions;
using KLC.Sessions;
using KLC.Stations;
using KLC.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace KLC.Fleets;

[Authorize(KLCPermissions.Fleets.Default)]
public class FleetAppService : KLCAppService, IFleetAppService
{
    private readonly IRepository<Fleet, Guid> _fleetRepository;
    private readonly IRepository<FleetVehicle, Guid> _fleetVehicleRepository;
    private readonly IRepository<FleetChargingSchedule, Guid> _scheduleRepository;
    private readonly IRepository<FleetAllowedStation, Guid> _allowedStationRepository;
    private readonly IRepository<Vehicle, Guid> _vehicleRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<StationGroup, Guid> _stationGroupRepository;

    public FleetAppService(
        IRepository<Fleet, Guid> fleetRepository,
        IRepository<FleetVehicle, Guid> fleetVehicleRepository,
        IRepository<FleetChargingSchedule, Guid> scheduleRepository,
        IRepository<FleetAllowedStation, Guid> allowedStationRepository,
        IRepository<Vehicle, Guid> vehicleRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<StationGroup, Guid> stationGroupRepository)
    {
        _fleetRepository = fleetRepository;
        _fleetVehicleRepository = fleetVehicleRepository;
        _scheduleRepository = scheduleRepository;
        _allowedStationRepository = allowedStationRepository;
        _vehicleRepository = vehicleRepository;
        _sessionRepository = sessionRepository;
        _stationRepository = stationRepository;
        _stationGroupRepository = stationGroupRepository;
    }

    public async Task<PagedResultDto<FleetDto>> GetListAsync(GetFleetListDto input)
    {
        var query = await _fleetRepository.WithDetailsAsync(f => f.Vehicles);

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            query = query.Where(f => f.Name.ToLower().Contains(search));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(f => f.IsActive == input.IsActive.Value);
        }

        // Cursor-based pagination by CreationTime
        if (input.Cursor.HasValue)
        {
            var cursorFleet = await _fleetRepository.FirstOrDefaultAsync(f => f.Id == input.Cursor.Value);
            if (cursorFleet != null)
            {
                query = query.Where(f => f.CreationTime < cursorFleet.CreationTime
                    || (f.CreationTime == cursorFleet.CreationTime && f.Id.CompareTo(input.Cursor.Value) > 0));
            }
        }

        query = query.OrderByDescending(f => f.CreationTime).ThenBy(f => f.Id);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var fleets = await AsyncExecuter.ToListAsync(query.Take(input.PageSize));

        var dtos = fleets.Select(f => MapToFleetDto(f)).ToList();

        return new PagedResultDto<FleetDto>(totalCount, dtos);
    }

    public async Task<FleetDetailDto> GetAsync(Guid id)
    {
        var query = await _fleetRepository.WithDetailsAsync(f => f.Vehicles);
        var fleet = await AsyncExecuter.FirstOrDefaultAsync(query.Where(f => f.Id == id));

        if (fleet == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.NotFound);

        var detail = new FleetDetailDto
        {
            Id = fleet.Id,
            Name = fleet.Name,
            Description = fleet.Description,
            OperatorUserId = fleet.OperatorUserId,
            MaxMonthlyBudgetVnd = fleet.MaxMonthlyBudgetVnd,
            CurrentMonthSpentVnd = fleet.CurrentMonthSpentVnd,
            ChargingPolicy = fleet.ChargingPolicy,
            IsActive = fleet.IsActive,
            BudgetAlertThresholdPercent = fleet.BudgetAlertThresholdPercent,
            VehicleCount = fleet.Vehicles.Count(v => v.IsActive),
            BudgetUtilizationPercent = fleet.GetBudgetUtilizationPercent(),
            CreationTime = fleet.CreationTime,
            Vehicles = new List<FleetVehicleDto>()
        };

        foreach (var fv in fleet.Vehicles.Where(v => v.IsActive))
        {
            var vehicleDto = await MapToFleetVehicleDtoAsync(fv);
            detail.Vehicles.Add(vehicleDto);
        }

        // Load schedules
        var schedules = await _scheduleRepository.GetListAsync(s => s.FleetId == id);
        detail.Schedules = schedules.Select(s => new FleetChargingScheduleDto
        {
            Id = s.Id,
            FleetId = s.FleetId,
            DayOfWeek = s.DayOfWeek,
            StartTimeUtc = s.StartTimeUtc,
            EndTimeUtc = s.EndTimeUtc
        }).ToList();

        // Load allowed station groups
        var allowedStations = await _allowedStationRepository.GetListAsync(a => a.FleetId == id);
        foreach (var allowed in allowedStations)
        {
            var group = await _stationGroupRepository.FirstOrDefaultAsync(sg => sg.Id == allowed.StationGroupId);
            detail.AllowedStationGroups.Add(new FleetAllowedStationGroupDto
            {
                Id = allowed.Id,
                FleetId = allowed.FleetId,
                StationGroupId = allowed.StationGroupId,
                StationGroupName = group?.Name ?? "Unknown"
            });
        }

        return detail;
    }

    [Authorize(KLCPermissions.Fleets.Create)]
    public async Task<FleetDto> CreateAsync(CreateFleetDto input)
    {
        var fleet = new Fleet(
            GuidGenerator.Create(),
            input.Name,
            CurrentUser.GetId(),
            input.Description,
            input.MaxMonthlyBudgetVnd,
            input.ChargingPolicy ?? ChargingPolicyType.AnytimeAnywhere,
            input.BudgetAlertThresholdPercent ?? 80);

        await _fleetRepository.InsertAsync(fleet);

        return MapToFleetDto(fleet);
    }

    [Authorize(KLCPermissions.Fleets.Update)]
    public async Task<FleetDto> UpdateAsync(Guid id, UpdateFleetDto input)
    {
        var query = await _fleetRepository.WithDetailsAsync(f => f.Vehicles);
        var fleet = await AsyncExecuter.FirstOrDefaultAsync(query.Where(f => f.Id == id));

        if (fleet == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.NotFound);

        fleet.SetName(input.Name);
        fleet.SetDescription(input.Description);
        fleet.SetBudget(input.MaxMonthlyBudgetVnd, input.BudgetAlertThresholdPercent);
        fleet.SetChargingPolicy(input.ChargingPolicy);

        await _fleetRepository.UpdateAsync(fleet);

        return MapToFleetDto(fleet);
    }

    [Authorize(KLCPermissions.Fleets.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var fleet = await _fleetRepository.FirstOrDefaultAsync(f => f.Id == id);
        if (fleet == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.NotFound);

        await _fleetRepository.DeleteAsync(fleet);
    }

    [Authorize(KLCPermissions.Fleets.ManageVehicles)]
    public async Task<FleetVehicleDto> AddVehicleAsync(Guid fleetId, AddFleetVehicleDto input)
    {
        var query = await _fleetRepository.WithDetailsAsync(f => f.Vehicles);
        var fleet = await AsyncExecuter.FirstOrDefaultAsync(query.Where(f => f.Id == fleetId));

        if (fleet == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.NotFound);

        // Verify vehicle exists
        var vehicle = await _vehicleRepository.FirstOrDefaultAsync(v => v.Id == input.VehicleId);
        if (vehicle == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        var fleetVehicle = fleet.AddVehicle(input.VehicleId, input.DriverUserId, input.DailyChargingLimitKwh);

        await _fleetRepository.UpdateAsync(fleet);

        return await MapToFleetVehicleDtoAsync(fleetVehicle);
    }

    [Authorize(KLCPermissions.Fleets.ManageVehicles)]
    public async Task RemoveVehicleAsync(Guid fleetId, Guid vehicleId)
    {
        var query = await _fleetRepository.WithDetailsAsync(f => f.Vehicles);
        var fleet = await AsyncExecuter.FirstOrDefaultAsync(query.Where(f => f.Id == fleetId));

        if (fleet == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.NotFound);

        fleet.RemoveVehicle(vehicleId);

        await _fleetRepository.UpdateAsync(fleet);
    }

    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task<List<FleetChargingScheduleDto>> GetSchedulesAsync(Guid fleetId)
    {
        var fleet = await _fleetRepository.FirstOrDefaultAsync(f => f.Id == fleetId);
        if (fleet == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.NotFound);

        var schedules = await _scheduleRepository.GetListAsync(s => s.FleetId == fleetId);

        return schedules.Select(s => new FleetChargingScheduleDto
        {
            Id = s.Id,
            FleetId = s.FleetId,
            DayOfWeek = s.DayOfWeek,
            StartTimeUtc = s.StartTimeUtc,
            EndTimeUtc = s.EndTimeUtc
        }).ToList();
    }

    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task<FleetChargingScheduleDto> AddScheduleAsync(Guid fleetId, CreateFleetScheduleDto input)
    {
        var fleet = await _fleetRepository.FirstOrDefaultAsync(f => f.Id == fleetId);
        if (fleet == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.NotFound);

        var schedule = new FleetChargingSchedule(
            GuidGenerator.Create(),
            fleetId,
            input.DayOfWeek,
            input.StartTimeUtc,
            input.EndTimeUtc);

        await _scheduleRepository.InsertAsync(schedule);

        return new FleetChargingScheduleDto
        {
            Id = schedule.Id,
            FleetId = schedule.FleetId,
            DayOfWeek = schedule.DayOfWeek,
            StartTimeUtc = schedule.StartTimeUtc,
            EndTimeUtc = schedule.EndTimeUtc
        };
    }

    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task RemoveScheduleAsync(Guid fleetId, Guid scheduleId)
    {
        var schedule = await _scheduleRepository.FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule == null || schedule.FleetId != fleetId)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        await _scheduleRepository.DeleteAsync(schedule);
    }

    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task<FleetAllowedStationGroupDto> AddAllowedStationGroupAsync(Guid fleetId, Guid stationGroupId)
    {
        var fleet = await _fleetRepository.FirstOrDefaultAsync(f => f.Id == fleetId);
        if (fleet == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.NotFound);

        // Verify station group exists
        var stationGroup = await _stationGroupRepository.FirstOrDefaultAsync(sg => sg.Id == stationGroupId);
        if (stationGroup == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        // Check for duplicate
        var existing = await _allowedStationRepository.FirstOrDefaultAsync(
            a => a.FleetId == fleetId && a.StationGroupId == stationGroupId);
        if (existing != null)
        {
            return new FleetAllowedStationGroupDto
            {
                Id = existing.Id,
                FleetId = existing.FleetId,
                StationGroupId = existing.StationGroupId,
                StationGroupName = stationGroup.Name
            };
        }

        var allowed = new FleetAllowedStation(GuidGenerator.Create(), fleetId, stationGroupId);
        await _allowedStationRepository.InsertAsync(allowed);

        return new FleetAllowedStationGroupDto
        {
            Id = allowed.Id,
            FleetId = allowed.FleetId,
            StationGroupId = allowed.StationGroupId,
            StationGroupName = stationGroup.Name
        };
    }

    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task RemoveAllowedStationGroupAsync(Guid fleetId, Guid stationGroupId)
    {
        var allowed = await _allowedStationRepository.FirstOrDefaultAsync(
            a => a.FleetId == fleetId && a.StationGroupId == stationGroupId);

        if (allowed == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        await _allowedStationRepository.DeleteAsync(allowed);
    }

    [Authorize(KLCPermissions.Fleets.ViewAnalytics)]
    public async Task<FleetAnalyticsDto> GetAnalyticsAsync(Guid fleetId, DateTime? from = null, DateTime? to = null)
    {
        var fleet = await _fleetRepository.FirstOrDefaultAsync(f => f.Id == fleetId);
        if (fleet == null)
            throw new BusinessException(KLCDomainErrorCodes.Fleet.NotFound);

        // Get all vehicle IDs in this fleet
        var fleetVehicles = await _fleetVehicleRepository.GetListAsync(fv => fv.FleetId == fleetId && fv.IsActive);
        var vehicleIds = fleetVehicles.Select(fv => fv.VehicleId).ToList();

        if (!vehicleIds.Any())
        {
            return new FleetAnalyticsDto();
        }

        // Query sessions for fleet vehicles
        var sessionQuery = (await _sessionRepository.GetQueryableAsync())
            .Where(s => s.VehicleId.HasValue && vehicleIds.Contains(s.VehicleId.Value));

        if (from.HasValue)
            sessionQuery = sessionQuery.Where(s => s.StartTime >= from.Value);

        if (to.HasValue)
            sessionQuery = sessionQuery.Where(s => s.StartTime <= to.Value);

        var sessions = await AsyncExecuter.ToListAsync(sessionQuery);

        var analytics = new FleetAnalyticsDto
        {
            TotalEnergyKwh = sessions.Sum(s => s.TotalEnergyKwh),
            TotalCostVnd = sessions.Sum(s => s.TotalCost),
            SessionCount = sessions.Count,
            AvgSessionDurationMinutes = sessions
                .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                .Select(s => (s.EndTime!.Value - s.StartTime!.Value).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average()
        };

        // Top vehicles by energy
        var vehicleGroups = sessions
            .Where(s => s.VehicleId.HasValue)
            .GroupBy(s => s.VehicleId!.Value)
            .OrderByDescending(g => g.Sum(s => s.TotalEnergyKwh))
            .Take(5);

        foreach (var vg in vehicleGroups)
        {
            var vehicle = await _vehicleRepository.FirstOrDefaultAsync(v => v.Id == vg.Key);
            analytics.TopVehicles.Add(new FleetAnalyticsVehicleDto
            {
                VehicleId = vg.Key,
                VehicleName = vehicle != null ? $"{vehicle.Make} {vehicle.Model}" : "Unknown",
                EnergyKwh = vg.Sum(s => s.TotalEnergyKwh),
                SessionCount = vg.Count()
            });
        }

        // Top stations by energy
        var stationGroups = sessions
            .GroupBy(s => s.StationId)
            .OrderByDescending(g => g.Sum(s => s.TotalEnergyKwh))
            .Take(5);

        foreach (var sg in stationGroups)
        {
            var station = await _stationRepository.FirstOrDefaultAsync(s => s.Id == sg.Key);
            analytics.TopStations.Add(new FleetAnalyticsStationDto
            {
                StationId = sg.Key,
                StationName = station?.Name ?? "Unknown",
                EnergyKwh = sg.Sum(s => s.TotalEnergyKwh),
                SessionCount = sg.Count()
            });
        }

        return analytics;
    }

    private FleetDto MapToFleetDto(Fleet fleet)
    {
        return new FleetDto
        {
            Id = fleet.Id,
            Name = fleet.Name,
            Description = fleet.Description,
            OperatorUserId = fleet.OperatorUserId,
            MaxMonthlyBudgetVnd = fleet.MaxMonthlyBudgetVnd,
            CurrentMonthSpentVnd = fleet.CurrentMonthSpentVnd,
            ChargingPolicy = fleet.ChargingPolicy,
            IsActive = fleet.IsActive,
            BudgetAlertThresholdPercent = fleet.BudgetAlertThresholdPercent,
            VehicleCount = fleet.Vehicles.Count(v => v.IsActive),
            BudgetUtilizationPercent = fleet.GetBudgetUtilizationPercent(),
            CreationTime = fleet.CreationTime
        };
    }

    private async Task<FleetVehicleDto> MapToFleetVehicleDtoAsync(FleetVehicle fv)
    {
        var vehicle = await _vehicleRepository.FirstOrDefaultAsync(v => v.Id == fv.VehicleId);

        return new FleetVehicleDto
        {
            Id = fv.Id,
            FleetId = fv.FleetId,
            VehicleId = fv.VehicleId,
            VehicleName = vehicle != null ? $"{vehicle.Make} {vehicle.Model}" : "Unknown",
            LicensePlate = vehicle?.LicensePlate,
            DriverUserId = fv.DriverUserId,
            DriverName = null, // Would require identity service lookup
            DailyChargingLimitKwh = fv.DailyChargingLimitKwh,
            CurrentDayEnergyKwh = fv.CurrentDayEnergyKwh,
            CurrentMonthEnergyKwh = fv.CurrentMonthEnergyKwh,
            IsActive = fv.IsActive
        };
    }
}
