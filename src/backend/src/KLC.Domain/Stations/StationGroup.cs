using System;
using System.Collections.Generic;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Stations;

/// <summary>
/// Represents a logical grouping of charging stations.
/// Supports hierarchy (parent-child) and categorization by type.
/// </summary>
public class StationGroup : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Name of the station group.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the group.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Region or area this group covers.
    /// </summary>
    public string? Region { get; private set; }

    /// <summary>
    /// Classification of this group's purpose.
    /// </summary>
    public StationGroupType GroupType { get; private set; }

    /// <summary>
    /// Parent group ID for hierarchy support. Null means top-level group.
    /// </summary>
    public Guid? ParentGroupId { get; private set; }

    /// <summary>
    /// Whether this group is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Collection of stations in this group.
    /// </summary>
    public ICollection<ChargingStation> Stations { get; private set; } = new List<ChargingStation>();

    /// <summary>
    /// Child groups in the hierarchy.
    /// </summary>
    public ICollection<StationGroup> Children { get; private set; } = new List<StationGroup>();

    /// <summary>
    /// Parent group reference.
    /// </summary>
    public StationGroup? ParentGroup { get; private set; }

    protected StationGroup()
    {
        // Required by EF Core
    }

    public StationGroup(
        Guid id,
        string name,
        string? description = null,
        string? region = null,
        StationGroupType groupType = StationGroupType.Geographic,
        Guid? parentGroupId = null)
        : base(id)
    {
        SetName(name);
        Description = description;
        Region = region;
        GroupType = groupType;
        ParentGroupId = parentGroupId;
        IsActive = true;
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 200);
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }

    public void SetRegion(string? region)
    {
        Region = region;
    }

    public void SetGroupType(StationGroupType groupType)
    {
        GroupType = groupType;
    }

    public void SetParentGroup(Guid? parentGroupId)
    {
        ParentGroupId = parentGroupId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
