using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KLC.Auditing;
using KLC.Configuration;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Volo.Abp.Identity;

namespace KLC.Driver.Services;

public interface IAuthBffService
{
    Task<RegisterResultDto> RegisterAsync(RegisterRequest request);
    Task<VerifyOtpResultDto> VerifyPhoneAsync(VerifyPhoneRequest request);
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
    private readonly KLCDbContext _dbContext;
    private readonly IdentityUserManager _userManager;
    private readonly IDatabase _redis;
    private readonly IConfiguration _configuration;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthBffService> _logger;
    private readonly KLC.Notifications.ISmsService _smsService;
    private readonly IAuditEventLogger _auditLogger;

    private const int OtpLength = 6;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);

    public AuthBffService(
        KLCDbContext dbContext,
        IdentityUserManager userManager,
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthBffService> logger,
        KLC.Notifications.ISmsService smsService,
        IAuditEventLogger auditLogger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _redis = redis.GetDatabase();
        _configuration = configuration;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _smsService = smsService;
        _auditLogger = auditLogger;
    }

    public async Task<RegisterResultDto> RegisterAsync(RegisterRequest request)
    {
        // Check if phone already registered
        var existing = await _dbContext.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && !u.IsDeleted);

        if (existing != null)
        {
            return new RegisterResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.PhoneAlreadyRegistered };
        }

        // Create ABP IdentityUser
        var identityUser = new IdentityUser(
            Guid.NewGuid(),
            request.PhoneNumber, // Use phone as username
            $"{request.PhoneNumber}@klc.local"); // Placeholder email

        var identityResult = await _userManager.CreateAsync(identityUser, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Identity user creation failed: {Errors}", errors);
            return new RegisterResultDto { Success = false, Error = errors };
        }

        // Create AppUser
        var appUser = new AppUser(
            Guid.NewGuid(),
            identityUser.Id,
            request.FullName,
            request.PhoneNumber);

        await _dbContext.AppUsers.AddAsync(appUser);

        // Create default notification preferences
        var prefs = new Notifications.NotificationPreference(Guid.NewGuid(), identityUser.Id);
        await _dbContext.NotificationPreferences.AddAsync(prefs);

        await _dbContext.SaveChangesAsync();

        // Generate and store OTP
        var otp = GenerateOtp();
        await StoreOtp(request.PhoneNumber, otp);

        await _smsService.SendAsync(request.PhoneNumber, $"Your KLC verification code is: {otp}");

        return new RegisterResultDto
        {
            Success = true,
            UserId = identityUser.Id,
            Message = "Registration successful. Please verify your phone number."
        };
    }

    public async Task<VerifyOtpResultDto> VerifyPhoneAsync(VerifyPhoneRequest request)
    {
        var storedOtp = await _redis.StringGetAsync($"otp:{request.PhoneNumber}");
        if (storedOtp.IsNullOrEmpty)
        {
            return new VerifyOtpResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.OtpExpired };
        }

        if (storedOtp.ToString() != request.Otp)
        {
            return new VerifyOtpResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.InvalidOtp };
        }

        // Mark phone as verified
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && !u.IsDeleted);

        if (appUser != null)
        {
            appUser.VerifyPhone();
            await _dbContext.SaveChangesAsync();
        }

        // Remove OTP
        await _redis.KeyDeleteAsync($"otp:{request.PhoneNumber}");

        return new VerifyOtpResultDto { Success = true };
    }

    public async Task ResendOtpAsync(ResendOtpRequest request)
    {
        var otp = GenerateOtp();
        await StoreOtp(request.PhoneNumber, otp);
        await _smsService.SendAsync(request.PhoneNumber, $"Your KLC verification code is: {otp}");
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequest request)
    {
        // Find user by phone number
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && !u.IsDeleted);

        if (appUser == null)
        {
            _auditLogger.LogAuthEvent("LoginFailed", details: $"Phone={request.PhoneNumber}, Reason=UserNotFound");
            return new LoginResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.InvalidCredentials };
        }

        if (!appUser.IsActive)
        {
            _auditLogger.LogAuthEvent("LoginFailed", appUser.IdentityUserId.ToString(), details: "AccountSuspended");
            return new LoginResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.AccountSuspended };
        }

        // Verify password via ABP IdentityUser
        var identityUser = await _userManager.FindByIdAsync(appUser.IdentityUserId.ToString());
        if (identityUser == null)
        {
            _auditLogger.LogAuthEvent("LoginFailed", appUser.IdentityUserId.ToString(), details: "IdentityUserNotFound");
            return new LoginResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.InvalidCredentials };
        }

        var passwordValid = await _userManager.CheckPasswordAsync(identityUser, request.Password);
        if (!passwordValid)
        {
            _auditLogger.LogAuthEvent("LoginFailed", appUser.IdentityUserId.ToString(), details: "InvalidPassword");
            return new LoginResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.InvalidCredentials };
        }

        appUser.RecordLogin();
        await _dbContext.SaveChangesAsync();

        // Generate tokens
        var accessToken = GenerateAccessToken(appUser);
        var refreshToken = GenerateRefreshToken();
        await StoreRefreshToken(appUser.IdentityUserId, refreshToken);

        _auditLogger.LogAuthEvent("LoginSuccess", appUser.IdentityUserId.ToString());

        return new LoginResultDto
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = GetAccessTokenExpirySeconds(),
            User = MapToUserDto(appUser)
        };
    }

    public async Task<LoginResultDto> FirebasePhoneLoginAsync(FirebasePhoneLoginRequest request)
    {
        // Step 1: Verify Firebase ID token
        FirebaseAdmin.Auth.FirebaseToken? firebaseToken;
        try
        {
            var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
            if (auth == null)
            {
                _logger.LogWarning("Firebase not initialized — cannot verify phone auth token");
                return new LoginResultDto { Success = false, Error = "Firebase Auth not configured" };
            }
            firebaseToken = await auth.VerifyIdTokenAsync(request.IdToken);
        }
        catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Firebase token verification failed");
            return new LoginResultDto { Success = false, Error = "Invalid or expired Firebase token" };
        }

        // Step 2: Extract phone number from Firebase token
        var firebaseUid = firebaseToken.Uid;
        var phoneNumber = firebaseToken.Claims.TryGetValue("phone_number", out var phone)
            ? phone?.ToString()
            : null;

        if (string.IsNullOrEmpty(phoneNumber))
        {
            _logger.LogWarning("Firebase token has no phone_number claim: uid={Uid}", firebaseUid);
            return new LoginResultDto { Success = false, Error = "Phone number not found in Firebase token" };
        }

        // Normalize VN phone: +84901234001 → 0901234001
        var localPhone = phoneNumber.StartsWith("+84")
            ? "0" + phoneNumber[3..]
            : phoneNumber;

        _logger.LogInformation("Firebase phone auth: uid={Uid}, phone={Phone}", firebaseUid, localPhone);

        // Step 3: Find or create AppUser
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == localPhone && !u.IsDeleted);

        if (appUser == null)
        {
            // Auto-register new user from Firebase
            var identityUser = new Volo.Abp.Identity.IdentityUser(
                Guid.NewGuid(), localPhone, $"{localPhone}@klc.local");
            identityUser.SetPhoneNumber(localPhone, true);
            await _userManager.CreateAsync(identityUser, $"Firebase_{Guid.NewGuid():N}"[..20]);

            appUser = new AppUser(
                Guid.NewGuid(),
                identityUser.Id,
                request.FullName ?? localPhone,
                localPhone);
            appUser.VerifyPhone();
            _dbContext.AppUsers.Add(appUser);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Auto-registered Firebase user: phone={Phone}, appUserId={Id}", localPhone, appUser.Id);
            _auditLogger.LogAuthEvent("FirebasePhoneRegister", userId: appUser.IdentityUserId.ToString(), details: $"Phone={localPhone}");
        }

        if (!appUser.IsActive)
        {
            return new LoginResultDto { Success = false, Error = "Account is suspended" };
        }

        // Step 4: Mark phone as verified (if not already)
        if (!appUser.IsPhoneVerified)
        {
            appUser.VerifyPhone();
            await _dbContext.SaveChangesAsync();
        }

        // Step 5: Record login + generate KLC tokens
        appUser.RecordLogin();
        await _dbContext.SaveChangesAsync();

        var accessToken = GenerateAccessToken(appUser);
        var refreshToken = GenerateRefreshToken();
        await StoreRefreshToken(appUser.IdentityUserId, refreshToken);

        _auditLogger.LogAuthEvent("FirebasePhoneLogin", userId: appUser.IdentityUserId.ToString(), details: $"Phone={localPhone}");

        return new LoginResultDto
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = GetAccessTokenExpirySeconds(),
            User = MapToUserDto(appUser)
        };
    }

    public async Task<LoginResultDto> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Validate refresh token from Redis
        var storedUserId = await _redis.StringGetAsync($"refresh:{request.RefreshToken}");
        if (storedUserId.IsNullOrEmpty)
        {
            _auditLogger.LogAuthEvent("TokenRefreshFailed", details: "InvalidRefreshToken");
            return new LoginResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.InvalidRefreshToken };
        }

        var userId = Guid.Parse(storedUserId.ToString());
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.IdentityUserId == userId && !u.IsDeleted);

        if (appUser == null || !appUser.IsActive)
        {
            _auditLogger.LogAuthEvent("TokenRefreshFailed", userId.ToString(), details: "AccountSuspendedOrNotFound");
            return new LoginResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.AccountSuspended };
        }

        // Revoke old token, generate new
        await _redis.KeyDeleteAsync($"refresh:{request.RefreshToken}");
        var accessToken = GenerateAccessToken(appUser);
        var refreshToken = GenerateRefreshToken();
        await StoreRefreshToken(userId, refreshToken);

        _auditLogger.LogAuthEvent("TokenRefreshSuccess", userId.ToString());

        return new LoginResultDto
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = GetAccessTokenExpirySeconds(),
            User = MapToUserDto(appUser)
        };
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _redis.KeyDeleteAsync($"refresh:{refreshToken}");
        }

        _auditLogger.LogAuthEvent("Logout", userId.ToString());
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var appUser = await _dbContext.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && !u.IsDeleted);

        if (appUser == null) return; // Don't reveal if phone exists

        var otp = GenerateOtp();
        await StoreOtp($"reset:{request.PhoneNumber}", otp);
        await _smsService.SendAsync(request.PhoneNumber, $"Your KLC password reset code is: {otp}");
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var storedOtp = await _redis.StringGetAsync($"otp:reset:{request.PhoneNumber}");
        if (storedOtp.IsNullOrEmpty || storedOtp.ToString() != request.Otp)
        {
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Auth.InvalidOtp);
        }

        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber && !u.IsDeleted);

        if (appUser == null)
        {
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);
        }

        var identityUser = await _userManager.FindByIdAsync(appUser.IdentityUserId.ToString());
        if (identityUser == null)
        {
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);
        }

        // Direct hash — same approach as Firebase reset (no token provider in BFF)
        var passwordValidator = _userManager.PasswordValidators.FirstOrDefault();
        if (passwordValidator != null)
        {
            var validateResult = await passwordValidator.ValidateAsync(_userManager, identityUser, request.NewPassword);
            if (!validateResult.Succeeded)
            {
                var errors = string.Join(", ", validateResult.Errors.Select(e => e.Description));
                throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.PasswordResetFailed, errors);
            }
        }

        // Set password hash directly via EF Core (ABP's PasswordHash setter is protected)
        var newHash = _userManager.PasswordHasher.HashPassword(identityUser, request.NewPassword);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"AbpUsers\" SET \"PasswordHash\" = {newHash} WHERE \"Id\" = {identityUser.Id}");

        await _redis.KeyDeleteAsync($"otp:reset:{request.PhoneNumber}");
    }

    public async Task ResetPasswordWithFirebaseAsync(FirebaseResetPasswordRequest request)
    {
        // Verify Firebase ID token (proves the user owns the phone number)
        FirebaseAdmin.Auth.FirebaseToken? firebaseToken;
        try
        {
            var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
            if (auth == null)
                throw new Volo.Abp.BusinessException("FIREBASE_NOT_CONFIGURED", "Firebase Auth not configured");

            firebaseToken = await auth.VerifyIdTokenAsync(request.IdToken);
        }
        catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Firebase token verification failed for password reset");
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Auth.InvalidOtp, "Invalid or expired Firebase token");
        }

        // Extract phone number from Firebase token
        var phoneNumber = firebaseToken.Claims.TryGetValue("phone_number", out var phone)
            ? phone?.ToString() : null;

        if (string.IsNullOrEmpty(phoneNumber))
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials, "Phone number not found in token");

        var localPhone = phoneNumber.StartsWith("+84") ? "0" + phoneNumber[3..] : phoneNumber;

        _logger.LogInformation("Firebase password reset: phone={Phone}", localPhone);

        // Override phone from request if token is valid (token is the source of truth)
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == localPhone && !u.IsDeleted);

        if (appUser == null)
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);

        var identityUser = await _userManager.FindByIdAsync(appUser.IdentityUserId.ToString());
        if (identityUser == null)
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);

        // Directly hash and set the new password.
        // Can't use GeneratePasswordResetToken (no token provider in BFF).
        // Can't use RemovePassword+AddPassword (breaks SecurityStamp → login fails).
        // Direct hash is safe here because Firebase ID token already proves identity.
        var passwordValidator = _userManager.PasswordValidators.FirstOrDefault();
        if (passwordValidator != null)
        {
            var validateResult = await passwordValidator.ValidateAsync(_userManager, identityUser, request.NewPassword);
            if (!validateResult.Succeeded)
            {
                var errors = string.Join(", ", validateResult.Errors.Select(e => e.Description));
                throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.PasswordResetFailed, errors);
            }
        }

        // Set password hash directly via EF Core (ABP's PasswordHash setter is protected)
        var newHash = _userManager.PasswordHasher.HashPassword(identityUser, request.NewPassword);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"AbpUsers\" SET \"PasswordHash\" = {newHash} WHERE \"Id\" = {identityUser.Id}");

        _logger.LogInformation("Password reset successful via Firebase for {Phone}", localPhone);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.IdentityUserId == userId && !u.IsDeleted);

        if (appUser == null)
        {
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);
        }

        var identityUser = await _userManager.FindByIdAsync(appUser.IdentityUserId.ToString());
        if (identityUser == null)
        {
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);
        }

        var result = await _userManager.ChangePasswordAsync(identityUser, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new Volo.Abp.BusinessException(KLCDomainErrorCodes.Profile.PasswordChangeFailed);
        }
    }

    public async Task<LoginResultDto> SocialLoginAsync(SocialLoginRequest request)
    {
        // Social login requires provider-specific token validation (Google Sign-In, Apple Sign-In, Facebook Login)
        // Will be implemented with Firebase Auth or direct provider SDKs in a future iteration
        _logger.LogInformation("Social login attempt: provider={Provider}", request.Provider);

        return new LoginResultDto
        {
            Success = false,
            Error = "Social login not yet implemented for " + request.Provider
        };
    }

    #region Private Helpers

    private string GenerateOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    private async Task StoreOtp(string key, string otp)
    {
        await _redis.StringSetAsync($"otp:{key}", otp, OtpTtl);
    }

    private string GenerateAccessToken(AppUser user)
    {
        if (string.IsNullOrEmpty(_jwtSettings.SecretKey))
            throw new InvalidOperationException("Jwt:SecretKey is not configured.");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.IdentityUserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("phone", user.PhoneNumber ?? ""),
            new Claim("name", user.FullName),
        };

        var expiry = TimeSpan.FromMinutes(_jwtSettings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetAccessTokenExpirySeconds()
    {
        return _jwtSettings.ExpiryMinutes * 60;
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private async Task StoreRefreshToken(Guid userId, string refreshToken)
    {
        await _redis.StringSetAsync($"refresh:{refreshToken}", userId.ToString(), RefreshTokenTtl);
    }

    private static AuthUserDto MapToUserDto(AppUser user)
    {
        return new AuthUserDto
        {
            UserId = user.IdentityUserId,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            IsPhoneVerified = user.IsPhoneVerified,
            MembershipTier = user.MembershipTier,
            WalletBalance = user.WalletBalance
        };
    }

    #endregion
}

// DTOs
public record RegisterRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}

public record RegisterResultDto
{
    public bool Success { get; init; }
    public Guid? UserId { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
}

public record VerifyPhoneRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
    public string Otp { get; init; } = string.Empty;
}

public record VerifyOtpResultDto
{
    public bool Success { get; init; }
    public string? Error { get; init; }
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

public record LoginResultDto
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public int? ExpiresIn { get; init; }
    public AuthUserDto? User { get; init; }
    public string? Error { get; init; }
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

public record AuthUserDto
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsPhoneVerified { get; init; }
    public MembershipTier MembershipTier { get; init; }
    public decimal WalletBalance { get; init; }
}
