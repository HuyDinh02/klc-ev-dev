using KCharge.EntityFrameworkCore;
using KCharge.Enums;
using KCharge.Users;
using Microsoft.EntityFrameworkCore;

namespace KCharge.Driver.Services;

public interface IProfileBffService
{
    Task<ProfileDto?> GetProfileAsync(Guid userId);
    Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<UserStatsDto> GetUserStatsAsync(Guid userId);
}

public class ProfileBffService : IProfileBffService
{
    private readonly KChargeDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<ProfileBffService> _logger;

    public ProfileBffService(
        KChargeDbContext dbContext,
        ICacheService cache,
        ILogger<ProfileBffService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProfileDto?> GetProfileAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:profile";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var user = await _dbContext.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

            if (user == null) return null;

            return new ProfileDto
            {
                UserId = userId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                PreferredLanguage = user.PreferredLanguage,
                WalletBalance = user.WalletBalance,
                IsEmailVerified = user.IsEmailVerified,
                IsPhoneVerified = user.IsPhoneVerified
            };
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

        if (user == null)
        {
            // Create new app user if not exists
            user = new AppUser(Guid.NewGuid(), userId, request.FullName ?? "User");
            await _dbContext.AppUsers.AddAsync(user);
        }

        if (!string.IsNullOrEmpty(request.FullName))
        {
            user.UpdateProfile(request.FullName, user.AvatarUrl);
        }

        if (!string.IsNullOrEmpty(request.AvatarUrl))
        {
            user.UpdateProfile(user.FullName, request.AvatarUrl);
        }

        if (!string.IsNullOrEmpty(request.PreferredLanguage))
        {
            user.SetPreferredLanguage(request.PreferredLanguage);
        }

        await _dbContext.SaveChangesAsync();
        await _cache.RemoveAsync($"user:{userId}:profile");

        return new ProfileDto
        {
            UserId = userId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            PreferredLanguage = user.PreferredLanguage,
            WalletBalance = user.WalletBalance,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified
        };
    }

    public async Task<UserStatsDto> GetUserStatsAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:stats";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var sessions = await _dbContext.ChargingSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.Status == SessionStatus.Completed)
                .ToListAsync();

            var totalSessions = sessions.Count;
            var totalEnergy = sessions.Sum(s => s.TotalEnergyKwh);
            var totalSpent = sessions.Sum(s => s.TotalCost);
            var totalDuration = sessions
                .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                .Sum(s => (s.EndTime!.Value - s.StartTime!.Value).TotalMinutes);

            // CO2 savings: ~0.5kg CO2 per kWh compared to gasoline
            var co2Saved = totalEnergy * 0.5m;

            return new UserStatsDto
            {
                TotalSessions = totalSessions,
                TotalEnergyKwh = Math.Round(totalEnergy, 2),
                TotalSpentVnd = totalSpent,
                TotalChargingMinutes = (int)totalDuration,
                Co2SavedKg = Math.Round(co2Saved, 2)
            };
        }, TimeSpan.FromMinutes(10));
    }
}

// DTOs
public record ProfileDto
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? AvatarUrl { get; init; }
    public string? PreferredLanguage { get; init; }
    public decimal WalletBalance { get; init; }
    public bool IsEmailVerified { get; init; }
    public bool IsPhoneVerified { get; init; }
}

public record UpdateProfileRequest
{
    public string? FullName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? PreferredLanguage { get; init; }
}

public record UserStatsDto
{
    public int TotalSessions { get; init; }
    public decimal TotalEnergyKwh { get; init; }
    public decimal TotalSpentVnd { get; init; }
    public int TotalChargingMinutes { get; init; }
    public decimal Co2SavedKg { get; init; }
}
