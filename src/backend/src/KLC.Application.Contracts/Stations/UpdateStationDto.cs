using System;
using System.ComponentModel.DataAnnotations;

namespace KLC.Stations;

/// <summary>
/// DTO for updating an existing charging station.
/// </summary>
public class UpdateStationDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }

    public Guid? StationGroupId { get; set; }

    public Guid? TariffPlanId { get; set; }
}
