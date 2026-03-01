using System;
using System.Collections.Generic;
using KCharge.Enums;
using Volo.Abp.Application.Dtos;

namespace KCharge.Stations;

/// <summary>
/// DTO for station details.
/// </summary>
public class StationDto : FullAuditedEntityDto<Guid>
{
    public string StationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public StationStatus Status { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? Model { get; set; }
    public string? Vendor { get; set; }
    public string? SerialNumber { get; set; }
    public Guid? StationGroupId { get; set; }
    public Guid? TariffPlanId { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public bool IsEnabled { get; set; }
    public List<ConnectorDto> Connectors { get; set; } = new();
}

/// <summary>
/// DTO for station list items (without connectors).
/// </summary>
public class StationListDto : EntityDto<Guid>
{
    public string StationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public StationStatus Status { get; set; }
    public bool IsEnabled { get; set; }
    public int ConnectorCount { get; set; }
    public DateTime? LastHeartbeat { get; set; }
}
