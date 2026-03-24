using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KLC.ChargingStations;

public class CreateChargingStationDto
{
    [Required]
    [MaxLength(50)]
    public string ChargePointId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    [MaxLength(100)]
    public string Vendor { get; set; }

    [MaxLength(100)]
    public string Model { get; set; }

    [MaxLength(100)]
    public string SerialNumber { get; set; }

    [MaxLength(100)]
    public string FirmwareVersion { get; set; }

    [MaxLength(500)]
    public string Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(100)]
    public string StationGroupId { get; set; }

    public List<CreateConnectorDto> Connectors { get; set; } = new();
}

public class CreateConnectorDto
{
    [Required]
    public int ConnectorId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Type { get; set; }

    [Required]
    public decimal MaxPowerKw { get; set; }
}
