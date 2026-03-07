using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Marketing;
using KLC.Payments;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Services;

public interface IVoucherBffService
{
    Task<List<VoucherDto>> GetAvailableVouchersAsync(Guid userId);
    Task<VoucherValidationResultDto> ValidateVoucherAsync(string code);
    Task<ApplyVoucherResponse> ApplyVoucherAsync(Guid userId, string code);
    Task<List<PromotionDto>> GetActivePromotionsAsync();
    Task<PromotionDetailDto?> GetPromotionDetailAsync(Guid id);
}

public class VoucherBffService : IVoucherBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<VoucherBffService> _logger;
    private readonly WalletDomainService _walletDomainService;
    private readonly IDriverHubNotifier _driverNotifier;

    public VoucherBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<VoucherBffService> logger,
        WalletDomainService walletDomainService,
        IDriverHubNotifier driverNotifier)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _walletDomainService = walletDomainService;
        _driverNotifier = driverNotifier;
    }

    public async Task<List<VoucherDto>> GetAvailableVouchersAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:available-vouchers";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var now = DateTime.UtcNow;

            // Get voucher IDs already used by this user
            var usedVoucherIds = await _dbContext.UserVouchers
                .AsNoTracking()
                .Where(uv => uv.UserId == userId && uv.IsUsed)
                .Select(uv => uv.VoucherId)
                .ToListAsync();

            // Get all active, valid vouchers the user hasn't used yet
            var vouchers = await _dbContext.Vouchers
                .AsNoTracking()
                .Where(v => v.IsActive
                            && !v.IsDeleted
                            && v.ExpiryDate > now
                            && v.UsedQuantity < v.TotalQuantity
                            && !usedVoucherIds.Contains(v.Id))
                .OrderBy(v => v.ExpiryDate)
                .Select(v => new VoucherDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    Type = v.Type,
                    Value = v.Value,
                    MinOrderAmount = v.MinOrderAmount,
                    MaxDiscountAmount = v.MaxDiscountAmount,
                    ExpiryDate = v.ExpiryDate,
                    Description = v.Description,
                    RemainingQuantity = v.TotalQuantity - v.UsedQuantity
                })
                .ToListAsync();

            return vouchers;
        }, TimeSpan.FromMinutes(2));
    }

    public async Task<VoucherValidationResultDto> ValidateVoucherAsync(string code)
    {
        var voucher = await _dbContext.Vouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Code == code && !v.IsDeleted);

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
            Voucher = new VoucherDto
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
            }
        };
    }

    public async Task<ApplyVoucherResponse> ApplyVoucherAsync(Guid userId, string code)
    {
        // 1. Find voucher by code
        var voucher = await _dbContext.Vouchers
            .FirstOrDefaultAsync(v => v.Code == code && !v.IsDeleted);

        if (voucher == null)
        {
            return new ApplyVoucherResponse(false, null, null, "Voucher not found");
        }

        if (!voucher.IsValid())
        {
            return new ApplyVoucherResponse(false, null, null, "Voucher is not valid or has expired");
        }

        // 2. Check user hasn't already used this voucher
        var alreadyUsed = await _dbContext.UserVouchers
            .AnyAsync(uv => uv.UserId == userId && uv.VoucherId == voucher.Id && uv.IsUsed);

        if (alreadyUsed)
        {
            return new ApplyVoucherResponse(false, null, null, "You have already used this voucher");
        }

        // 3. Calculate credit amount
        // For standalone wallet credit (no order context):
        // - FixedAmount: credit the fixed value
        // - Percentage: credit the MaxDiscountAmount cap (the maximum possible discount)
        // - FreeCharging: credit the voucher value as session credit
        decimal creditAmount = voucher.Type switch
        {
            VoucherType.FixedAmount => voucher.Value,
            VoucherType.Percentage => voucher.MaxDiscountAmount ?? voucher.Value,
            VoucherType.FreeCharging => voucher.Value,
            _ => voucher.Value
        };

        if (creditAmount <= 0)
        {
            return new ApplyVoucherResponse(false, null, null, "Invalid voucher credit amount");
        }

        // 4. Get user
        var user = await _dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

        if (user == null)
        {
            return new ApplyVoucherResponse(false, null, null, "User not found");
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
            await _dbContext.WalletTransactions.AddAsync(transaction);
            await _dbContext.UserVouchers.AddAsync(userVoucher);
            await _dbContext.SaveChangesAsync();

            // Invalidate caches
            await _cache.RemoveAsync($"user:{userId}:wallet-balance");
            await _cache.RemoveAsync($"user:{userId}:available-vouchers");
            await _cache.RemoveAsync($"user:{userId}:wallet-summary");

            _logger.LogInformation(
                "Voucher applied: UserId={UserId}, Code={Code}, CreditAmount={CreditAmount}, NewBalance={NewBalance}",
                userId, code, creditAmount, newBalance);

            // 9. Notify via SignalR
            await _driverNotifier.NotifyWalletBalanceChangedAsync(userId,
                new WalletBalanceChangedMessage
                {
                    UserId = userId,
                    NewBalance = newBalance,
                    ChangeAmount = creditAmount,
                    Reason = $"Voucher {code} applied",
                    Timestamp = DateTime.UtcNow
                });

            return new ApplyVoucherResponse(true, newBalance, creditAmount, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply voucher {Code} for user {UserId}", code, userId);
            return new ApplyVoucherResponse(false, null, null, "Failed to apply voucher");
        }
    }

    public async Task<List<PromotionDto>> GetActivePromotionsAsync()
    {
        var cacheKey = "promotions:active";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var now = DateTime.UtcNow;

            var promotions = await _dbContext.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive
                            && !p.IsDeleted
                            && p.StartDate <= now
                            && p.EndDate >= now)
                .OrderByDescending(p => p.CreationTime)
                .Select(p => new PromotionDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Type = p.Type
                })
                .ToListAsync();

            return promotions;
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<PromotionDetailDto?> GetPromotionDetailAsync(Guid id)
    {
        var cacheKey = $"promotion:{id}:detail";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var promotion = await _dbContext.Promotions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (promotion == null) return null;

            return new PromotionDetailDto
            {
                Id = promotion.Id,
                Title = promotion.Title,
                Description = promotion.Description,
                ImageUrl = promotion.ImageUrl,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                Type = promotion.Type,
                IsActive = promotion.IsCurrentlyActive()
            };
        }, TimeSpan.FromMinutes(5));
    }
}

// DTOs
public record VoucherDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public VoucherType Type { get; init; }
    public decimal Value { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public DateTime ExpiryDate { get; init; }
    public string? Description { get; init; }
    public int RemainingQuantity { get; init; }
}

public record VoucherValidationResultDto
{
    public bool IsValid { get; init; }
    public VoucherDto? Voucher { get; init; }
    public string? Error { get; init; }
}

public record PromotionDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public PromotionType Type { get; init; }
}

public record PromotionDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public PromotionType Type { get; init; }
    public bool IsActive { get; init; }
}

public record ApplyVoucherRequest(string Code);

public record ApplyVoucherResponse(bool Success, decimal? NewBalance, decimal? CreditAmount, string? Error);
