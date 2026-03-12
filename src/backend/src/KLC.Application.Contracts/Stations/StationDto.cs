using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;
using Volo.Abp.Application.Dtos;

namespace KLC.Stations;

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

// Station Amenity DTOs
public class StationAmenityDto
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public AmenityType AmenityType { get; set; }
}

public class AddStationAmenityDto
{
    [Required]
    public AmenityType AmenityType { get; set; }
}

// Station Photo DTOs
public class StationPhotoDto
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreationTime { get; set; }
}

public class AddStationPhotoDto
{
    [Required]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class StationPhotoUploadResultDto
{
    public string Url { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
