using System;
using KLC.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Users;

/// <summary>
/// Represents a mobile device push notification token.
/// Supports multi-device per user (replaces single FcmToken on AppUser).
/// </summary>
public class DeviceToken : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the AppUser.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// FCM or APNs token string.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Device platform (iOS/Android).
    /// </summary>
    public DevicePlatform Platform { get; private set; }

    /// <summary>
    /// Whether this token is still active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When this token was registered.
    /// </summary>
    public DateTime RegisteredAt { get; private set; }

    protected DeviceToken()
    {
        // Required by EF Core
    }

    public DeviceToken(
        Guid id,
        Guid userId,
        string token,
        DevicePlatform platform)
        : base(id)
    {
        UserId = userId;
        Token = Check.NotNullOrWhiteSpace(token, nameof(token), maxLength: 500);
        Platform = platform;
        IsActive = true;
        RegisteredAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void UpdateToken(string token)
    {
        Token = Check.NotNullOrWhiteSpace(token, nameof(token), maxLength: 500);
        RegisteredAt = DateTime.UtcNow;
        IsActive = true;
    }
}
