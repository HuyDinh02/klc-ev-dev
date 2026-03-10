using KLC.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Services;

public class PromotionBffService : IPromotionBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<PromotionBffService> _logger;

    public PromotionBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<PromotionBffService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<PromotionListItemDto>> GetActivePromotionsAsync(Guid? cursor, int pageSize)
    {
        var cacheKey = cursor.HasValue
            ? $"promotions:active:cursor:{cursor}:size:{pageSize}"
            : $"promotions:active:first:size:{pageSize}";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var now = DateTime.UtcNow;

            var query = _dbContext.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive
                            && !p.IsDeleted
                            && p.StartDate <= now
                            && p.EndDate >= now)
                .OrderByDescending(p => p.CreationTime);

            if (cursor.HasValue)
            {
                var cursorPromotion = await _dbContext.Promotions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == cursor.Value);

                if (cursorPromotion != null)
                {
                    query = (IOrderedQueryable<KLC.Marketing.Promotion>)query
                        .Where(p => p.CreationTime < cursorPromotion.CreationTime);
                }
            }

            var promotions = await query
                .Take(pageSize + 1)
                .Select(p => new PromotionListItemDto
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

            var hasMore = promotions.Count > pageSize;
            var items = hasMore ? promotions.Take(pageSize).ToList() : promotions;
            var nextCursor = hasMore && items.Count > 0 ? items[^1].Id : (Guid?)null;

            return new PagedResult<PromotionListItemDto>
            {
                Data = items,
                NextCursor = nextCursor,
                HasMore = hasMore,
                PageSize = pageSize
            };
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
        }, TimeSpan.FromMinutes(10));
    }
}
