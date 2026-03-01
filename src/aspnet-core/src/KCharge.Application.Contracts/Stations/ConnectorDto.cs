using System;
using KCharge.Enums;
using Volo.Abp.Application.Dtos;

namespace KCharge.Stations;

/// <summary>
/// DTO for connector details.
/// </summary>
public class ConnectorDto : FullAuditedEntityDto<Guid>
{
    public Guid StationId { get; set; }
    public int ConnectorNumber { get; set; }
    public ConnectorType ConnectorType { get; set; }
    public decimal MaxPowerKw { get; set; }
    public ConnectorStatus Status { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// DTO for connector list items.
/// </summary>
public class ConnectorListDto : EntityDto<Guid>
{
    public int ConnectorNumber { get; set; }
    public ConnectorType ConnectorType { get; set; }
    public decimal MaxPowerKw { get; set; }
    public ConnectorStatus Status { get; set; }
    public bool IsEnabled { get; set; }
}
