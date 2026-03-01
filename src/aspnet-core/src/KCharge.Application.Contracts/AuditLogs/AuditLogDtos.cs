using System;

namespace KCharge.AuditLogs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? ImpersonatorUserId { get; set; }
    public string? ImpersonatorUserName { get; set; }
    public DateTime ExecutionTime { get; set; }
    public int ExecutionDuration { get; set; }
    public string? ClientIpAddress { get; set; }
    public string? ClientName { get; set; }
    public string? BrowserInfo { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public string? Comments { get; set; }
}

public class AuditLogListDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public DateTime ExecutionTime { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public int ExecutionDuration { get; set; }
}

public class EntityChangeDto
{
    public Guid Id { get; set; }
    public Guid AuditLogId { get; set; }
    public DateTime ChangeTime { get; set; }
    public string ChangeType { get; set; } = string.Empty; // Created, Updated, Deleted
    public string EntityTypeFullName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
}

public class EntityPropertyChangeDto
{
    public Guid Id { get; set; }
    public Guid EntityChangeId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
    public string PropertyTypeFullName { get; set; } = string.Empty;
}

public class GetAuditLogListDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? UserName { get; set; }
    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public int? MinExecutionDuration { get; set; }
    public int? MaxExecutionDuration { get; set; }
    public bool? HasException { get; set; }
    public int? HttpStatusCode { get; set; }
    public Guid? Cursor { get; set; }
    public int MaxResultCount { get; set; } = 50;
}

public class GetEntityChangesDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? EntityTypeFullName { get; set; }
    public string? EntityId { get; set; }
    public string? ChangeType { get; set; }
    public Guid? Cursor { get; set; }
    public int MaxResultCount { get; set; } = 50;
}
