using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KCharge.Stations;

/// <summary>
/// Represents a logical grouping of charging stations (by region, operator, etc.).
/// Aggregate root for station grouping.
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
    /// Whether this group is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Collection of stations in this group.
    /// </summary>
    public ICollection<ChargingStation> Stations { get; private set; } = new List<ChargingStation>();

    protected StationGroup()
    {
        // Required by EF Core
    }

    public StationGroup(Guid id, string name, string? description = null, string? region = null)
        : base(id)
    {
        SetName(name);
        Description = description;
        Region = region;
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

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
