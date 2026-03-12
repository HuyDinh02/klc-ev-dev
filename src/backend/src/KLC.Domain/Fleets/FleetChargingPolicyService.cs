using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Stations;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace KLC.Fleets;

/// <summary>
/// Domain service that validates charging requests against fleet policies.
/// </summary>
public class FleetChargingPolicyService : DomainService, IFleetChargingPolicyService
{
    private readonly IRepository<FleetVehicle, Guid> _fleetVehicleRepository;
    private readonly IRepository<Fleet, Guid> _fleetRepository;
    private readonly IRepository<FleetChargingSchedule, Guid> _scheduleRepository;
    private readonly IRepository<FleetAllowedStation, Guid> _allowedStationRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;

    public FleetChargingPolicyService(
        IRepository<FleetVehicle, Guid> fleetVehicleRepository,
        IRepository<Fleet, Guid> fleetRepository,
        IRepository<FleetChargingSchedule, Guid> scheduleRepository,
        IRepository<FleetAllowedStation, Guid> allowedStationRepository,
        IRepository<ChargingStation, Guid> stationRepository)
    {
        _fleetVehicleRepository = fleetVehicleRepository;
        _fleetRepository = fleetRepository;
        _scheduleRepository = scheduleRepository;
        _allowedStationRepository = allowedStationRepository;
        _stationRepository = stationRepository;
    }

    public async Task<FleetChargingValidationResult> ValidateChargingAsync(Guid vehicleId, Guid stationId)
    {
        // 1. Find FleetVehicle by vehicleId
        var fleetVehicle = await _fleetVehicleRepository.FindAsync(
            fv => fv.VehicleId == vehicleId && fv.IsActive);

        // 2. If not a fleet vehicle, allow (no restrictions apply)
        if (fleetVehicle == null)
            return new FleetChargingValidationResult(true);

        // 3. Load parent fleet
        var fleet = await _fleetRepository.FindAsync(f => f.Id == fleetVehicle.FleetId);
        if (fleet == null)
            return new FleetChargingValidationResult(true);

        // 4. Check fleet is active
        if (!fleet.IsActive)
            return new FleetChargingValidationResult(false, KLCDomainErrorCodes.Fleet.ChargingDenied);

        // 5. Check daily energy limit on the vehicle
        if (fleetVehicle.IsDailyLimitExceeded())
            return new FleetChargingValidationResult(false, KLCDomainErrorCodes.Fleet.DailyLimitExceeded);

        // 6. Check fleet budget
        if (fleet.IsBudgetExceeded())
            return new FleetChargingValidationResult(false, KLCDomainErrorCodes.Fleet.BudgetExceeded);

        // 7. Check charging policy
        switch (fleet.ChargingPolicy)
        {
            case ChargingPolicyType.AnytimeAnywhere:
                return new FleetChargingValidationResult(true);

            case ChargingPolicyType.ScheduledOnly:
                return await ValidateScheduleAsync(fleet.Id);

            case ChargingPolicyType.ApprovedStationsOnly:
                return await ValidateApprovedStationAsync(fleet.Id, stationId);

            case ChargingPolicyType.DailyEnergyLimit:
                // Already checked via IsDailyLimitExceeded above
                return new FleetChargingValidationResult(true);

            default:
                return new FleetChargingValidationResult(true);
        }
    }

    private async Task<FleetChargingValidationResult> ValidateScheduleAsync(Guid fleetId)
    {
        var now = DateTime.UtcNow;
        var currentDayOfWeek = (int)now.DayOfWeek;
        var currentTime = now.TimeOfDay;

        var schedules = await _scheduleRepository.GetListAsync(
            s => s.FleetId == fleetId && s.DayOfWeek == currentDayOfWeek);

        var isWithinSchedule = schedules.Any(s =>
            currentTime >= s.StartTimeUtc && currentTime <= s.EndTimeUtc);

        return isWithinSchedule
            ? new FleetChargingValidationResult(true)
            : new FleetChargingValidationResult(false, KLCDomainErrorCodes.Fleet.OutsideSchedule);
    }

    private async Task<FleetChargingValidationResult> ValidateApprovedStationAsync(Guid fleetId, Guid stationId)
    {
        // Get the station to find its group
        var station = await _stationRepository.FindAsync(s => s.Id == stationId);
        if (station == null)
            return new FleetChargingValidationResult(false, KLCDomainErrorCodes.Fleet.StationNotAllowed);

        if (!station.StationGroupId.HasValue)
            return new FleetChargingValidationResult(false, KLCDomainErrorCodes.Fleet.StationNotAllowed);

        // Check if the station's group is in the allowed list
        var allowedStations = await _allowedStationRepository.GetListAsync(
            a => a.FleetId == fleetId && a.StationGroupId == station.StationGroupId.Value);
        var isAllowed = allowedStations.Count > 0;

        return isAllowed
            ? new FleetChargingValidationResult(true)
            : new FleetChargingValidationResult(false, KLCDomainErrorCodes.Fleet.StationNotAllowed);
    }
}
