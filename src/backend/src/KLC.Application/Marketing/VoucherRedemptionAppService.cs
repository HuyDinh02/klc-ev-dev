using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Payments;
using KLC.Users;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace KLC.Marketing;

/// <summary>
/// Application service encapsulating voucher redemption business logic:
/// validation and wallet crediting. Shared between Admin API and Driver BFF.
/// </summary>
public class VoucherRedemptionAppService : IVoucherRedemptionAppService
{
    private readonly IRepository<Voucher, Guid> _voucherRepository;
    private readonly IRepository<UserVoucher, Guid> _userVoucherRepository;
    private readonly IRepository<WalletTransaction, Guid> _walletTransactionRepository;
    private readonly IRepository<AppUser, Guid> _appUserRepository;
    private readonly WalletDomainService _walletDomainService;
    private readonly ILogger<VoucherRedemptionAppService> _logger;

    public VoucherRedemptionAppService(
        IRepository<Voucher, Guid> voucherRepository,
        IRepository<UserVoucher, Guid> userVoucherRepository,
        IRepository<WalletTransaction, Guid> walletTransactionRepository,
        IRepository<AppUser, Guid> appUserRepository,
        WalletDomainService walletDomainService,
        ILogger<VoucherRedemptionAppService> logger)
    {
        _voucherRepository = voucherRepository;
        _userVoucherRepository = userVoucherRepository;
        _walletTransactionRepository = walletTransactionRepository;
        _appUserRepository = appUserRepository;
        _walletDomainService = walletDomainService;
        _logger = logger;
    }

    public async Task<VoucherValidationResultDto> ValidateVoucherAsync(string code)
    {
        var voucher = await _voucherRepository.FirstOrDefaultAsync(v => v.Code == code && !v.IsDeleted);

        if (voucher == null)
        {
            return new VoucherValidationResultDto
            {
                IsValid = false,
                Error = "Voucher not found"
            };
        }

        if (!voucher.IsActive)
        {
            return new VoucherValidationResultDto
            {
                IsValid = false,
                Error = "Voucher is no longer active"
            };
        }

        if (voucher.ExpiryDate <= DateTime.UtcNow)
        {
            return new VoucherValidationResultDto
            {
                IsValid = false,
                Error = "Voucher has expired"
            };
        }

        if (voucher.UsedQuantity >= voucher.TotalQuantity)
        {
            return new VoucherValidationResultDto
            {
                IsValid = false,
                Error = "Voucher has been fully redeemed"
            };
        }

        return new VoucherValidationResultDto
        {
            IsValid = true,
            Voucher = MapToVoucherInfoDto(voucher)
        };
    }

    public async Task<ApplyVoucherResultDto> ApplyVoucherAsync(Guid userId, string code)
    {
        // 1. Find voucher by code
        var voucher = await _voucherRepository.FirstOrDefaultAsync(v => v.Code == code && !v.IsDeleted);

        if (voucher == null)
        {
            return new ApplyVoucherResultDto { Success = false, UserId = userId, Error = "Voucher not found" };
        }

        if (!voucher.IsValid())
        {
            return new ApplyVoucherResultDto { Success = false, UserId = userId, Error = "Voucher is not valid or has expired" };
        }

        // 2. Check user hasn't already used this voucher
        var alreadyUsed = await _userVoucherRepository.AnyAsync(
            uv => uv.UserId == userId && uv.VoucherId == voucher.Id && uv.IsUsed);

        if (alreadyUsed)
        {
            return new ApplyVoucherResultDto { Success = false, UserId = userId, Error = "You have already used this voucher" };
        }

        // 3. Calculate credit amount
        decimal creditAmount = voucher.Type switch
        {
            VoucherType.FixedAmount => voucher.Value,
            VoucherType.Percentage => voucher.MaxDiscountAmount ?? voucher.Value,
            VoucherType.FreeCharging => voucher.Value,
            _ => voucher.Value
        };

        if (creditAmount <= 0)
        {
            return new ApplyVoucherResultDto { Success = false, UserId = userId, Error = "Invalid voucher credit amount" };
        }

        // 4. Get user
        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == userId);

        if (user == null)
        {
            return new ApplyVoucherResultDto { Success = false, UserId = userId, Error = "User not found" };
        }

        try
        {
            // 5. Apply voucher credit via domain service
            var (newBalance, transaction) = _walletDomainService.ApplyVoucher(
                user, creditAmount, $"Voucher {code}");

            // 6. Create UserVoucher record
            var userVoucher = new UserVoucher(Guid.NewGuid(), userId, voucher.Id);
            userVoucher.MarkUsed();

            // 7. Increment voucher usage
            voucher.IncrementUsage();

            // 8. Save atomically
            await _walletTransactionRepository.InsertAsync(transaction);
            await _userVoucherRepository.InsertAsync(userVoucher);
            await _voucherRepository.UpdateAsync(voucher);
            await _appUserRepository.UpdateAsync(user);

            _logger.LogInformation(
                "Voucher applied: UserId={UserId}, Code={Code}, CreditAmount={CreditAmount}, NewBalance={NewBalance}",
                userId, code, creditAmount, newBalance);

            return new ApplyVoucherResultDto
            {
                Success = true,
                NewBalance = newBalance,
                CreditAmount = creditAmount,
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply voucher {Code} for user {UserId}", code, userId);
            return new ApplyVoucherResultDto { Success = false, UserId = userId, Error = "Failed to apply voucher" };
        }
    }

    private static VoucherInfoDto MapToVoucherInfoDto(Voucher voucher)
    {
        return new VoucherInfoDto
        {
            Id = voucher.Id,
            Code = voucher.Code,
            Type = voucher.Type,
            Value = voucher.Value,
            MinOrderAmount = voucher.MinOrderAmount,
            MaxDiscountAmount = voucher.MaxDiscountAmount,
            ExpiryDate = voucher.ExpiryDate,
            Description = voucher.Description,
            RemainingQuantity = voucher.TotalQuantity - voucher.UsedQuantity
        };
    }
}
