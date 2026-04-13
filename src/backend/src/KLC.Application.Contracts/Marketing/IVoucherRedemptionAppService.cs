using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace KLC.Marketing;

/// <summary>
/// Application service for voucher redemption business logic: validation and application.
/// The BFF delegates to this service for all voucher mutations.
/// </summary>
public interface IVoucherRedemptionAppService : IApplicationService
{
    /// <summary>
    /// Validate a voucher code without applying it.
    /// </summary>
    Task<VoucherValidationResultDto> ValidateVoucherAsync(string code);

    /// <summary>
    /// Apply a voucher: validate, credit wallet, track usage.
    /// Returns the result so the BFF can handle cache invalidation and notifications.
    /// </summary>
    Task<ApplyVoucherResultDto> ApplyVoucherAsync(Guid userId, string code);
}

/// <summary>
/// Result of voucher validation.
/// </summary>
public class VoucherValidationResultDto
{
    public bool IsValid { get; set; }
    public VoucherInfoDto? Voucher { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Voucher info returned from validation/application.
/// </summary>
public class VoucherInfoDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Enums.VoucherType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Description { get; set; }
    public int RemainingQuantity { get; set; }
}

/// <summary>
/// Result of applying a voucher to a user's wallet.
/// </summary>
public class ApplyVoucherResultDto
{
    public bool Success { get; set; }
    public decimal? NewBalance { get; set; }
    public decimal? CreditAmount { get; set; }
    public Guid UserId { get; set; }
    public string? Error { get; set; }
}
