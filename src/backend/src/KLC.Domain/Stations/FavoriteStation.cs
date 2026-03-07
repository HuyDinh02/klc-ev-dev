using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Stations;

/// <summary>
/// Represents a user's favorite/bookmarked charging station.
/// </summary>
public class FavoriteStation : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the AppUser.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Reference to the ChargingStation.
    /// </summary>
    public Guid StationId { get; private set; }

    protected FavoriteStation()
    {
        // Required by EF Core
    }

    public FavoriteStation(Guid id, Guid userId, Guid stationId)
        : base(id)
    {
        UserId = userId;
        StationId = stationId;
    }
}
