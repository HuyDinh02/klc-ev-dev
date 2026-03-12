using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Fleets;

/// <summary>
/// Links a fleet to an approved station group (for ApprovedStationsOnly policy).
/// </summary>
public class FleetAllowedStation : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the parent fleet.
    /// </summary>
    public Guid FleetId { get; private set; }

    /// <summary>
    /// Reference to the allowed station group.
    /// </summary>
    public Guid StationGroupId { get; private set; }

    protected FleetAllowedStation()
    {
        // Required by EF Core
    }

    public FleetAllowedStation(
        Guid id,
        Guid fleetId,
        Guid stationGroupId)
        : base(id)
    {
        FleetId = fleetId;
        StationGroupId = stationGroupId;
    }
}
