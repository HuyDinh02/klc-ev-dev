using System;
using KLC.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Notifications;

/// <summary>
/// Represents an operational alert for admin/operations team.
/// </summary>
public class Alert : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the station (if applicable).
    /// </summary>
    public Guid? StationId { get; private set; }

    /// <summary>
    /// Connector number (if applicable).
    /// </summary>
    public int? ConnectorNumber { get; private set; }

    /// <summary>
    /// Type of alert.
    /// </summary>
    public AlertType Type { get; private set; }

    /// <summary>
    /// Alert message.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Current alert status.
    /// </summary>
    public AlertStatus Status { get; private set; }

    /// <summary>
    /// When the alert was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; private set; }

    /// <summary>
    /// User who acknowledged the alert.
    /// </summary>
    public Guid? AcknowledgedByUserId { get; private set; }

    /// <summary>
    /// When the alert was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>
    /// User who resolved the alert.
    /// </summary>
    public Guid? ResolvedByUserId { get; private set; }

    /// <summary>
    /// Resolution notes.
    /// </summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>
    /// Priority level (1 = critical, 2 = high, 3 = medium, 4 = low).
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Additional context data as JSON.
    /// </summary>
    public string? Data { get; private set; }

    protected Alert()
    {
        // Required by EF Core
    }

    public Alert(
        Guid id,
        AlertType type,
        string message,
        Guid? stationId = null,
        int? connectorNumber = null,
        string? data = null)
        : base(id)
    {
        Type = type;
        Message = message;
        StationId = stationId;
        ConnectorNumber = connectorNumber;
        Data = data;
        Status = AlertStatus.New;
        Priority = DeterminePriority(type);
    }

    private static int DeterminePriority(AlertType type)
    {
        return type switch
        {
            AlertType.ConnectorFault => 1,
            AlertType.PaymentFailure => 1,
            AlertType.StationOffline => 2,
            AlertType.HeartbeatTimeout => 2,
            AlertType.EInvoiceFailure => 3,
            AlertType.FirmwareUpdate => 4,
            AlertType.LowUtilization => 4,
            AlertType.HighUtilization => 3,
            _ => 3
        };
    }

    public void Acknowledge(Guid userId)
    {
        if (Status != AlertStatus.New)
            throw new InvalidOperationException("Can only acknowledge new alerts");
        Status = AlertStatus.Acknowledged;
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgedByUserId = userId;
    }

    public void Resolve(Guid userId, string? notes = null)
    {
        Status = AlertStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        ResolvedByUserId = userId;
        ResolutionNotes = notes;

        if (!AcknowledgedAt.HasValue)
        {
            AcknowledgedAt = DateTime.UtcNow;
            AcknowledgedByUserId = userId;
        }
    }

    public void SetPriority(int priority)
    {
        if (priority < 1 || priority > 4)
            throw new ArgumentException("Priority must be between 1 and 4", nameof(priority));
        Priority = priority;
    }
}
