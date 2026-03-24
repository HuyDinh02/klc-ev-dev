using System;
using Volo.Abp.Application.Dtos;

namespace KLC.ChargingStations;

public class ConnectorDto : AuditedEntityDto<Guid>
{
    public int ConnectorId { get; set; }
    public string Type { get; set; }
    public string Status { get; set; }
    public string ErrorCode { get; set; }
    public decimal MaxPowerKw { get; set; }
    public Guid ChargingStationId { get; set; }
}
