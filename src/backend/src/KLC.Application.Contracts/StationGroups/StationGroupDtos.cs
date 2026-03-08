using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;

namespace KLC.StationGroups;

public class StationGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Region { get; set; }
    public StationGroupType GroupType { get; set; }
    public Guid? ParentGroupId { get; set; }
    public string? ParentGroupName { get; set; }
    public bool IsActive { get; set; }
    public int StationCount { get; set; }
    public int ChildGroupCount { get; set; }
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
}

public class StationGroupListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Region { get; set; }
    public StationGroupType GroupType { get; set; }
    public Guid? ParentGroupId { get; set; }
    public string? ParentGroupName { get; set; }
    public bool IsActive { get; set; }
    public int StationCount { get; set; }
    public int ChildGroupCount { get; set; }
}

public class StationGroupDetailDto : StationGroupDto
{
    public List<GroupStationDto> Stations { get; set; } = new();
    public List<StationGroupListDto> ChildGroups { get; set; } = new();
    public StationGroupStatsDto Stats { get; set; } = new();
}

public class StationGroupStatsDto
{
    public int TotalStations { get; set; }
    public int TotalConnectors { get; set; }
    public int AvailableConnectors { get; set; }
    public int OccupiedConnectors { get; set; }
    public int FaultedConnectors { get; set; }
    public int OfflineStations { get; set; }
    public double TotalCapacityKw { get; set; }
}

public class GroupStationDto
{
    public Guid StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ConnectorCount { get; set; }
    public int AvailableConnectors { get; set; }
}

public class CreateStationGroupDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Region { get; set; }

    public StationGroupType GroupType { get; set; } = StationGroupType.Geographic;

    public Guid? ParentGroupId { get; set; }
}

public class UpdateStationGroupDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Region { get; set; }

    public StationGroupType? GroupType { get; set; }

    public Guid? ParentGroupId { get; set; }

    public bool? IsActive { get; set; }
}

public class AssignStationDto
{
    [Required]
    public Guid StationId { get; set; }
}

public class GetStationGroupListDto
{
    public string? Search { get; set; }
    public string? Region { get; set; }
    public bool? IsActive { get; set; }
    public StationGroupType? GroupType { get; set; }
    public Guid? ParentGroupId { get; set; }
    public bool? TopLevelOnly { get; set; }
    public Guid? Cursor { get; set; }

    [Range(1, 100)]
    public int MaxResultCount { get; set; } = 20;
}
