using System;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Support;

/// <summary>
/// Represents a user feedback/support ticket from the mobile app.
/// </summary>
public class UserFeedback : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the AppUser who submitted the feedback.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Type of feedback.
    /// </summary>
    public FeedbackType Type { get; private set; }

    /// <summary>
    /// Subject/title of the feedback.
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// Detailed message from the user.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Current status of the feedback ticket.
    /// </summary>
    public FeedbackStatus Status { get; private set; }

    /// <summary>
    /// Admin response to the feedback.
    /// </summary>
    public string? AdminResponse { get; private set; }

    /// <summary>
    /// When the admin responded.
    /// </summary>
    public DateTime? RespondedAt { get; private set; }

    /// <summary>
    /// Admin user who responded.
    /// </summary>
    public Guid? RespondedBy { get; private set; }

    protected UserFeedback()
    {
        // Required by EF Core
    }

    public UserFeedback(
        Guid id,
        Guid userId,
        FeedbackType type,
        string subject,
        string message)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Subject = Check.NotNullOrWhiteSpace(subject, nameof(subject), maxLength: 200);
        Message = Check.NotNullOrWhiteSpace(message, nameof(message), maxLength: 2000);
        Status = FeedbackStatus.Open;
    }

    public void SetInReview()
    {
        Status = FeedbackStatus.InReview;
    }

    public void Resolve(string adminResponse, Guid respondedBy)
    {
        AdminResponse = Check.NotNullOrWhiteSpace(adminResponse, nameof(adminResponse), maxLength: 2000);
        RespondedBy = respondedBy;
        RespondedAt = DateTime.UtcNow;
        Status = FeedbackStatus.Resolved;
    }

    public void Close()
    {
        Status = FeedbackStatus.Closed;
    }
}
