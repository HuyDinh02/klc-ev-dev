using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Volo.Abp;

namespace KLC.Users;

/// <summary>
/// Application service encapsulating driver profile mutation logic:
/// phone change (OTP flow) and account deletion.
/// Shared between Admin API and Driver BFF.
/// </summary>
public class ProfileAppService : IProfileAppService
{
    private readonly KLCDbContext _dbContext;
    private readonly IDatabase _redis;
    private readonly ILogger<ProfileAppService> _logger;

    public ProfileAppService(
        KLCDbContext dbContext,
        IConnectionMultiplexer redis,
        ILogger<ProfileAppService> logger)
    {
        _dbContext = dbContext;
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    public async Task RequestPhoneChangeAsync(Guid userId, string newPhoneNumber)
    {
        // Check if phone already in use
        var existing = await _dbContext.AppUsers
            .AsNoTracking()
            .AnyAsync(u => u.PhoneNumber == newPhoneNumber && u.IdentityUserId != userId && !u.IsDeleted);

        if (existing)
            throw new BusinessException(KLCDomainErrorCodes.Profile.PhoneAlreadyUsed);

        // Generate and store OTP for phone change
        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        await _redis.StringSetAsync($"otp:phone-change:{userId}:{newPhoneNumber}", otp, TimeSpan.FromMinutes(5));
        _logger.LogInformation("Phone change OTP for user {UserId}: {Otp}", userId, otp);
    }

    public async Task ConfirmPhoneChangeAsync(Guid userId, string newPhoneNumber, string otp)
    {
        var storedOtp = await _redis.StringGetAsync($"otp:phone-change:{userId}:{newPhoneNumber}");
        if (storedOtp.IsNullOrEmpty || storedOtp.ToString() != otp)
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidOtp);

        var user = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.IdentityUserId == userId && !u.IsDeleted);

        if (user == null)
            throw new BusinessException(KLCDomainErrorCodes.Auth.InvalidCredentials);

        user.SetPhoneNumber(newPhoneNumber, isVerified: true);
        await _dbContext.SaveChangesAsync();
        await _redis.KeyDeleteAsync($"otp:phone-change:{userId}:{newPhoneNumber}");
    }

    public async Task DeleteAccountAsync(Guid userId)
    {
        // Check for active sessions
        var hasActive = await _dbContext.ChargingSessions
            .AnyAsync(s => s.UserId == userId && (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Starting));

        if (hasActive)
            throw new BusinessException(KLCDomainErrorCodes.Profile.HasActiveSession);

        var user = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.IdentityUserId == userId && !u.IsDeleted);

        if (user == null) return;

        user.Deactivate();
        // ABP soft delete will handle IsDeleted flag
        await _dbContext.SaveChangesAsync();
    }
}
