using System;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Maintenance;

public class MaintenanceTask : FullAuditedAggregateRoot<Guid>
{
    public Guid StationId { get; private set; }

    public int? ConnectorNumber { get; private set; }

    public MaintenanceTaskType Type { get; private set; }

    public MaintenanceTaskStatus Status { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string AssignedTo { get; private set; } = string.Empty;

    public DateTime ScheduledDate { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public string? Notes { get; private set; }

    protected MaintenanceTask()
    {
    }

    public MaintenanceTask(
        Guid id,
        Guid stationId,
        MaintenanceTaskType type,
        string title,
        string assignedTo,
        DateTime scheduledDate,
        int? connectorNumber = null,
        string? description = null)
        : base(id)
    {
        StationId = stationId;
        Type = type;
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        AssignedTo = Check.NotNullOrWhiteSpace(assignedTo, nameof(assignedTo), maxLength: 200);
        ScheduledDate = scheduledDate;
        ConnectorNumber = connectorNumber;
        Description = description;
        Status = MaintenanceTaskStatus.Planned;
    }

    public void Start()
    {
        if (Status != MaintenanceTaskStatus.Planned)
            throw new BusinessException(KLCDomainErrorCodes.Maintenance.InvalidStateTransition)
                .WithData("currentStatus", Status.ToString());

        Status = MaintenanceTaskStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(string? notes = null)
    {
        if (Status != MaintenanceTaskStatus.InProgress)
            throw new BusinessException(KLCDomainErrorCodes.Maintenance.InvalidStateTransition)
                .WithData("currentStatus", Status.ToString());

        Status = MaintenanceTaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Notes = notes;
    }

    public void Cancel(string? notes = null)
    {
        if (Status == MaintenanceTaskStatus.Completed || Status == MaintenanceTaskStatus.Cancelled)
            throw new BusinessException(KLCDomainErrorCodes.Maintenance.InvalidStateTransition)
                .WithData("currentStatus", Status.ToString());

        Status = MaintenanceTaskStatus.Cancelled;
        Notes = notes;
    }

    public void Update(string? title, string? description, string? assignedTo, DateTime? scheduledDate)
    {
        if (title != null) Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        if (description != null) Description = description;
        if (assignedTo != null) AssignedTo = Check.NotNullOrWhiteSpace(assignedTo, nameof(assignedTo), maxLength: 200);
        if (scheduledDate.HasValue) ScheduledDate = scheduledDate.Value;
    }

    public bool IsOverdue()
    {
        return Status == MaintenanceTaskStatus.Planned && ScheduledDate < DateTime.UtcNow;
    }
}
