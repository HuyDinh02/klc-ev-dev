using KLC.Enums;

namespace KLC.Driver.Services;

public interface IPromotionBffService
{
    Task<PagedResult<PromotionListItemDto>> GetActivePromotionsAsync(Guid? cursor, int pageSize);
    Task<PromotionDetailDto?> GetPromotionDetailAsync(Guid id);
    Task<ClaimVoucherResultDto> ClaimVoucherFromPromotionAsync(Guid userId, Guid promotionId);
    Task<List<PromotionVoucherDto>> GetPromotionVouchersAsync(Guid promotionId);
}

// DTOs
public record PromotionListItemDto
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

public record ClaimVoucherResultDto
{
    public bool Success { get; init; }
    public string? VoucherCode { get; init; }
    public Guid? VoucherId { get; init; }
    public string? Error { get; init; }
}

public record PromotionVoucherDto
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
