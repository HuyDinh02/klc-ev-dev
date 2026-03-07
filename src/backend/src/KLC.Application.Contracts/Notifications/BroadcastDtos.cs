using System;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;

namespace KLC.Notifications;

public class BroadcastNotificationDto
{
    [Required]
    public NotificationType Type { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Body { get; set; } = string.Empty;

    public string? Data { get; set; }
    public string? ActionUrl { get; set; }
}

public class BroadcastResultDto
{
    public string Message { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
}

public class BroadcastHistoryDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public int RecipientCount { get; set; }
    public DateTime SentAt { get; set; }
}

public class GetBroadcastHistoryDto
{
    public Guid? Cursor { get; set; }

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}
