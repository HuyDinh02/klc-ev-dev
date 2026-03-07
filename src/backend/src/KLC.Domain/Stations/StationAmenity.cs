using System;
using KLC.Enums;
using Volo.Abp.Domain.Entities;

namespace KLC.Stations;

/// <summary>
/// Represents an amenity tag on a charging station.
/// </summary>
public class StationAmenity : Entity<Guid>
{
    /// <summary>
    /// Reference to the ChargingStation.
    /// </summary>
    public Guid StationId { get; private set; }

    /// <summary>
    /// Type of amenity available at the station.
    /// </summary>
    public AmenityType AmenityType { get; private set; }

    protected StationAmenity()
    {
        // Required by EF Core
    }

    public StationAmenity(Guid id, Guid stationId, AmenityType amenityType)
        : base(id)
    {
        StationId = stationId;
        AmenityType = amenityType;
    }
}
