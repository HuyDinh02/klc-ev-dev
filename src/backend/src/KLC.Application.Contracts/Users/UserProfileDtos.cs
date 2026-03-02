using System;
using System.ComponentModel.DataAnnotations;

namespace KLC.Users;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public Guid IdentityUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? AvatarUrl { get; set; }
    public string PreferredLanguage { get; set; } = "vi";
    public bool IsNotificationsEnabled { get; set; }
    public decimal WalletBalance { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Statistics
    public int TotalSessions { get; set; }
    public decimal TotalEnergyKwh { get; set; }
    public decimal TotalSpent { get; set; }
}

public class UpdateProfileDto
{
    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [StringLength(5)]
    public string? PreferredLanguage { get; set; }

    public bool? IsNotificationsEnabled { get; set; }
}

public class UpdatePhoneDto
{
    [Required]
    [StringLength(20)]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class UpdateEmailDto
{
    [Required]
    [StringLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class VerifyPhoneDto
{
    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;
}

public class VerifyEmailDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class UserStatisticsDto
{
    public int TotalSessions { get; set; }
    public decimal TotalEnergyKwh { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageSessionDurationMinutes { get; set; }
    public decimal AverageEnergyPerSession { get; set; }
    public int SessionsThisMonth { get; set; }
    public decimal EnergyThisMonth { get; set; }
}
