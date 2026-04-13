using System.Linq;
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
            HttpContext httpContext,
            [FromBody] TopUpRequest request,
            ClaimsPrincipal user,
            IWalletBffService walletService) =>
        {
            var userId = GetUserId(user);
            request = request with { ClientIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() };
            var result = await walletService.TopUpAsync(userId, request);

            return result.Success
                ? Results.Created($"/api/v1/wallet/topup/{result.TransactionId}/status", result)
                : Results.BadRequest(new { error = new { code = "TOPUP_FAILED", message = result.Error } });
        })
        .WithName("InitiateTopUp")
        .WithSummary("Initiate a wallet top-up")
        .Produces<TopUpResultDto>(201)
        .Produces(400);

        // GET /api/v1/wallet/topup/vnpay-ipn — VNPay IPN callback (GET with query params)
        // Case 13: Whitelist VnPay IPN IPs + log calling IP
        group.MapGet("/topup/vnpay-ipn", async (
            HttpContext httpContext,
            IWalletBffService walletService,
            ILogger<WalletBffService> logger,
            IConfiguration configuration) =>
        {
            // On Cloud Run, RemoteIpAddress is the internal proxy (169.254.x.x).
            // X-Forwarded-For contains the real client IP (set by Google Front End).
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            var callerIp = !string.IsNullOrEmpty(forwardedFor)
                ? forwardedFor.Split(',')[0].Trim()  // First IP in chain is the original client
                : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var txnRef = httpContext.Request.Query["vnp_TxnRef"].FirstOrDefault() ?? "?";

            logger.LogInformation("[VnPay IPN] Received: TxnRef={TxnRef}, CallerIP={CallerIP}", txnRef, callerIp);

            // Whitelist VnPay IPN source IPs (Case 13) — supports individual IPs and CIDR notation
            // VnPay uses multiple IPs across 103.220.84.0/22 subnet; CIDR avoids breaking on new IPs
            var whitelistStr = configuration["Payment:VnPay:IpnWhitelist"] ?? "";
            if (string.IsNullOrEmpty(whitelistStr))
            {
                logger.LogError("[VnPay IPN] REJECTED: IPN whitelist not configured. TxnRef={TxnRef}, CallerIP={CallerIP}", txnRef, callerIp);
                return Results.Json(new KLC.Payments.VnPayIpnResponse { RspCode = "99", Message = "IPN whitelist not configured" });
            }

            if (!System.Net.IPAddress.TryParse(callerIp, out var callerAddr))
            {
                logger.LogWarning("[VnPay IPN] REJECTED: Invalid IP {CallerIP}, TxnRef={TxnRef}", callerIp, txnRef);
                return Results.Json(new KLC.Payments.VnPayIpnResponse { RspCode = "99", Message = "Unauthorized IP" });
            }

            var entries = whitelistStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ipAllowed = false;
            foreach (var entry in entries)
            {
                if (entry.Contains('/'))
                {
                    // CIDR notation: e.g. 103.220.84.0/22
                    var parts = entry.Split('/');
                    if (System.Net.IPAddress.TryParse(parts[0], out var network) && int.TryParse(parts[1], out var prefixLen))
                    {
                        var networkBytes = network.GetAddressBytes();
                        var addrBytes = callerAddr.GetAddressBytes();
                        if (networkBytes.Length == addrBytes.Length)
                        {
                            var match = true;
                            var bits = prefixLen;
                            for (int i = 0; i < networkBytes.Length && bits > 0; i++)
                            {
                                var mask = (byte)(bits >= 8 ? 0xFF : (0xFF << (8 - bits)));
                                if ((networkBytes[i] & mask) != (addrBytes[i] & mask)) { match = false; break; }
                                bits -= 8;
                            }
                            if (match) { ipAllowed = true; break; }
                        }
                    }
                }
                else if (entry == callerIp)
                {
                    ipAllowed = true;
                    break;
                }
            }

            if (!ipAllowed)
            {
                logger.LogWarning("[VnPay IPN] REJECTED: IP {CallerIP} not in whitelist, TxnRef={TxnRef}", callerIp, txnRef);
                return Results.Json(new KLC.Payments.VnPayIpnResponse { RspCode = "99", Message = "Unauthorized IP" });
            }

            var queryParams = httpContext.Request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString());

            try
            {
                var result = await walletService.ProcessVnPayIpnAsync(queryParams);
                logger.LogInformation("[VnPay IPN] Response: TxnRef={TxnRef}, RspCode={RspCode}, CallerIP={CallerIP}",
                    txnRef, result.RspCode, callerIp);
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                // Case 11: System error → return 99
                logger.LogError(ex, "[VnPay IPN] Unhandled error: TxnRef={TxnRef}, CallerIP={CallerIP}", txnRef, callerIp);
                return Results.Json(new KLC.Payments.VnPayIpnResponse { RspCode = "99", Message = "Unknow error" });
            }
        })
        .WithName("VnPayTopUpIpn")
        .WithSummary("VNPay IPN callback for wallet top-up")
        .Produces<KLC.Payments.VnPayIpnResponse>(200)
        .AllowAnonymous();

        // POST /api/v1/wallet/topup/callback
        // Case 12: This endpoint does NOT credit the wallet for VnPay.
        // VnPay payments are ONLY processed via IPN (server-to-server).
        // This callback is for MoMo/ZaloPay only (which don't have IPN).
        // For VnPay, mobile should poll /topup/{id}/status instead.
        group.MapPost("/topup/callback", async (
            HttpContext httpContext,
            [FromBody] TopUpCallbackRequest request,
            IWalletBffService walletService,
            ILogger<WalletBffService> logger,
            IEnumerable<KLC.Payments.IPaymentGatewayService> paymentGateways) =>
        {
            // Block VnPay from using this endpoint — VnPay uses IPN only (Case 12)
            if (request.Gateway == PaymentGateway.VnPay || request.Gateway == null)
            {
                logger.LogWarning("[Callback] VnPay callback blocked — use IPN instead. Ref={Ref}",
                    request.ReferenceCode);
                return Results.Ok(new TopUpCallbackResultDto
                {
                    Success = false,
                    Error = "VnPay payments are confirmed via IPN. Please check /topup/{id}/status."
                });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.ReferenceCode))
            {
                return Results.BadRequest(new
                {
                    error = new { code = "INVALID_REQUEST", message = "ReferenceCode is required" }
                });
            }

            // Verify HMAC signature from the gateway — MANDATORY for all callbacks
            var signature = httpContext.Request.Headers["X-Payment-Signature"].FirstOrDefault()
                            ?? httpContext.Request.Query["signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                logger.LogWarning("[Callback] Missing signature, Ref={Ref}, Gateway={Gateway}",
                    request.ReferenceCode, request.Gateway);
                return Results.BadRequest(new
                {
                    error = new { code = "MISSING_SIGNATURE", message = "Payment callback signature is required" }
                });
            }

            var gateway = paymentGateways.FirstOrDefault(g => g.Gateway == request.Gateway);
            if (gateway == null)
            {
                return Results.BadRequest(new
                {
                    error = new { code = "UNKNOWN_GATEWAY", message = $"Gateway {request.Gateway} is not configured" }
                });
            }

            var parameters = new Dictionary<string, string>
            {
                { "referenceCode", request.ReferenceCode },
                { "gatewayTransactionId", request.GatewayTransactionId ?? string.Empty },
                { "status", ((int)request.Status).ToString() }
            };

            if (!gateway.VerifyCallbackSignature(parameters, signature))
            {
                logger.LogWarning("[Callback] Invalid signature, Ref={Ref}, Gateway={Gateway}",
                    request.ReferenceCode, request.Gateway);
                return Results.BadRequest(new
                {
                    error = new { code = "INVALID_SIGNATURE", message = "Payment callback signature verification failed" }
                });
            }

            var result = await walletService.ProcessTopUpCallbackAsync(request);

            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(new { error = new { code = "CALLBACK_FAILED", message = result.Error } });
        })
        .WithName("TopUpCallback")
        .WithSummary("Payment gateway callback for top-up (MoMo/ZaloPay only, NOT VnPay)")
        .Produces<TopUpCallbackResultDto>(200)
        .Produces(400)
        .AllowAnonymous();

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
