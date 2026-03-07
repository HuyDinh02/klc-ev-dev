using System;
using KLC.Enums;

namespace KLC.Maintenance;

public class MaintenanceTaskDto
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int? ConnectorNumber { get; set; }
    public MaintenanceTaskType Type { get; set; }
    public MaintenanceTaskStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }
}

public class MaintenanceStatsDto
{
    public int PlannedCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }
}

public class CreateMaintenanceTaskDto
{
    public Guid StationId { get; set; }
    public int? ConnectorNumber { get; set; }
    public MaintenanceTaskType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
}

public class UpdateMaintenanceTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

public class GetMaintenanceTaskListDto
{
    public MaintenanceTaskStatus? Status { get; set; }
    public MaintenanceTaskType? Type { get; set; }
    public Guid? StationId { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 20;
}

public class CompleteMaintenanceTaskDto
{
    public string? Notes { get; set; }
}

public class CancelMaintenanceTaskDto
{
    public string? Notes { get; set; }
}
