using System;
using System.ComponentModel.DataAnnotations;
using KCharge.Enums;
using Volo.Abp.Application.Dtos;

namespace KCharge.Sessions;

public class ChargingSessionDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid StationId { get; set; }
    public int ConnectorNumber { get; set; }
    public int? OcppTransactionId { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MeterStart { get; set; }
    public int? MeterStop { get; set; }
    public decimal TotalEnergyKwh { get; set; }
    public decimal TotalCost { get; set; }
    public Guid? TariffPlanId { get; set; }
    public decimal RatePerKwh { get; set; }
    public string? StopReason { get; set; }
    public string? IdTag { get; set; }

    // Computed fields for display
    public string? StationName { get; set; }
    public string? VehicleName { get; set; }
    public TimeSpan? Duration => EndTime.HasValue && StartTime.HasValue
        ? EndTime.Value - StartTime.Value
        : StartTime.HasValue ? DateTime.UtcNow - StartTime.Value : null;
}

public class SessionListDto : EntityDto<Guid>
{
    public string StationName { get; set; } = string.Empty;
    public int ConnectorNumber { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal TotalEnergyKwh { get; set; }
    public decimal TotalCost { get; set; }
}

public class StartSessionDto
{
    [Required]
    public Guid StationId { get; set; }

    [Required]
    public int ConnectorNumber { get; set; }

    public Guid? VehicleId { get; set; }
}

public class StopSessionDto
{
    public string? StopReason { get; set; }
}

public class GetSessionListDto : LimitedResultRequestDto
{
    public SessionStatus? Status { get; set; }
    public Guid? StationId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? Cursor { get; set; }
}

public class ActiveSessionDto
{
    public Guid SessionId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationAddress { get; set; } = string.Empty;
    public int ConnectorNumber { get; set; }
    public DateTime StartTime { get; set; }
    public decimal CurrentEnergyKwh { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal? CurrentPowerKw { get; set; }
    public TimeSpan Duration => DateTime.UtcNow - StartTime;
}

public class MeterValueDto : EntityDto<Guid>
{
    public DateTime Timestamp { get; set; }
    public decimal EnergyKwh { get; set; }
    public decimal? CurrentAmps { get; set; }
    public decimal? VoltageVolts { get; set; }
    public decimal? PowerKw { get; set; }
    public decimal? SocPercent { get; set; }
}
