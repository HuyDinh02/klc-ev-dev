using System.Security.Claims;
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

        // POST /api/v1/promotions/{id}/claim
        group.MapPost("/{id:guid}/claim", async (
            Guid id,
            ClaimsPrincipal user,
            IPromotionBffService promotionService) =>
        {
            var userId = GetUserId(user);
            var result = await promotionService.ClaimVoucherFromPromotionAsync(userId, id);

            return result.Success
                ? Results.Ok(new { data = result })
                : Results.BadRequest(new { error = new { code = "CLAIM_FAILED", message = result.Error } });
        })
        .WithName("ClaimVoucherFromPromotion")
        .WithSummary("Claim a voucher from a promotion")
        .Produces<object>(200)
        .Produces(400);

        // GET /api/v1/promotions/{id}/vouchers
        group.MapGet("/{id:guid}/vouchers", async (
            Guid id,
            IPromotionBffService promotionService) =>
        {
            var vouchers = await promotionService.GetPromotionVouchersAsync(id);
            return Results.Ok(new { data = vouchers });
        })
        .WithName("GetPromotionVouchers")
        .WithSummary("List available vouchers for a promotion")
        .Produces<object>(200);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
