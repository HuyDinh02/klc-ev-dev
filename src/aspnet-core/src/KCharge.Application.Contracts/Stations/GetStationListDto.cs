using System;
using KCharge.Enums;
using Volo.Abp.Application.Dtos;

namespace KCharge.Stations;

/// <summary>
/// DTO for filtering and paginating station list.
/// Uses cursor-based pagination per project conventions.
/// </summary>
public class GetStationListDto : LimitedResultRequestDto
{
    /// <summary>
    /// Filter by station status.
    /// </summary>
    public StationStatus? Status { get; set; }

    /// <summary>
    /// Filter by station group.
    /// </summary>
    public Guid? StationGroupId { get; set; }

    /// <summary>
    /// Search by name or station code.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by enabled/disabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Cursor for pagination (station Id to start after).
    /// </summary>
    public Guid? Cursor { get; set; }

    /// <summary>
    /// Sort field (Name, StationCode, Status, CreationTime).
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction (asc/desc).
    /// </summary>
    public string? SortOrder { get; set; }
}
