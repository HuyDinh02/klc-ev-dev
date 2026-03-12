using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;
using Volo.Abp.Application.Dtos;

namespace KLC.PowerSharing;

public class PowerSharingGroupDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal MaxCapacityKw { get; set; }
    public PowerSharingMode Mode { get; set; }
    public PowerDistributionStrategy DistributionStrategy { get; set; }
    public bool IsActive { get; set; }
    public decimal MinPowerPerConnectorKw { get; set; }
    public Guid? StationGroupId { get; set; }
    public List<PowerSharingMemberDto> Members { get; set; } = [];
}

public class PowerSharingGroupListDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal MaxCapacityKw { get; set; }
    public PowerSharingMode Mode { get; set; }
    public PowerDistributionStrategy DistributionStrategy { get; set; }
    public bool IsActive { get; set; }
    public int MemberCount { get; set; }
    public decimal TotalAllocatedKw { get; set; }
    public DateTime CreationTime { get; set; }
}

public class PowerSharingMemberDto : EntityDto<Guid>
{
    public Guid StationId { get; set; }
    public Guid ConnectorId { get; set; }
    public int Priority { get; set; }
    public decimal AllocatedPowerKw { get; set; }
    public string? StationName { get; set; }
    public int ConnectorNumber { get; set; }
    public string? ConnectorType { get; set; }
    public decimal MaxPowerKw { get; set; }
}

public class CreatePowerSharingGroupDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000)]
    public decimal MaxCapacityKw { get; set; }

    [Required]
    public PowerSharingMode Mode { get; set; }

    public PowerDistributionStrategy DistributionStrategy { get; set; } = PowerDistributionStrategy.Average;

    [Range(0, 1000)]
    public decimal MinPowerPerConnectorKw { get; set; } = 1.4m;

    public Guid? StationGroupId { get; set; }
}

public class UpdatePowerSharingGroupDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000)]
    public decimal MaxCapacityKw { get; set; }

    public PowerDistributionStrategy DistributionStrategy { get; set; }

    [Range(0, 1000)]
    public decimal MinPowerPerConnectorKw { get; set; }
}

public class AddMemberDto
{
    [Required]
    public Guid StationId { get; set; }

    [Required]
    public Guid ConnectorId { get; set; }

    public int Priority { get; set; }
}

public class GetPowerSharingGroupListDto
{
    public string? Cursor { get; set; }
    public int PageSize { get; set; } = 20;
    public bool? IsActive { get; set; }
    public PowerSharingMode? Mode { get; set; }
    public string? Search { get; set; }
}

public class PowerAllocationDto
{
    public Guid ConnectorId { get; set; }
    public Guid StationId { get; set; }
    public decimal AllocatedPowerKw { get; set; }
    public decimal MaxPowerKw { get; set; }
}

public class SiteLoadProfileDto : EntityDto<Guid>
{
    public Guid PowerSharingGroupId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal TotalLoadKw { get; set; }
    public decimal AvailableCapacityKw { get; set; }
    public int ActiveSessionCount { get; set; }
    public int TotalConnectorCount { get; set; }
    public decimal PeakLoadKw { get; set; }
}
