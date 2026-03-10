using KLC.Enums;

namespace KLC.Driver.Services;

public interface IPromotionBffService
{
    Task<PagedResult<PromotionListItemDto>> GetActivePromotionsAsync(Guid? cursor, int pageSize);
    Task<PromotionDetailDto?> GetPromotionDetailAsync(Guid id);
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
