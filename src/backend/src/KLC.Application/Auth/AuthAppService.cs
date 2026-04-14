using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using KLC.Auditing;
using KLC.Configuration;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Notifications;
using KLC.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.Identity;

namespace KLC.Auth;

/// <summary>
/// Application service encapsulating all driver authentication business logic:
/// registration, phone verification, login, password management.
/// Shared between Admin API and Driver BFF.
/// </summary>
public class AuthAppService : IAuthAppService
{
    private readonly KLCDbContext _dbContext;
    private readonly IdentityUserManager _userManager;
    private readonly IDatabase _redis;
    private readonly IConfiguration _configuration;
    private readonly JwtSettings _jwtSettings;
    private readonly ISmsService _smsService;
    private readonly IAuditEventLogger _auditLogger;
    private readonly ILogger<AuthAppService> _logger;

    private const int OtpLength = 6;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);

    public AuthAppService(
        KLCDbContext dbContext,
        IdentityUserManager userManager,
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        IOptions<JwtSettings> jwtSettings,
        ISmsService smsService,
        IAuditEventLogger auditLogger,
        ILogger<AuthAppService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _redis = redis.GetDatabase();
        _configuration = configuration;
        _jwtSettings = jwtSettings.Value;
        _smsService = smsService;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<RegisterResultDto> RegisterAsync(RegisterInput input)
    {
        // Check if phone already registered
        var existing = await _dbContext.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == input.PhoneNumber && !u.IsDeleted);

        if (existing != null)
        {
            return new RegisterResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.PhoneAlreadyRegistered };
        }

        // Create ABP IdentityUser
        var identityUser = new IdentityUser(
            Guid.NewGuid(),
            input.PhoneNumber, // Use phone as username
            $"{input.PhoneNumber}@klc.local"); // Placeholder email

        var identityResult = await _userManager.CreateAsync(identityUser, input.Password);
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
            input.FullName,
            input.PhoneNumber);

        await _dbContext.AppUsers.AddAsync(appUser);

        // Create default notification preferences
        var prefs = new KLC.Notifications.NotificationPreference(Guid.NewGuid(), identityUser.Id);
        await _dbContext.NotificationPreferences.AddAsync(prefs);

        await _dbContext.SaveChangesAsync();

        // Generate and store OTP
        var otp = GenerateOtp();
        await StoreOtp(input.PhoneNumber, otp);

        await _smsService.SendAsync(input.PhoneNumber, $"Your KLC verification code is: {otp}");

        return new RegisterResultDto
        {
            Success = true,
            UserId = identityUser.Id,
            Message = "Registration successful. Please verify your phone number."
        };
    }

    public async Task<VerifyResultDto> VerifyPhoneAsync(string phoneNumber, string otp)
    {
        var storedOtp = await _redis.StringGetAsync($"otp:{phoneNumber}");
        if (storedOtp.IsNullOrEmpty)
        {
            return new VerifyResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.OtpExpired };
        }

        if (storedOtp.ToString() != otp)
        {
            return new VerifyResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.InvalidOtp };
        }

        // Mark phone as verified
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted);

        if (appUser != null)
        {
            appUser.VerifyPhone();
            await _dbContext.SaveChangesAsync();
        }

        // Remove OTP
        await _redis.KeyDeleteAsync($"otp:{phoneNumber}");

        return new VerifyResultDto { Success = true };
    }

    public async Task<VerifyResultDto> VerifyPhoneWithFirebaseAsync(string idToken)
    {
        // Verify Firebase ID token
        FirebaseAdmin.Auth.FirebaseToken? firebaseToken;
        try
        {
            var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
            if (auth == null)
                return new VerifyResultDto { Success = false, Error = "Firebase Auth not configured" };

            firebaseToken = await auth.VerifyIdTokenAsync(idToken);
        }
        catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Firebase token verification failed for phone verify");
            return new VerifyResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.InvalidOtp };
        }

        // Extract phone from token
        var phoneNumber = firebaseToken.Claims.TryGetValue("phone_number", out var phone)
            ? phone?.ToString() : null;

        if (string.IsNullOrEmpty(phoneNumber))
            return new VerifyResultDto { Success = false, Error = "Phone number not found in Firebase token" };

        var localPhone = phoneNumber.StartsWith("+84") ? "0" + phoneNumber[3..] : phoneNumber;

        _logger.LogInformation("Firebase phone verify: phone={Phone}", localPhone);

        // Mark phone as verified
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == localPhone && !u.IsDeleted);

        if (appUser != null && !appUser.IsPhoneVerified)
        {
            appUser.VerifyPhone();
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Phone verified via Firebase for {Phone}", localPhone);
        }

        return new VerifyResultDto { Success = true };
    }

    public async Task ResendOtpAsync(string phoneNumber)
    {
        var otp = GenerateOtp();
        await StoreOtp(phoneNumber, otp);
        await _smsService.SendAsync(phoneNumber, $"Your KLC verification code is: {otp}");
    }

    public async Task<LoginResultDto> LoginAsync(string phoneNumber, string password)
    {
        // Find user by phone number
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted);

        if (appUser == null)
        {
            _auditLogger.LogAuthEvent("LoginFailed", details: $"Phone={phoneNumber}, Reason=UserNotFound");
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

        var passwordValid = await _userManager.CheckPasswordAsync(identityUser, password);
        if (!passwordValid)
        {
            _auditLogger.LogAuthEvent("LoginFailed", appUser.IdentityUserId.ToString(), details: "InvalidPassword");
            return new LoginResultDto { Success = false, Error = KLCDomainErrorCodes.Auth.InvalidCredentials };
        }

        // Atomic update — avoids AbpDbConcurrencyException when multiple
        // concurrent logins hit the same user row (e.g., token refresh + login)
        await _dbContext.Set<AppUser>()
            .Where(u => u.Id == appUser.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTime.UtcNow));

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

    public async Task<LoginResultDto> FirebasePhoneLoginAsync(string idToken, string? fullName)
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
            firebaseToken = await auth.VerifyIdTokenAsync(idToken);
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

        // Normalize VN phone: +84901234001 -> 0901234001
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
            var identityUser = new IdentityUser(
                Guid.NewGuid(), localPhone, $"{localPhone}@klc.local");
            identityUser.SetPhoneNumber(localPhone, true);
            await _userManager.CreateAsync(identityUser, $"Firebase_{Guid.NewGuid():N}"[..20]);

            appUser = new AppUser(
                Guid.NewGuid(),
                identityUser.Id,
                fullName ?? localPhone,
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

    public async Task<LoginResultDto> RefreshTokenAsync(string refreshToken)
    {
        // Validate refresh token from Redis
        var storedUserId = await _redis.StringGetAsync($"refresh:{refreshToken}");
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
        await _redis.KeyDeleteAsync($"refresh:{refreshToken}");
        var accessToken = GenerateAccessToken(appUser);
        var newRefreshToken = GenerateRefreshToken();
        await StoreRefreshToken(userId, newRefreshToken);

        _auditLogger.LogAuthEvent("TokenRefreshSuccess", userId.ToString());

        return new LoginResultDto
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
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

    public async Task ForgotPasswordAsync(string phoneNumber)
    {
        var appUser = await _dbContext.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted);

        if (appUser == null) return; // Don't reveal if phone exists

        var otp = GenerateOtp();
        await StoreOtp($"reset:{phoneNumber}", otp);
        await _smsService.SendAsync(phoneNumber, $"Your KLC password reset code is: {otp}");
    }

    public async Task ResetPasswordAsync(string phoneNumber, string otp, string newPassword)
    {
        var storedOtp = await _redis.StringGetAsync($"otp:reset:{phoneNumber}");
        if (storedOtp.IsNullOrEmpty || storedOtp.ToString() != otp)
        {
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidOtp);
        }

        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted);

        if (appUser == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);
        }

        var identityUser = await _userManager.FindByIdAsync(appUser.IdentityUserId.ToString());
        if (identityUser == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);
        }

        // BFF can't use GeneratePasswordResetTokenAsync (ABP token provider not registered).
        // Hash directly + clear EF tracker so next FindByIdAsync gets fresh data.
        var validators = _userManager.PasswordValidators;
        foreach (var validator in validators)
        {
            var vr = await validator.ValidateAsync(_userManager, identityUser, newPassword);
            if (!vr.Succeeded)
                throw new BusinessException(KLCDomainErrorCodes.PasswordResetFailed,
                    string.Join(", ", vr.Errors.Select(e => e.Description)));
        }

        var newHash = _userManager.PasswordHasher.HashPassword(identityUser, newPassword);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"AbpUsers\" SET \"PasswordHash\" = {newHash} WHERE \"Id\" = {identityUser.Id}");
        _dbContext.ChangeTracker.Clear(); // Invalidate cached entities

        await _redis.KeyDeleteAsync($"otp:reset:{phoneNumber}");
    }

    public async Task ResetPasswordWithFirebaseAsync(string idToken, string newPassword)
    {
        // Verify Firebase ID token (proves the user owns the phone number)
        FirebaseAdmin.Auth.FirebaseToken? firebaseToken;
        try
        {
            var auth = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance;
            if (auth == null)
                throw new BusinessException("FIREBASE_NOT_CONFIGURED", "Firebase Auth not configured");

            firebaseToken = await auth.VerifyIdTokenAsync(idToken);
        }
        catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Firebase token verification failed for password reset");
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidOtp, "Invalid or expired Firebase token");
        }

        // Extract phone number from Firebase token
        var phoneNumber = firebaseToken.Claims.TryGetValue("phone_number", out var phone)
            ? phone?.ToString() : null;

        if (string.IsNullOrEmpty(phoneNumber))
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials, "Phone number not found in token");

        var localPhone = phoneNumber.StartsWith("+84") ? "0" + phoneNumber[3..] : phoneNumber;

        _logger.LogInformation("Firebase password reset: phone={Phone}", localPhone);

        // Override phone from request if token is valid (token is the source of truth)
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.PhoneNumber == localPhone && !u.IsDeleted);

        if (appUser == null)
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);

        var identityUser = await _userManager.FindByIdAsync(appUser.IdentityUserId.ToString());
        if (identityUser == null)
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);

        // BFF can't use GeneratePasswordResetTokenAsync (ABP token provider not registered).
        // Hash directly + clear EF tracker so next FindByIdAsync gets fresh data.
        var validators = _userManager.PasswordValidators;
        foreach (var validator in validators)
        {
            var vr = await validator.ValidateAsync(_userManager, identityUser, newPassword);
            if (!vr.Succeeded)
            {
                var errors = string.Join(", ", vr.Errors.Select(e => e.Description));
                _logger.LogWarning("Password reset failed for {Phone}: {Errors}", localPhone, errors);
                throw new BusinessException(KLCDomainErrorCodes.PasswordResetFailed, errors);
            }
        }

        var newHash = _userManager.PasswordHasher.HashPassword(identityUser, newPassword);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"AbpUsers\" SET \"PasswordHash\" = {newHash} WHERE \"Id\" = {identityUser.Id}");
        _dbContext.ChangeTracker.Clear();

        _logger.LogInformation("Password reset successful via Firebase for {Phone}", localPhone);
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var appUser = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.IdentityUserId == userId && !u.IsDeleted);

        if (appUser == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);
        }

        var identityUser = await _userManager.FindByIdAsync(appUser.IdentityUserId.ToString());
        if (identityUser == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);
        }

        var result = await _userManager.ChangePasswordAsync(identityUser, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            throw new BusinessException(KLCDomainErrorCodes.Profile.PasswordChangeFailed);
        }
    }

    public async Task<LoginResultDto> SocialLoginAsync(string provider, string accessToken)
    {
        // Social login requires provider-specific token validation (Google Sign-In, Apple Sign-In, Facebook Login)
        // Will be implemented with Firebase Auth or direct provider SDKs in a future iteration
        _logger.LogInformation("Social login attempt: provider={Provider}", provider);

        return new LoginResultDto
        {
            Success = false,
            Error = "Social login not yet implemented for " + provider
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
