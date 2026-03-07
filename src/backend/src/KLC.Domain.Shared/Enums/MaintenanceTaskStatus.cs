namespace KLC.Enums;

public enum MaintenanceTaskStatus
{
    Planned = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum MaintenanceTaskType
{
    Scheduled = 0,
    Inspection = 1,
    Emergency = 2
}
