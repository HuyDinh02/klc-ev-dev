using System;
using System.ComponentModel.DataAnnotations;
using KCharge.Enums;
using Volo.Abp.Application.Dtos;

namespace KCharge.Vehicles;

public class VehicleDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public string? Color { get; set; }
    public int? Year { get; set; }
    public decimal? BatteryCapacityKwh { get; set; }
    public ConnectorType? PreferredConnectorType { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public string? Nickname { get; set; }
}

public class VehicleListDto : EntityDto<Guid>
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? LicensePlate { get; set; }
    public string? Nickname { get; set; }
    public bool IsDefault { get; set; }
    public ConnectorType? PreferredConnectorType { get; set; }
}

public class CreateVehicleDto
{
    [Required]
    [StringLength(100)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    [StringLength(20)]
    public string? LicensePlate { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [Range(1900, 2100)]
    public int? Year { get; set; }

    [Range(0, 500)]
    public decimal? BatteryCapacityKwh { get; set; }

    public ConnectorType? PreferredConnectorType { get; set; }

    [StringLength(100)]
    public string? Nickname { get; set; }
}

public class UpdateVehicleDto
{
    [Required]
    [StringLength(100)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    [StringLength(20)]
    public string? LicensePlate { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    [Range(1900, 2100)]
    public int? Year { get; set; }

    [Range(0, 500)]
    public decimal? BatteryCapacityKwh { get; set; }

    public ConnectorType? PreferredConnectorType { get; set; }

    [StringLength(100)]
    public string? Nickname { get; set; }
}
