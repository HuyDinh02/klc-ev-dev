using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Stations;

/// <summary>
/// Represents a photo in a charging station's gallery.
/// </summary>
public class StationPhoto : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the ChargingStation.
    /// </summary>
    public Guid StationId { get; private set; }

    /// <summary>
    /// Full-size image URL.
    /// </summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>
    /// Thumbnail image URL.
    /// </summary>
    public string? ThumbnailUrl { get; private set; }

    /// <summary>
    /// Whether this is the primary/cover photo.
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Display order in the gallery.
    /// </summary>
    public int SortOrder { get; private set; }

    protected StationPhoto()
    {
        // Required by EF Core
    }

    public StationPhoto(
        Guid id,
        Guid stationId,
        string url,
        string? thumbnailUrl = null,
        bool isPrimary = false,
        int sortOrder = 0)
        : base(id)
    {
        StationId = stationId;
        Url = Check.NotNullOrWhiteSpace(url, nameof(url), maxLength: 500);
        ThumbnailUrl = thumbnailUrl;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void UnsetPrimary()
    {
        IsPrimary = false;
    }

    public void SetSortOrder(int order)
    {
        SortOrder = order;
    }
}
