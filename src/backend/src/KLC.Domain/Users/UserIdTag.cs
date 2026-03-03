using System;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Users;

/// <summary>
/// Maps a physical idTag (RFID card, key fob) or app-generated token to a registered user.
/// Used by OCPP Authorize/StartTransaction to resolve who is charging.
/// </summary>
public class UserIdTag : FullAuditedEntity<Guid>
{
    /// <summary>
    /// The user this tag belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The OCPP idTag value (RFID UID, app token, etc.). Must be unique.
    /// </summary>
    public string IdTag { get; private set; } = string.Empty;

    /// <summary>
    /// Type of identification tag.
    /// </summary>
    public IdTagType TagType { get; private set; }

    /// <summary>
    /// User-friendly label (e.g., "My Blue Card", "Office Key Fob").
    /// </summary>
    public string? FriendlyName { get; private set; }

    /// <summary>
    /// Whether this tag is currently active. Can be deactivated without deletion.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Optional expiration date for the tag.
    /// </summary>
    public DateTime? ExpiryDate { get; private set; }

    protected UserIdTag()
    {
        // Required by EF Core
    }

    public UserIdTag(
        Guid id,
        Guid userId,
        string idTag,
        IdTagType tagType,
        string? friendlyName = null,
        DateTime? expiryDate = null)
        : base(id)
    {
        Check.NotDefaultOrNull<Guid>(userId, nameof(userId));
        Check.NotNullOrWhiteSpace(idTag, nameof(idTag), maxLength: 50);

        UserId = userId;
        IdTag = idTag;
        TagType = tagType;
        FriendlyName = friendlyName;
        IsActive = true;
        ExpiryDate = expiryDate;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetFriendlyName(string? friendlyName)
    {
        FriendlyName = friendlyName;
    }

    public void SetExpiryDate(DateTime? expiryDate)
    {
        ExpiryDate = expiryDate;
    }

    /// <summary>
    /// Returns true if this tag is active and not expired.
    /// </summary>
    public bool IsValid()
    {
        return IsActive && (ExpiryDate == null || ExpiryDate > DateTime.UtcNow);
    }
}
