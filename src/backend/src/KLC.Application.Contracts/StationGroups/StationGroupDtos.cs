using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KLC.StationGroups;

public class StationGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Region { get; set; }
    public bool IsActive { get; set; }
    public int StationCount { get; set; }
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
}

public class StationGroupListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Region { get; set; }
    public bool IsActive { get; set; }
    public int StationCount { get; set; }
}

public class StationGroupDetailDto : StationGroupDto
{
    public List<GroupStationDto> Stations { get; set; } = new();
}

public class GroupStationDto
{
    public Guid StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
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
    public Guid? Cursor { get; set; }

    [Range(1, 100)]
    public int MaxResultCount { get; set; } = 20;
}
