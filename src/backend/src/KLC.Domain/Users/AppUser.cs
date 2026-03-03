using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace KLC.Users;

/// <summary>
/// Extended user entity for mobile app users.
/// Note: This is a separate entity from ABP IdentityUser for additional fields.
/// The UserId links to the IdentityUser.
/// </summary>
public class AppUser : FullAuditedEntity<Guid>
{
    /// <summary>
    /// Reference to the ABP IdentityUser.
    /// </summary>
    public Guid IdentityUserId { get; private set; }

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// User's phone number.
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Whether the phone number is verified.
    /// </summary>
    public bool IsPhoneVerified { get; private set; }

    /// <summary>
    /// Whether the email is verified.
    /// </summary>
    public bool IsEmailVerified { get; private set; }

    /// <summary>
    /// User's avatar URL.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// User's preferred language (vi, en).
    /// </summary>
    public string PreferredLanguage { get; private set; } = "vi";

    /// <summary>
    /// Whether the user has enabled notifications.
    /// </summary>
    public bool IsNotificationsEnabled { get; private set; }

    /// <summary>
    /// Firebase Cloud Messaging token for push notifications.
    /// </summary>
    public string? FcmToken { get; private set; }

    /// <summary>
    /// User's wallet balance in VND.
    /// </summary>
    public decimal WalletBalance { get; private set; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Last login timestamp.
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    protected AppUser()
    {
        // Required by EF Core
    }

    public AppUser(
        Guid id,
        Guid identityUserId,
        string fullName,
        string? phoneNumber = null,
        string? email = null)
        : base(id)
    {
        IdentityUserId = identityUserId;
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Email = email;
        IsPhoneVerified = false;
        IsEmailVerified = false;
        IsNotificationsEnabled = true;
        IsActive = true;
        WalletBalance = 0;
        PreferredLanguage = "vi";
    }

    public void UpdateProfile(string fullName, string? avatarUrl = null)
    {
        FullName = fullName;
        AvatarUrl = avatarUrl;
    }

    public void SetPhoneNumber(string phoneNumber, bool isVerified = false)
    {
        PhoneNumber = phoneNumber;
        IsPhoneVerified = isVerified;
    }

    public void VerifyPhone()
    {
        IsPhoneVerified = true;
    }

    public void SetEmail(string email, bool isVerified = false)
    {
        Email = email;
        IsEmailVerified = isVerified;
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
    }

    public void SetPreferredLanguage(string language)
    {
        if (language != "vi" && language != "en")
            throw new ArgumentException("Language must be 'vi' or 'en'", nameof(language));
        PreferredLanguage = language;
    }

    public void EnableNotifications()
    {
        IsNotificationsEnabled = true;
    }

    public void DisableNotifications()
    {
        IsNotificationsEnabled = false;
    }

    public void UpdateFcmToken(string? token)
    {
        FcmToken = token;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }

    public void AddToWallet(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
        WalletBalance += amount;
    }

    public void DeductFromWallet(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
        if (amount > WalletBalance)
            throw new BusinessException(KLCDomainErrorCodes.Wallet.InsufficientBalance);
        WalletBalance -= amount;
    }
}
