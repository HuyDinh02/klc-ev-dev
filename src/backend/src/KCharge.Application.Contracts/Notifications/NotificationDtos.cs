using System;
using System.ComponentModel.DataAnnotations;
using KCharge.Enums;
using Volo.Abp.Application.Dtos;

namespace KCharge.Notifications;

public class NotificationDto : EntityDto<Guid>
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ReferenceId { get; set; }
}

public class NotificationListDto : EntityDto<Guid>
{
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetNotificationListDto : LimitedResultRequestDto
{
    public bool? IsRead { get; set; }
    public NotificationType? Type { get; set; }
    public Guid? Cursor { get; set; }
}

public class RegisterDeviceDto
{
    [Required]
    [StringLength(500)]
    public string FcmToken { get; set; } = string.Empty;

    [StringLength(50)]
    public string? DeviceType { get; set; } // "ios" or "android"
}

public class AlertDto : EntityDto<Guid>
{
    public Guid? StationId { get; set; }
    public string? StationName { get; set; }
    public AlertType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedBy { get; set; }
}

public class GetAlertListDto : LimitedResultRequestDto
{
    public Guid? StationId { get; set; }
    public AlertType? Type { get; set; }
    public AlertStatus? Status { get; set; }
    public Guid? Cursor { get; set; }
}
