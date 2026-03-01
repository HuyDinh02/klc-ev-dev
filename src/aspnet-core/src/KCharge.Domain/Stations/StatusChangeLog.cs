using System;
using KCharge.Enums;
using Volo.Abp.Domain.Entities;

namespace KCharge.Stations;

/// <summary>
/// Logs status changes for stations and connectors for audit and analytics.
/// </summary>
public class StatusChangeLog : Entity<Guid>
{
    /// <summary>
    /// Reference to the charging station.
    /// </summary>
    public Guid StationId { get; private set; }

    /// <summary>
    /// Reference to the connector (null if station-level change).
    /// </summary>
    public int? ConnectorNumber { get; private set; }

    /// <summary>
    /// Previous status value (as string to support both station and connector status).
    /// </summary>
    public string PreviousStatus { get; private set; } = string.Empty;

    /// <summary>
    /// New status value.
    /// </summary>
    public string NewStatus { get; private set; } = string.Empty;

    /// <summary>
    /// When the status change occurred.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Source of the status change (OCPP, Admin, System).
    /// </summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>
    /// Additional details or reason for the change.
    /// </summary>
    public string? Details { get; private set; }

    protected StatusChangeLog()
    {
        // Required by EF Core
    }

    public StatusChangeLog(
        Guid id,
        Guid stationId,
        int? connectorNumber,
        string previousStatus,
        string newStatus,
        string source,
        string? details = null)
        : base(id)
    {
        StationId = stationId;
        ConnectorNumber = connectorNumber;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Timestamp = DateTime.UtcNow;
        Source = source;
        Details = details;
    }

    public static StatusChangeLog CreateForStation(
        Guid id,
        Guid stationId,
        StationStatus previousStatus,
        StationStatus newStatus,
        string source,
        string? details = null)
    {
        return new StatusChangeLog(
            id,
            stationId,
            null,
            previousStatus.ToString(),
            newStatus.ToString(),
            source,
            details);
    }

    public static StatusChangeLog CreateForConnector(
        Guid id,
        Guid stationId,
        int connectorNumber,
        ConnectorStatus previousStatus,
        ConnectorStatus newStatus,
        string source,
        string? details = null)
    {
        return new StatusChangeLog(
            id,
            stationId,
            connectorNumber,
            previousStatus.ToString(),
            newStatus.ToString(),
            source,
            details);
    }
}
