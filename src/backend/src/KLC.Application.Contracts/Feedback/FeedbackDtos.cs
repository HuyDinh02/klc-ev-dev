using System;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;

namespace KLC.Feedback;

public class FeedbackListDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public FeedbackType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public FeedbackStatus Status { get; set; }
    public string? AdminResponse { get; set; }
    public DateTime? RespondedAt { get; set; }
    public Guid? RespondedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FeedbackDetailDto : FeedbackListDto
{
    public string Message { get; set; } = string.Empty;
}

public class GetFeedbackListDto
{
    public FeedbackStatus? Status { get; set; }
    public FeedbackType? Type { get; set; }
    public Guid? Cursor { get; set; }

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class RespondToFeedbackDto
{
    [Required]
    [StringLength(2000)]
    public string Response { get; set; } = string.Empty;
}
