using KLC.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class PromotionEndpoints
{
    public static void MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/promotions")
            .WithTags("Promotions")
            .RequireAuthorization();

        // GET /api/v1/promotions
        group.MapGet("/", async (
            [FromQuery] Guid? cursor,
            [FromQuery] int pageSize = 20,
            IPromotionBffService promotionService = null!) =>
        {
            if (pageSize <= 0 || pageSize > 50) pageSize = 20;

            var result = await promotionService.GetActivePromotionsAsync(cursor, pageSize);
            return Results.Ok(new
            {
                data = result.Data,
                pagination = new
                {
                    nextCursor = result.NextCursor,
                    hasMore = result.HasMore,
                    pageSize = result.PageSize
                }
            });
        })
        .WithName("GetActivePromotions")
        .WithSummary("List active promotions with cursor-based pagination")
        .Produces<object>(200);

        // GET /api/v1/promotions/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IPromotionBffService promotionService) =>
        {
            var promotion = await promotionService.GetPromotionDetailAsync(id);

            return promotion is null
                ? Results.NotFound(new { error = new { code = "PROMOTION_NOT_FOUND", message = "Promotion not found" } })
                : Results.Ok(new { data = promotion });
        })
        .WithName("GetPromotionDetail")
        .WithSummary("Get promotion details")
        .Produces<object>(200)
        .Produces(404);
    }
}
