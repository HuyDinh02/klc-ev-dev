using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Payments;
using KLC.Permissions;
using KLC.Sessions;
using KLC.Users;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace KLC.MobileUsers;

[Authorize(KLCPermissions.MobileUsers.Default)]
public class MobileUserAppService : KLCAppService, IMobileUserAppService
{
    private readonly IRepository<AppUser, Guid> _userRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<WalletTransaction, Guid> _transactionRepository;
    private readonly WalletDomainService _walletDomainService;

    public MobileUserAppService(
        IRepository<AppUser, Guid> userRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<WalletTransaction, Guid> transactionRepository,
        WalletDomainService walletDomainService)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _transactionRepository = transactionRepository;
        _walletDomainService = walletDomainService;
    }

    [Authorize(KLCPermissions.MobileUsers.ViewAll)]
    public async Task<CursorPagedResultDto<MobileUserListDto>> GetListAsync(GetMobileUserListDto input)
    {
        var pageSize = input.PageSize;
        if (pageSize <= 0 || pageSize > 50) pageSize = 20;

        var query = await _userRepository.GetQueryableAsync();
        query = query.Where(u => !u.IsDeleted);

        if (!string.IsNullOrEmpty(input.Search))
        {
            query = query.Where(u => u.FullName.Contains(input.Search) || u.PhoneNumber!.Contains(input.Search));
        }

        if (input.Status == "active") query = query.Where(u => u.IsActive);
        if (input.Status == "suspended") query = query.Where(u => !u.IsActive);

        if (input.Cursor.HasValue)
        {
            var cursorUser = await _userRepository.FirstOrDefaultAsync(u => u.Id == input.Cursor.Value);
            if (cursorUser != null)
                query = query.Where(u => u.CreationTime < cursorUser.CreationTime);
        }

        query = query.OrderByDescending(u => u.CreationTime);

        var users = await AsyncExecuter.ToListAsync(query.Take(pageSize + 1));
        var hasMore = users.Count > pageSize;
        var items = hasMore ? users.Take(pageSize).ToList() : users;

        var dtos = items.Select(u => new MobileUserListDto
        {
            Id = u.Id,
            IdentityUserId = u.IdentityUserId,
            FullName = u.FullName,
            PhoneNumber = u.PhoneNumber,
            Email = u.Email,
            IsActive = u.IsActive,
            WalletBalance = u.WalletBalance,
            MembershipTier = u.MembershipTier,
            IsPhoneVerified = u.IsPhoneVerified,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreationTime
        }).ToList();

        return new CursorPagedResultDto<MobileUserListDto>
        {
            Data = dtos,
            Pagination = new CursorPaginationDto
            {
                NextCursor = hasMore && items.Any() ? items.Last().Id : null,
                HasMore = hasMore,
                PageSize = pageSize
            }
        };
    }

    public async Task<MobileUserDetailDto> GetAsync(Guid id)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        var sessionCount = await AsyncExecuter.CountAsync(
            (await _sessionRepository.GetQueryableAsync()).Where(s => s.UserId == user.IdentityUserId));

        var transactionQuery = await _transactionRepository.GetQueryableAsync();
        var totalSpent = await AsyncExecuter.SumAsync(
            transactionQuery
                .Where(t => t.UserId == user.IdentityUserId
                    && t.Type == WalletTransactionType.SessionPayment
                    && t.Status == TransactionStatus.Completed)
                .Select(t => Math.Abs(t.Amount)));

        return new MobileUserDetailDto
        {
            Id = user.Id,
            IdentityUserId = user.IdentityUserId,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            WalletBalance = user.WalletBalance,
            MembershipTier = user.MembershipTier,
            IsPhoneVerified = user.IsPhoneVerified,
            IsEmailVerified = user.IsEmailVerified,
            DateOfBirth = user.DateOfBirth,
            Gender = user.Gender,
            LastLoginAt = user.LastLoginAt,
            LastTopUpAt = user.LastTopUpAt,
            CreatedAt = user.CreationTime,
            SessionCount = sessionCount,
            TotalSpent = totalSpent
        };
    }

    [Authorize(KLCPermissions.MobileUsers.Suspend)]
    public async Task SuspendAsync(Guid id)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        user.Deactivate();
        await _userRepository.UpdateAsync(user);
    }

    [Authorize(KLCPermissions.MobileUsers.Suspend)]
    public async Task UnsuspendAsync(Guid id)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        user.Reactivate();
        await _userRepository.UpdateAsync(user);
    }

    public async Task<CursorPagedResultDto<MobileUserSessionDto>> GetSessionsAsync(Guid id, GetMobileUserSessionsDto input)
    {
        var pageSize = input.PageSize;
        if (pageSize <= 0 || pageSize > 50) pageSize = 20;

        var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        var query = await _sessionRepository.GetQueryableAsync();
        query = query
            .Where(s => s.UserId == user.IdentityUserId)
            .OrderByDescending(s => s.CreationTime);

        var sessions = await AsyncExecuter.ToListAsync(query.Take(pageSize + 1));
        var hasMore = sessions.Count > pageSize;
        var items = hasMore ? sessions.Take(pageSize).ToList() : sessions;

        var dtos = items.Select(s => new MobileUserSessionDto
        {
            Id = s.Id,
            StationId = s.StationId,
            Status = s.Status,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            TotalEnergyKwh = s.TotalEnergyKwh,
            TotalCost = s.TotalCost,
            CreatedAt = s.CreationTime
        }).ToList();

        return new CursorPagedResultDto<MobileUserSessionDto>
        {
            Data = dtos,
            Pagination = new CursorPaginationDto
            {
                HasMore = hasMore,
                PageSize = pageSize
            }
        };
    }

    public async Task<CursorPagedResultDto<MobileUserTransactionDto>> GetTransactionsAsync(Guid id, GetMobileUserTransactionsDto input)
    {
        var pageSize = input.PageSize;
        if (pageSize <= 0 || pageSize > 50) pageSize = 20;

        var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        var query = await _transactionRepository.GetQueryableAsync();
        query = query
            .Where(t => t.UserId == user.IdentityUserId)
            .OrderByDescending(t => t.CreationTime);

        var transactions = await AsyncExecuter.ToListAsync(query.Take(pageSize + 1));
        var hasMore = transactions.Count > pageSize;
        var items = hasMore ? transactions.Take(pageSize).ToList() : transactions;

        var dtos = items.Select(t => new MobileUserTransactionDto
        {
            Id = t.Id,
            Type = t.Type,
            Amount = t.Amount,
            BalanceAfter = t.BalanceAfter,
            Status = t.Status,
            Description = t.Description,
            ReferenceCode = t.ReferenceCode,
            CreatedAt = t.CreationTime
        }).ToList();

        return new CursorPagedResultDto<MobileUserTransactionDto>
        {
            Data = dtos,
            Pagination = new CursorPaginationDto
            {
                HasMore = hasMore,
                PageSize = pageSize
            }
        };
    }

    [Authorize(KLCPermissions.MobileUsers.WalletAdjust)]
    public async Task<WalletAdjustResultDto> AdjustWalletAsync(Guid id, WalletAdjustDto input)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null)
            throw new BusinessException(KLCDomainErrorCodes.EntityNotFound);

        var (newBalance, transaction) = _walletDomainService.Adjust(user, input.Amount, input.Reason);
        await _transactionRepository.InsertAsync(transaction);
        await _userRepository.UpdateAsync(user);

        return new WalletAdjustResultDto
        {
            NewBalance = newBalance,
            TransactionId = transaction.Id
        };
    }

    public async Task<MobileUserStatisticsDto> GetStatisticsAsync()
    {
        var query = await _userRepository.GetQueryableAsync();
        var total = await AsyncExecuter.CountAsync(query.Where(u => !u.IsDeleted));
        var active = await AsyncExecuter.CountAsync(query.Where(u => u.IsActive && !u.IsDeleted));

        return new MobileUserStatisticsDto
        {
            Total = total,
            Active = active,
            Suspended = total - active
        };
    }
}
