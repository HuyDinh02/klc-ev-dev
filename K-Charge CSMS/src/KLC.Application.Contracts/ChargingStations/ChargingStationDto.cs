using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace KLC.ChargingStations;

public class ChargingStationDto : AuditedEntityDto<Guid>
{
    public string ChargePointId { get; set; }
    public string Name { get; set; }
    public string Vendor { get; set; }
    public string Model { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string SerialNumber { get; set; }
    public string FirmwareVersion { get; set; }
    public string StationGroupId { get; set; }
    public string Status { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public int ConnectorCount { get; set; }
    public List<ConnectorDto> Connectors { get; set; } = new();
}
