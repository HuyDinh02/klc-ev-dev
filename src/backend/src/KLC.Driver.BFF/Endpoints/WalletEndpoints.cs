using System.Security.Claims;
using KLC.Driver.Services;
using KLC.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class WalletEndpoints
{
    public static void MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/wallet")
            .WithTags("Wallet")
            .RequireAuthorization();

        // GET /api/v1/wallet/balance
        group.MapGet("/balance", async (
            ClaimsPrincipal user,
            IWalletBffService walletService) =>
        {
            var userId = GetUserId(user);
            var balance = await walletService.GetBalanceAsync(userId);
            return Results.Ok(balance);
        })
        .WithName("GetWalletBalance")
        .WithSummary("Get current wallet balance")
        .Produces<WalletBalanceDto>(200);

        // POST /api/v1/wallet/topup
        group.MapPost("/topup", async (
            [FromBody] TopUpRequest request,
            ClaimsPrincipal user,
            IWalletBffService walletService) =>
        {
            var userId = GetUserId(user);
            var result = await walletService.TopUpAsync(userId, request);

            return result.Success
                ? Results.Created($"/api/v1/wallet/topup/{result.TransactionId}/status", result)
                : Results.BadRequest(new { error = new { code = "TOPUP_FAILED", message = result.Error } });
        })
        .WithName("InitiateTopUp")
        .WithSummary("Initiate a wallet top-up")
        .Produces<TopUpResultDto>(201)
        .Produces(400);

        // POST /api/v1/wallet/topup/callback
        group.MapPost("/topup/callback", async (
            [FromBody] TopUpCallbackRequest request,
            IWalletBffService walletService) =>
        {
            var result = await walletService.ProcessTopUpCallbackAsync(request);

            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(new { error = new { code = "CALLBACK_FAILED", message = result.Error } });
        })
        .WithName("TopUpCallback")
        .WithSummary("Payment gateway callback for top-up")
        .Produces<TopUpCallbackResultDto>(200)
        .Produces(400)
        .AllowAnonymous(); // Gateway callbacks are authenticated via reference code

        // GET /api/v1/wallet/topup/{id}/status
        group.MapGet("/topup/{id:guid}/status", async (
            Guid id,
            ClaimsPrincipal user,
            IWalletBffService walletService) =>
        {
            var userId = GetUserId(user);
            var status = await walletService.GetTopUpStatusAsync(userId, id);

            return status is null
                ? Results.NotFound(new { error = new { code = "TOPUP_NOT_FOUND", message = "Top-up transaction not found" } })
                : Results.Ok(status);
        })
        .WithName("GetTopUpStatus")
        .WithSummary("Check top-up transaction status")
        .Produces<TopUpStatusDto>(200)
        .Produces(404);

        // GET /api/v1/wallet/transactions
        group.MapGet("/transactions", async (
            [FromQuery] Guid? cursor,
            [FromQuery] WalletTransactionType? type,
            [FromQuery] int pageSize = 20,
            ClaimsPrincipal user = null!,
            IWalletBffService walletService = null!) =>
        {
            var userId = GetUserId(user);
            if (pageSize <= 0 || pageSize > 50) pageSize = 20;

            var result = await walletService.GetTransactionsAsync(userId, cursor, pageSize, type);
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
        .WithName("GetWalletTransactions")
        .WithSummary("Get wallet transaction history")
        .Produces<object>(200);

        // GET /api/v1/wallet/transactions/summary
        group.MapGet("/transactions/summary", async (
            ClaimsPrincipal user,
            IWalletBffService walletService) =>
        {
            var userId = GetUserId(user);
            var summary = await walletService.GetTransactionSummaryAsync(userId);
            return Results.Ok(summary);
        })
        .WithName("GetTransactionSummary")
        .WithSummary("Get aggregated transaction summary")
        .Produces<TransactionSummaryDto>(200);

        // GET /api/v1/wallet/transactions/{id}
        group.MapGet("/transactions/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IWalletBffService walletService) =>
        {
            var userId = GetUserId(user);
            var transaction = await walletService.GetTransactionDetailAsync(userId, id);

            return transaction is null
                ? Results.NotFound(new { error = new { code = "TRANSACTION_NOT_FOUND", message = "Transaction not found" } })
                : Results.Ok(transaction);
        })
        .WithName("GetTransactionDetail")
        .WithSummary("Get wallet transaction details")
        .Produces<WalletTransactionDetailDto>(200)
        .Produces(404);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
