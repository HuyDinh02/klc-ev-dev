using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Auth;

/// <summary>
/// Application service for driver authentication business logic: registration,
/// phone verification, login, password management. The BFF delegates to this service.
/// </summary>
public interface IAuthAppService : IApplicationService
{
    Task<RegisterResultDto> RegisterAsync(RegisterInput input);
    Task<VerifyResultDto> VerifyPhoneAsync(string phoneNumber, string otp);
    Task<VerifyResultDto> VerifyPhoneWithFirebaseAsync(string idToken);
    Task ResendOtpAsync(string phoneNumber);
    Task<LoginResultDto> LoginAsync(string phoneNumber, string password);
    Task<LoginResultDto> FirebasePhoneLoginAsync(string idToken, string? fullName);
    Task<LoginResultDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(Guid userId, string? refreshToken);
    Task ForgotPasswordAsync(string phoneNumber);
    Task ResetPasswordAsync(string phoneNumber, string otp, string newPassword);
    Task ResetPasswordWithFirebaseAsync(string idToken, string newPassword);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<LoginResultDto> SocialLoginAsync(string provider, string accessToken);
}
