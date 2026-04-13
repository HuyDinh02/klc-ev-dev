using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KLC.Marketing;

/// <summary>
/// Application service encapsulating promotion voucher claim logic:
/// validates promotion, selects an available voucher, creates UserVoucher record.
/// Shared between Admin API and Driver BFF.
/// </summary>
public class PromotionClaimAppService : KLCAppService, IPromotionClaimAppService
{
    private readonly KLCDbContext _dbContext;
    private readonly ILogger<PromotionClaimAppService> _logger;

    public PromotionClaimAppService(
        KLCDbContext dbContext,
        ILogger<PromotionClaimAppService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PromotionClaimResultDto> ClaimVoucherFromPromotionAsync(Guid userId, Guid promotionId)
    {
        // 1. Find the promotion (must be active)
        var promotion = await _dbContext.Promotions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == promotionId && !p.IsDeleted);

        if (promotion == null || !promotion.IsCurrentlyActive())
        {
            return new PromotionClaimResultDto
            {
                Success = false,
                Error = "Promotion not found or not active"
            };
        }

        // 2. Get voucher IDs already claimed by this user for this promotion
        var claimedVoucherIds = await _dbContext.UserVouchers
            .AsNoTracking()
            .Where(uv => uv.UserId == userId)
            .Select(uv => uv.VoucherId)
            .ToListAsync();

        // 3. Find an available voucher linked to this promotion (not claimed by user, has stock)
        var now = DateTime.UtcNow;
        var voucher = await _dbContext.Vouchers
            .FirstOrDefaultAsync(v =>
                v.PromotionId == promotionId
                && v.IsActive
                && !v.IsDeleted
                && v.ExpiryDate > now
                && v.UsedQuantity < v.TotalQuantity
                && !claimedVoucherIds.Contains(v.Id));

        if (voucher == null)
        {
            return new PromotionClaimResultDto
            {
                Success = false,
                Error = "No available vouchers for this promotion"
            };
        }

        // 4. Create a UserVoucher record (claimed but not used yet)
        var userVoucher = new UserVoucher(Guid.NewGuid(), userId, voucher.Id);
        await _dbContext.UserVouchers.AddAsync(userVoucher);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} claimed voucher {VoucherCode} from promotion {PromotionId}",
            userId, voucher.Code, promotionId);

        return new PromotionClaimResultDto
        {
            Success = true,
            VoucherCode = voucher.Code,
            VoucherId = voucher.Id
        };
    }
}
