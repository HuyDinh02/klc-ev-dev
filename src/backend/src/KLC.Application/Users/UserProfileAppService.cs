using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Payments;
using KLC.Sessions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace KLC.Users;

[Authorize]
public class UserProfileAppService : KLCAppService, IUserProfileAppService
{
    private readonly IRepository<AppUser, Guid> _appUserRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IdentityUserManager _userManager;

    public UserProfileAppService(
        IRepository<AppUser, Guid> appUserRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IdentityUserManager userManager)
    {
        _appUserRepository = appUserRepository;
        _sessionRepository = sessionRepository;
        _paymentRepository = paymentRepository;
        _userManager = userManager;
    }

    public async Task<UserProfileDto> GetProfileAsync()
    {
        var userId = CurrentUser.GetId();
        var appUser = await GetOrCreateAppUserAsync(userId);
        var stats = await GetUserStatsInternalAsync(userId);

        return new UserProfileDto
        {
            Id = appUser.Id,
            IdentityUserId = appUser.IdentityUserId,
            FullName = appUser.FullName,
            PhoneNumber = appUser.PhoneNumber,
            Email = appUser.Email,
            IsPhoneVerified = appUser.IsPhoneVerified,
            IsEmailVerified = appUser.IsEmailVerified,
            AvatarUrl = appUser.AvatarUrl,
            PreferredLanguage = appUser.PreferredLanguage,
            IsNotificationsEnabled = appUser.IsNotificationsEnabled,
            WalletBalance = appUser.WalletBalance,
            LastLoginAt = appUser.LastLoginAt,
            TotalSessions = stats.TotalSessions,
            TotalEnergyKwh = stats.TotalEnergyKwh,
            TotalSpent = stats.TotalSpent
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(UpdateProfileDto input)
    {
        var userId = CurrentUser.GetId();
        var appUser = await GetOrCreateAppUserAsync(userId);

        appUser.UpdateProfile(input.FullName, input.AvatarUrl);

        if (!string.IsNullOrEmpty(input.PreferredLanguage))
        {
            appUser.SetPreferredLanguage(input.PreferredLanguage);
        }

        if (input.IsNotificationsEnabled.HasValue)
        {
            if (input.IsNotificationsEnabled.Value)
                appUser.EnableNotifications();
            else
                appUser.DisableNotifications();
        }

        await _appUserRepository.UpdateAsync(appUser);

        return await GetProfileAsync();
    }

    public async Task UpdatePhoneAsync(UpdatePhoneDto input)
    {
        var userId = CurrentUser.GetId();

        // Check if phone is already used
        var existing = await _appUserRepository.FirstOrDefaultAsync(
            u => u.PhoneNumber == input.PhoneNumber && u.IdentityUserId != userId);
        if (existing != null)
        {
            throw new BusinessException("MOD_011_002");
        }

        var appUser = await GetOrCreateAppUserAsync(userId);
        appUser.SetPhoneNumber(input.PhoneNumber, false);
        await _appUserRepository.UpdateAsync(appUser);

        // TODO: Send verification SMS
    }

    public async Task VerifyPhoneAsync(VerifyPhoneDto input)
    {
        var userId = CurrentUser.GetId();
        var appUser = await GetOrCreateAppUserAsync(userId);

        // TODO: Validate verification code
        // For now, just mark as verified
        appUser.VerifyPhone();
        await _appUserRepository.UpdateAsync(appUser);
    }

    public async Task UpdateEmailAsync(UpdateEmailDto input)
    {
        var userId = CurrentUser.GetId();

        // Check if email is already used
        var existing = await _appUserRepository.FirstOrDefaultAsync(
            u => u.Email == input.Email && u.IdentityUserId != userId);
        if (existing != null)
        {
            throw new BusinessException("MOD_011_001");
        }

        var appUser = await GetOrCreateAppUserAsync(userId);
        appUser.SetEmail(input.Email, false);
        await _appUserRepository.UpdateAsync(appUser);

        // TODO: Send verification email
    }

    public async Task VerifyEmailAsync(VerifyEmailDto input)
    {
        var userId = CurrentUser.GetId();
        var appUser = await GetOrCreateAppUserAsync(userId);

        // TODO: Validate verification token
        // For now, just mark as verified
        appUser.VerifyEmail();
        await _appUserRepository.UpdateAsync(appUser);
    }

    public async Task ChangePasswordAsync(ChangePasswordDto input)
    {
        var userId = CurrentUser.GetId();
        var identityUser = await _userManager.GetByIdAsync(userId);

        var result = await _userManager.ChangePasswordAsync(
            identityUser,
            input.CurrentPassword,
            input.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessException("MOD_011_004")
                .WithData("errors", errors);
        }
    }

    public async Task<UserStatisticsDto> GetStatisticsAsync()
    {
        var userId = CurrentUser.GetId();
        return await GetUserStatsInternalAsync(userId);
    }

    public async Task DeactivateAccountAsync()
    {
        var userId = CurrentUser.GetId();

        // Check for active sessions
        var activeSession = await _sessionRepository.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Status == SessionStatus.InProgress);
        if (activeSession != null)
        {
            throw new BusinessException("MOD_011_005")
                .WithData("reason", "Active charging session exists");
        }

        var appUser = await GetOrCreateAppUserAsync(userId);
        appUser.Deactivate();
        await _appUserRepository.UpdateAsync(appUser);
    }

    private async Task<AppUser> GetOrCreateAppUserAsync(Guid identityUserId)
    {
        var appUser = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
        if (appUser == null)
        {
            // Create AppUser record if it doesn't exist
            var identityUser = await _userManager.GetByIdAsync(identityUserId);
            appUser = new AppUser(
                GuidGenerator.Create(),
                identityUserId,
                identityUser.Name ?? identityUser.UserName ?? "User",
                identityUser.PhoneNumber,
                identityUser.Email
            );
            await _appUserRepository.InsertAsync(appUser);
        }
        return appUser;
    }

    private async Task<UserStatisticsDto> GetUserStatsInternalAsync(Guid userId)
    {
        var sessionQuery = await _sessionRepository.GetQueryableAsync();
        var userSessions = await AsyncExecuter.ToListAsync(
            sessionQuery.Where(s => s.UserId == userId && s.Status == SessionStatus.Completed));

        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthSessions = userSessions.Where(s => s.CreationTime >= thisMonth).ToList();

        var totalDuration = userSessions
            .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
            .Sum(s => (s.EndTime!.Value - s.StartTime!.Value).TotalMinutes);

        return new UserStatisticsDto
        {
            TotalSessions = userSessions.Count,
            TotalEnergyKwh = userSessions.Sum(s => s.TotalEnergyKwh),
            TotalSpent = userSessions.Sum(s => s.TotalCost),
            AverageSessionDurationMinutes = userSessions.Count > 0 ? (decimal)(totalDuration / userSessions.Count) : 0,
            AverageEnergyPerSession = userSessions.Count > 0 ? userSessions.Sum(s => s.TotalEnergyKwh) / userSessions.Count : 0,
            SessionsThisMonth = monthSessions.Count,
            EnergyThisMonth = monthSessions.Sum(s => s.TotalEnergyKwh)
        };
    }
}
