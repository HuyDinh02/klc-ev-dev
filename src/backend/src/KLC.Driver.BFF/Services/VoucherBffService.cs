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
}

public class VoucherBffService : IVoucherBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<VoucherBffService> _logger;
    private readonly IVoucherRedemptionAppService _voucherRedemptionAppService;
    private readonly IDriverHubNotifier _driverNotifier;

    public VoucherBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<VoucherBffService> logger,
        IVoucherRedemptionAppService voucherRedemptionAppService,
        IDriverHubNotifier driverNotifier)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _voucherRedemptionAppService = voucherRedemptionAppService;
        _driverNotifier = driverNotifier;
    }

    public async Task<List<VoucherDto>> GetAvailableVouchersAsync(Guid userId)
    {
        var cacheKey = CacheKeys.UserAvailableVouchers(userId);

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
        // Delegate to Application layer
        var result = await _voucherRedemptionAppService.ValidateVoucherAsync(code);

        // Map Application DTO to BFF DTO (preserves mobile API contract)
        return new VoucherValidationResultDto
        {
            IsValid = result.IsValid,
            Voucher = result.Voucher != null ? new VoucherDto
            {
                Id = result.Voucher.Id,
                Code = result.Voucher.Code,
                Type = result.Voucher.Type,
                Value = result.Voucher.Value,
                MinOrderAmount = result.Voucher.MinOrderAmount,
                MaxDiscountAmount = result.Voucher.MaxDiscountAmount,
                ExpiryDate = result.Voucher.ExpiryDate,
                Description = result.Voucher.Description,
                RemainingQuantity = result.Voucher.RemainingQuantity
            } : null,
            Error = result.Error
        };
    }

    public async Task<ApplyVoucherResponse> ApplyVoucherAsync(Guid userId, string code)
    {
        // Delegate business logic to Application layer
        var result = await _voucherRedemptionAppService.ApplyVoucherAsync(userId, code);

        // BFF handles cache invalidation and SignalR notifications
        if (result.Success && result.NewBalance.HasValue)
        {
            await _cache.RemoveAsync(CacheKeys.UserWalletBalance(userId));
            await _cache.RemoveAsync(CacheKeys.UserAvailableVouchers(userId));
            await _cache.RemoveAsync(CacheKeys.UserWalletSummary(userId));

            await _driverNotifier.NotifyWalletBalanceChangedAsync(userId,
                new WalletBalanceChangedMessage
                {
                    UserId = userId,
                    NewBalance = result.NewBalance.Value,
                    ChangeAmount = result.CreditAmount ?? 0,
                    Reason = $"Voucher {code} applied",
                    Timestamp = DateTime.UtcNow
                });
        }

        return new ApplyVoucherResponse(result.Success, result.NewBalance, result.CreditAmount, result.Error);
    }
}

// BFF DTOs (preserved for API contract compatibility)
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

public record ApplyVoucherRequest(string Code);

public record ApplyVoucherResponse(bool Success, decimal? NewBalance, decimal? CreditAmount, string? Error);
