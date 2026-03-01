using System.Security.Claims;
using KCharge.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KCharge.Driver.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var paymentGroup = app.MapGroup("/api/v1/payments")
            .WithTags("Payments")
            .RequireAuthorization();

        // POST /api/v1/payments/process
        paymentGroup.MapPost("/process", async (
            [FromBody] ProcessPaymentRequest request,
            ClaimsPrincipal user,
            IPaymentBffService paymentService) =>
        {
            var userId = GetUserId(user);
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
            [FromQuery] int pageSize,
            ClaimsPrincipal user,
            IPaymentBffService paymentService) =>
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
