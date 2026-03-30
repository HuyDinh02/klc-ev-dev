using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Payments;
using KLC.Permissions;
using KLC.Sessions;
using KLC.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Repositories;

namespace KLC.Controllers;

[ApiController]
[Route("api/v1/admin/mobile-users")]
[Authorize(KLCPermissions.MobileUsers.Default)]
public class AdminMobileUserController : AbpController
{
    private readonly IRepository<AppUser, Guid> _appUserRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<WalletTransaction, Guid> _walletTransactionRepository;

    public AdminMobileUserController(
        IRepository<AppUser, Guid> appUserRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<WalletTransaction, Guid> walletTransactionRepository)
    {
        _appUserRepository = appUserRepository;
        _sessionRepository = sessionRepository;
        _walletTransactionRepository = walletTransactionRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<MobileUserListDto>>> GetAllAsync(
        [FromQuery] string? search,
        [FromQuery] int maxResultCount = 20,
        [FromQuery] int skipCount = 0)
    {
        var query = (await _appUserRepository.GetQueryableAsync())
            .Where(u => !u.IsDeleted);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)) ||
                (u.Email != null && u.Email.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreationTime)
            .Skip(skipCount)
            .Take(Math.Min(maxResultCount, 100))
            .ToListAsync();

        var dtos = users.Select(u => new MobileUserListDto
        {
            Id = u.Id,
            FullName = u.FullName,
            PhoneNumber = u.PhoneNumber,
            Email = u.Email,
            WalletBalance = u.WalletBalance,
            MembershipTier = u.MembershipTier,
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            CreationTime = u.CreationTime
        }).ToList();

        return Ok(new PagedResultDto<MobileUserListDto>(totalCount, dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MobileUserDetailDto>> GetAsync(Guid id)
    {
        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var sessionCount = await (await _sessionRepository.GetQueryableAsync())
            .CountAsync(s => (s.UserId == user.Id || s.UserId == user.IdentityUserId) && !s.IsDeleted);

        var totalSpent = await (await _sessionRepository.GetQueryableAsync())
            .Where(s => (s.UserId == user.Id || s.UserId == user.IdentityUserId) && !s.IsDeleted && s.Status == SessionStatus.Completed)
            .SumAsync(s => (decimal?)s.TotalCost) ?? 0;

        return Ok(new MobileUserDetailDto
        {
            Id = user.Id,
            IdentityUserId = user.IdentityUserId,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            WalletBalance = user.WalletBalance,
            MembershipTier = user.MembershipTier,
            IsActive = user.IsActive,
            IsPhoneVerified = user.IsPhoneVerified,
            LastLoginAt = user.LastLoginAt,
            CreationTime = user.CreationTime,
            TotalSessions = sessionCount,
            TotalSpent = totalSpent
        });
    }

    [HttpGet("{id:guid}/sessions")]
    public async Task<ActionResult<object>> GetSessionsAsync(
        Guid id,
        [FromQuery] int maxResultCount = 20,
        [FromQuery] int skipCount = 0)
    {
        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        // Search by both AppUser.Id and IdentityUserId
        var query = (await _sessionRepository.GetQueryableAsync())
            .Where(s => (s.UserId == user.Id || s.UserId == user.IdentityUserId) && !s.IsDeleted)
            .OrderByDescending(s => s.CreationTime);

        var totalCount = await query.CountAsync();
        var sessions = await query
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync();

        var dtos = sessions.Select(s => new
        {
            s.Id,
            s.StationId,
            s.ConnectorNumber,
            s.Status,
            s.StartTime,
            s.EndTime,
            s.TotalEnergyKwh,
            s.TotalCost,
            s.RatePerKwh,
            s.CreationTime
        });

        return Ok(new { data = dtos, totalCount });
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<ActionResult<object>> GetTransactionsAsync(
        Guid id,
        [FromQuery] int maxResultCount = 20,
        [FromQuery] int skipCount = 0)
    {
        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var query = (await _walletTransactionRepository.GetQueryableAsync())
            .Where(t => t.UserId == user.Id || t.UserId == user.IdentityUserId)
            .OrderByDescending(t => t.CreationTime);

        var totalCount = await query.CountAsync();
        var txns = await query
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync();

        var dtos = txns.Select(t => new
        {
            t.Id,
            t.Type,
            t.Amount,
            t.BalanceAfter,
            t.PaymentGateway,
            t.Status,
            t.Description,
            t.ReferenceCode,
            t.CreationTime
        });

        return Ok(new { data = dtos, totalCount });
    }

    [HttpPost("{id:guid}/suspend")]
    [Authorize(KLCPermissions.MobileUsers.Default)]
    public async Task<ActionResult> SuspendAsync(Guid id)
    {
        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        user.Deactivate();
        await _appUserRepository.UpdateAsync(user);
        return NoContent();
    }

    [HttpPost("{id:guid}/unsuspend")]
    [Authorize(KLCPermissions.MobileUsers.Default)]
    public async Task<ActionResult> UnsuspendAsync(Guid id)
    {
        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        user.Reactivate();
        await _appUserRepository.UpdateAsync(user);
        return NoContent();
    }

    [HttpPost("{id:guid}/wallet/adjust")]
    [Authorize(KLCPermissions.MobileUsers.Default)]
    public async Task<ActionResult<object>> AdjustBalanceAsync(
        Guid id,
        [FromBody] AdjustBalanceRequest request)
    {
        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.AddToWallet(request.Amount);

        var txn = new WalletTransaction(
            Guid.NewGuid(),
            user.Id,
            WalletTransactionType.Adjustment,
            request.Amount,
            user.WalletBalance,
            description: request.Reason ?? "Admin adjustment");
        txn.MarkCompleted();

        await _walletTransactionRepository.InsertAsync(txn);
        await _appUserRepository.UpdateAsync(user);

        return Ok(new { newBalance = user.WalletBalance, transactionId = txn.Id });
    }
}

public class MobileUserListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public decimal WalletBalance { get; set; }
    public MembershipTier MembershipTier { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreationTime { get; set; }
}

public class MobileUserDetailDto : MobileUserListDto
{
    public Guid IdentityUserId { get; set; }
    public bool IsPhoneVerified { get; set; }
    public int TotalSessions { get; set; }
    public decimal TotalSpent { get; set; }
}

public record AdjustBalanceRequest
{
    public decimal Amount { get; init; }
    public string? Reason { get; init; }
}
