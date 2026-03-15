using System;
using System.Collections.Generic;
using KLC.Enums;

namespace KLC.Fleets;

public class FleetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OperatorUserId { get; set; }
    public decimal MaxMonthlyBudgetVnd { get; set; }
    public decimal CurrentMonthSpentVnd { get; set; }
    public ChargingPolicyType ChargingPolicy { get; set; }
    public bool IsActive { get; set; }
    public int BudgetAlertThresholdPercent { get; set; }
    public int VehicleCount { get; set; }
    public decimal BudgetUtilizationPercent { get; set; }
    public DateTime CreationTime { get; set; }
}

public class FleetDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OperatorUserId { get; set; }
    public decimal MaxMonthlyBudgetVnd { get; set; }
    public decimal CurrentMonthSpentVnd { get; set; }
    public ChargingPolicyType ChargingPolicy { get; set; }
    public bool IsActive { get; set; }
    public int BudgetAlertThresholdPercent { get; set; }
    public int VehicleCount { get; set; }
    public decimal BudgetUtilizationPercent { get; set; }
    public DateTime CreationTime { get; set; }
    public List<FleetVehicleDto> Vehicles { get; set; } = new();
    public List<FleetChargingScheduleDto> Schedules { get; set; } = new();
    public List<FleetAllowedStationGroupDto> AllowedStationGroups { get; set; } = new();
}

public class FleetVehicleDto
{
    public Guid Id { get; set; }
    public Guid FleetId { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public Guid? DriverUserId { get; set; }
    public string? DriverName { get; set; }
    public decimal? DailyChargingLimitKwh { get; set; }
    public decimal CurrentDayEnergyKwh { get; set; }
    public decimal CurrentMonthEnergyKwh { get; set; }
    public bool IsActive { get; set; }
}

public class FleetChargingScheduleDto
{
    public Guid Id { get; set; }
    public Guid FleetId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartTimeUtc { get; set; }
    public TimeSpan EndTimeUtc { get; set; }
}

public class FleetAllowedStationGroupDto
{
    public Guid Id { get; set; }
    public Guid FleetId { get; set; }
    public Guid StationGroupId { get; set; }
    public string StationGroupName { get; set; } = string.Empty;
}

public class CreateFleetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MaxMonthlyBudgetVnd { get; set; }
    public ChargingPolicyType? ChargingPolicy { get; set; }
    public int? BudgetAlertThresholdPercent { get; set; }
}

public class UpdateFleetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MaxMonthlyBudgetVnd { get; set; }
    public ChargingPolicyType ChargingPolicy { get; set; }
    public int BudgetAlertThresholdPercent { get; set; }
}

public class AddFleetVehicleDto
{
    public Guid VehicleId { get; set; }
    public Guid? DriverUserId { get; set; }
    public decimal? DailyChargingLimitKwh { get; set; }
}

public class CreateFleetScheduleDto
{
    public int DayOfWeek { get; set; }
    public TimeSpan StartTimeUtc { get; set; }
    public TimeSpan EndTimeUtc { get; set; }
}

public class GetFleetListDto
{
    public Guid? Cursor { get; set; }
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class FleetAnalyticsDto
{
    public decimal TotalEnergyKwh { get; set; }
    public decimal TotalCostVnd { get; set; }
    public int SessionCount { get; set; }
    public double AvgSessionDurationMinutes { get; set; }
    public List<FleetAnalyticsVehicleDto> TopVehicles { get; set; } = new();
    public List<FleetAnalyticsStationDto> TopStations { get; set; } = new();
}

public class FleetAnalyticsVehicleDto
{
    public Guid VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public decimal EnergyKwh { get; set; }
    public int SessionCount { get; set; }
}

public class FleetAnalyticsStationDto
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public decimal EnergyKwh { get; set; }
    public int SessionCount { get; set; }
}
