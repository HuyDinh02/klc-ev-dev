using System.Security.Claims;
using KLC.Driver.Services;
using KLC.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var paymentGroup = app.MapGroup("/api/v1/payments")
            .WithTags("Payments")
            .RequireAuthorization();

        // POST /api/v1/payments/process
        paymentGroup.MapPost("/process", async (
            HttpContext httpContext,
            [FromBody] ProcessPaymentRequest request,
            ClaimsPrincipal user,
            IPaymentBffService paymentService) =>
        {
            var userId = GetUserId(user);
            request = request with { ClientIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() };
            var result = await paymentService.ProcessPaymentAsync(userId, request);

            return result.Success
                ? Results.Created($"/api/v1/payments/{result.PaymentId}", result)
                : Results.BadRequest(new { error = new { code = "PAYMENT_FAILED", message = result.Error } });
        })
        .WithName("ProcessPayment")
        .WithSummary("Process payment for a completed session")
        .Produces<PaymentResultDto>(201)
        .Produces(400);

        // GET /api/v1/payments/history
        paymentGroup.MapGet("/history", async (
            [FromQuery] Guid? cursor,
            [FromQuery] int pageSize = 20,
            ClaimsPrincipal user = null!,
            IPaymentBffService paymentService = null!) =>
        {
            var userId = GetUserId(user);
            if (pageSize <= 0 || pageSize > 50) pageSize = 20;

            var result = await paymentService.GetPaymentHistoryAsync(userId, cursor, pageSize);
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
        .WithName("GetPaymentHistory")
        .WithSummary("Get payment history")
        .Produces<object>(200);

        // GET /api/v1/payments/{id}
        paymentGroup.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IPaymentBffService paymentService) =>
        {
            var userId = GetUserId(user);
            var payment = await paymentService.GetPaymentDetailAsync(userId, id);

            return payment is null
                ? Results.NotFound(new { error = new { code = "PAYMENT_NOT_FOUND", message = "Payment not found" } })
                : Results.Ok(payment);
        })
        .WithName("GetPaymentDetail")
        .WithSummary("Get payment details")
        .Produces<PaymentDetailDto>(200)
        .Produces(404);

        // POST /api/v1/payments/callback
        paymentGroup.MapPost("/callback", async (
            HttpContext httpContext,
            [FromBody] PaymentCallbackRequestDto request,
            IEnumerable<KLC.Payments.IPaymentGatewayService> paymentGateways) =>
        {
            var gateway = paymentGateways.FirstOrDefault(g => g.Gateway == request.Gateway);
            if (gateway == null)
            {
                return Results.BadRequest(new
                {
                    error = new { code = "UNSUPPORTED_GATEWAY", message = $"Gateway {request.Gateway} not supported" }
                });
            }

            // Verify HMAC signature
            var signature = request.Signature
                            ?? httpContext.Request.Headers["X-Payment-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                return Results.BadRequest(new
                {
                    error = new { code = "MISSING_SIGNATURE", message = "Payment callback signature is required" }
                });
            }

            if (!gateway.VerifyCallbackSignature(request.Parameters ?? new(), signature))
            {
                return Results.BadRequest(new
                {
                    error = new { code = "INVALID_SIGNATURE", message = "Payment callback signature verification failed" }
                });
            }

            // Signature verified — process the callback via VerifyCallbackAsync
            var result = await gateway.VerifyCallbackAsync(request.RawData ?? string.Empty, signature);

            return result.IsValid
                ? Results.Ok(new
                {
                    success = result.IsSuccess,
                    referenceCode = result.ReferenceCode,
                    gatewayTransactionId = result.GatewayTransactionId,
                    error = result.ErrorMessage
                })
                : Results.BadRequest(new
                {
                    error = new { code = "CALLBACK_INVALID", message = result.ErrorMessage ?? "Invalid callback" }
                });
        })
        .WithName("PaymentCallback")
        .WithSummary("Payment gateway callback with HMAC signature verification")
        .Produces<object>(200)
        .Produces(400)
        .AllowAnonymous(); // Gateway callbacks are authenticated via HMAC signature

        // Payment methods
        var methodGroup = app.MapGroup("/api/v1/payment-methods")
            .WithTags("Payment Methods")
            .RequireAuthorization();

        // GET /api/v1/payment-methods
        methodGroup.MapGet("/", async (
            ClaimsPrincipal user,
            IPaymentBffService paymentService) =>
        {
            var userId = GetUserId(user);
            var methods = await paymentService.GetPaymentMethodsAsync(userId);
            return Results.Ok(new { data = methods });
        })
        .WithName("GetPaymentMethods")
        .WithSummary("Get saved payment methods")
        .Produces<object>(200);

        // POST /api/v1/payment-methods
        methodGroup.MapPost("/", async (
            [FromBody] AddPaymentMethodRequest request,
            ClaimsPrincipal user,
            IPaymentBffService paymentService) =>
        {
            var userId = GetUserId(user);
            var method = await paymentService.AddPaymentMethodAsync(userId, request);
            return Results.Created($"/api/v1/payment-methods/{method.Id}", method);
        })
        .WithName("AddPaymentMethod")
        .WithSummary("Add a payment method")
        .Produces<PaymentMethodDto>(201);

        // DELETE /api/v1/payment-methods/{id}
        methodGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IPaymentBffService paymentService) =>
        {
            var userId = GetUserId(user);
            await paymentService.DeletePaymentMethodAsync(userId, id);
            return Results.NoContent();
        })
        .WithName("DeletePaymentMethod")
        .WithSummary("Delete a payment method")
        .Produces(204);

        // POST /api/v1/payment-methods/{id}/set-default
        methodGroup.MapPost("/{id:guid}/set-default", async (
            Guid id,
            ClaimsPrincipal user,
            IPaymentBffService paymentService) =>
        {
            var userId = GetUserId(user);
            await paymentService.SetDefaultPaymentMethodAsync(userId, id);
            return Results.NoContent();
        })
        .WithName("SetDefaultPaymentMethod")
        .WithSummary("Set default payment method")
        .Produces(204);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

/// <summary>
/// DTO for payment gateway callback with HMAC signature verification.
/// </summary>
public record PaymentCallbackRequestDto
{
    /// <summary>Payment gateway that sent this callback.</summary>
    public PaymentGateway Gateway { get; init; }

    /// <summary>HMAC signature from the payment gateway.</summary>
    public string? Signature { get; init; }

    /// <summary>Raw callback data for VerifyCallbackAsync processing.</summary>
    public string? RawData { get; init; }

    /// <summary>Parsed callback parameters for VerifyCallbackSignature verification.</summary>
    public Dictionary<string, string>? Parameters { get; init; }
}
