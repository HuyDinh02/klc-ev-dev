using System;
using KLC.Enums;

namespace KLC.Auth;

public class RegisterInput
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class RegisterResultDto
{
    public bool Success { get; set; }
    public Guid? UserId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public class VerifyResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class LoginResultDto
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int? ExpiresIn { get; set; }
    public AuthUserDto? User { get; set; }
    public string? Error { get; set; }
}

public class AuthUserDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsPhoneVerified { get; set; }
    public MembershipTier MembershipTier { get; set; }
    public decimal WalletBalance { get; set; }
}
