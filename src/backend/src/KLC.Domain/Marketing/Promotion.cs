using System;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Marketing;

/// <summary>
/// Represents a marketing campaign/promotion.
/// </summary>
public class Promotion : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Promotion title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Promotion description/content.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Banner/cover image URL.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// When the promotion starts.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// When the promotion ends.
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// How the promotion is displayed.
    /// </summary>
    public PromotionType Type { get; private set; }

    /// <summary>
    /// Whether the promotion is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    protected Promotion()
    {
        // Required by EF Core
    }

    public Promotion(
        Guid id,
        string title,
        DateTime startDate,
        DateTime endDate,
        PromotionType type,
        string? description = null,
        string? imageUrl = null)
        : base(id)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        StartDate = startDate;
        EndDate = endDate;
        Type = type;
        Description = description;
        ImageUrl = imageUrl;
        IsActive = true;
    }

    public bool IsCurrentlyActive()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartDate && now <= EndDate;
    }

    public void Update(string? title, string? description, string? imageUrl,
        DateTime? startDate, DateTime? endDate, PromotionType? type, bool? isActive)
    {
        if (title != null) Title = Check.NotNullOrWhiteSpace(title, nameof(title), maxLength: 200);
        if (description != null) Description = description;
        if (imageUrl != null) ImageUrl = imageUrl;
        if (startDate.HasValue) StartDate = startDate.Value;
        if (endDate.HasValue) EndDate = endDate.Value;
        if (type.HasValue) Type = type.Value;
        if (isActive.HasValue) IsActive = isActive.Value;
    }
}
