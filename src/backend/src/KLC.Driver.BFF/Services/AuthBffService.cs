using KLC.Auth;

namespace KLC.Driver.Services;

public interface IAuthBffService
{
    Task<RegisterResultDto> RegisterAsync(RegisterRequest request);
    Task<VerifyResultDto> VerifyPhoneAsync(VerifyPhoneRequest request);
    Task<VerifyResultDto> VerifyPhoneWithFirebaseAsync(FirebaseVerifyPhoneRequest request);
    Task ResendOtpAsync(ResendOtpRequest request);
    Task<LoginResultDto> LoginAsync(LoginRequest request);
    Task<LoginResultDto> FirebasePhoneLoginAsync(FirebasePhoneLoginRequest request);
    Task<LoginResultDto> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(Guid userId, string? refreshToken);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ResetPasswordWithFirebaseAsync(FirebaseResetPasswordRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<LoginResultDto> SocialLoginAsync(SocialLoginRequest request);
}

public class AuthBffService : IAuthBffService
{
    private readonly IAuthAppService _authAppService;

    public AuthBffService(IAuthAppService authAppService)
    {
        _authAppService = authAppService;
    }

    public async Task<RegisterResultDto> RegisterAsync(RegisterRequest request)
    {
        return await _authAppService.RegisterAsync(new RegisterInput
        {
            PhoneNumber = request.PhoneNumber,
            Password = request.Password,
            FullName = request.FullName
        });
    }

    public async Task<VerifyResultDto> VerifyPhoneAsync(VerifyPhoneRequest request)
    {
        return await _authAppService.VerifyPhoneAsync(request.PhoneNumber, request.Otp);
    }

    public async Task<VerifyResultDto> VerifyPhoneWithFirebaseAsync(FirebaseVerifyPhoneRequest request)
    {
        return await _authAppService.VerifyPhoneWithFirebaseAsync(request.IdToken);
    }

    public async Task ResendOtpAsync(ResendOtpRequest request)
    {
        await _authAppService.ResendOtpAsync(request.PhoneNumber);
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequest request)
    {
        return await _authAppService.LoginAsync(request.PhoneNumber, request.Password);
    }

    public async Task<LoginResultDto> FirebasePhoneLoginAsync(FirebasePhoneLoginRequest request)
    {
        return await _authAppService.FirebasePhoneLoginAsync(request.IdToken, request.FullName);
    }

    public async Task<LoginResultDto> RefreshTokenAsync(RefreshTokenRequest request)
    {
        return await _authAppService.RefreshTokenAsync(request.RefreshToken);
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken)
    {
        await _authAppService.LogoutAsync(userId, refreshToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        await _authAppService.ForgotPasswordAsync(request.PhoneNumber);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        await _authAppService.ResetPasswordAsync(request.PhoneNumber, request.Otp, request.NewPassword);
    }

    public async Task ResetPasswordWithFirebaseAsync(FirebaseResetPasswordRequest request)
    {
        await _authAppService.ResetPasswordWithFirebaseAsync(request.IdToken, request.NewPassword);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        await _authAppService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
    }

    public async Task<LoginResultDto> SocialLoginAsync(SocialLoginRequest request)
    {
        return await _authAppService.SocialLoginAsync(request.Provider, request.AccessToken);
    }
}

// BFF request DTOs (preserved for API contract compatibility)
public record RegisterRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}

public record VerifyPhoneRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Otp { get; init; } = string.Empty;
}

public record ResendOtpRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
}

public record LoginRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record ForgotPasswordRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
}

public record ResetPasswordRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Otp { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public record FirebaseResetPasswordRequest
{
    /// <summary>Firebase ID token proving phone ownership</summary>
    public string IdToken { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public record FirebaseVerifyPhoneRequest
{
    /// <summary>Firebase ID token proving phone ownership (from signInWithPhoneNumber + confirm OTP)</summary>
    public string IdToken { get; init; } = string.Empty;
}

public record ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public record SocialLoginRequest
{
    public string Provider { get; init; } = string.Empty; // google, apple, facebook
    public string AccessToken { get; init; } = string.Empty;
}

public record FirebasePhoneLoginRequest
{
    /// <summary>Firebase ID token from client-side phone auth</summary>
    public string IdToken { get; init; } = string.Empty;
    /// <summary>Optional user name for auto-registration</summary>
    public string? FullName { get; init; }
}

// Note: BFF-specific DTO types (RegisterResultDto, VerifyOtpResultDto, LoginResultDto, AuthUserDto)
// have been replaced by Application layer DTOs from KLC.Auth namespace.
// The following type aliases preserve backward compatibility for BFF endpoint code
// that may reference the old VerifyOtpResultDto name.
// VerifyOtpResultDto was renamed to VerifyResultDto in the Application layer.
