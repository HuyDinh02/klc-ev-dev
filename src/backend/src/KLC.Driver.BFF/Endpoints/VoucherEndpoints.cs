using System.Security.Claims;
using KLC.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class VoucherEndpoints
{
    public static void MapVoucherEndpoints(this IEndpointRouteBuilder app)
    {
        var voucherGroup = app.MapGroup("/api/v1/vouchers")
            .WithTags("Vouchers")
            .RequireAuthorization();

        // GET /api/v1/vouchers
        voucherGroup.MapGet("/", async (
            ClaimsPrincipal user,
            IVoucherBffService voucherService) =>
        {
            var userId = GetUserId(user);
            var vouchers = await voucherService.GetAvailableVouchersAsync(userId);
            return Results.Ok(new { data = vouchers });
        })
        .WithName("GetAvailableVouchers")
        .WithSummary("List available vouchers for the current user")
        .Produces<object>(200);

        // POST /api/v1/vouchers/apply
        voucherGroup.MapPost("/apply", async (
            [FromBody] ApplyVoucherRequest request,
            ClaimsPrincipal user,
            IVoucherBffService voucherService) =>
        {
            var userId = GetUserId(user);
            var result = await voucherService.ApplyVoucherAsync(userId, request.Code);

            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(new { error = new { code = "VOUCHER_APPLY_FAILED", message = result.Error } });
        })
        .WithName("ApplyVoucher")
        .WithSummary("Apply a voucher code to receive wallet credit")
        .Produces<ApplyVoucherResponse>(200)
        .Produces(400);

        // POST /api/v1/vouchers/validate
        voucherGroup.MapPost("/validate", async (
            [FromBody] ValidateVoucherRequest request,
            IVoucherBffService voucherService) =>
        {
            var result = await voucherService.ValidateVoucherAsync(request.Code);

            return result.IsValid
                ? Results.Ok(result)
                : Results.BadRequest(new { error = new { code = "VOUCHER_INVALID", message = result.Error } });
        })
        .WithName("ValidateVoucher")
        .WithSummary("Validate a voucher code")
        .Produces<VoucherValidationResultDto>(200)
        .Produces(400);

        // Promotions group
        var promotionGroup = app.MapGroup("/api/v1/promotions")
            .WithTags("Promotions")
            .RequireAuthorization();

        // GET /api/v1/promotions
        promotionGroup.MapGet("/", async (
            IVoucherBffService voucherService) =>
        {
            var promotions = await voucherService.GetActivePromotionsAsync();
            return Results.Ok(new { data = promotions });
        })
        .WithName("GetActivePromotions")
        .WithSummary("List active promotions")
        .Produces<object>(200);

        // GET /api/v1/promotions/{id}
        promotionGroup.MapGet("/{id:guid}", async (
            Guid id,
            IVoucherBffService voucherService) =>
        {
            var promotion = await voucherService.GetPromotionDetailAsync(id);

            return promotion is null
                ? Results.NotFound(new { error = new { code = "PROMOTION_NOT_FOUND", message = "Promotion not found" } })
                : Results.Ok(promotion);
        })
        .WithName("GetPromotionDetail")
        .WithSummary("Get promotion details")
        .Produces<PromotionDetailDto>(200)
        .Produces(404);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public record ValidateVoucherRequest
{
    public string Code { get; init; } = string.Empty;
}
