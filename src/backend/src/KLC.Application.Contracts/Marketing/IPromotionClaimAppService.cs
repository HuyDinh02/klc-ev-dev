using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Marketing;

/// <summary>
/// Application service for promotion voucher claiming business logic.
/// Validates the promotion, selects an available voucher, and creates the UserVoucher record.
/// </summary>
public interface IPromotionClaimAppService : IApplicationService
{
    /// <summary>
    /// Claim a voucher from an active promotion for the given user.
    /// </summary>
    Task<PromotionClaimResultDto> ClaimVoucherFromPromotionAsync(Guid userId, Guid promotionId);
}

public class PromotionClaimResultDto
{
    public bool Success { get; set; }
    public string? VoucherCode { get; set; }
    public Guid? VoucherId { get; set; }
    public string? Error { get; set; }
}
