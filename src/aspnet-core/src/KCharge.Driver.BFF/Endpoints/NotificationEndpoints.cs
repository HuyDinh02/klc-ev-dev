using System.Security.Claims;
using KCharge.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KCharge.Driver.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        // GET /api/v1/notifications
        group.MapGet("/", async (
            [FromQuery] Guid? cursor,
            [FromQuery] int pageSize,
            ClaimsPrincipal user,
            INotificationBffService notificationService) =>
        {
            var userId = GetUserId(user);
            if (pageSize <= 0 || pageSize > 50) pageSize = 20;

            var result = await notificationService.GetNotificationsAsync(userId, cursor, pageSize);
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
        .WithName("GetNotifications")
        .WithSummary("Get notifications")
        .Produces<object>(200);

        // GET /api/v1/notifications/unread-count
        group.MapGet("/unread-count", async (
            ClaimsPrincipal user,
            INotificationBffService notificationService) =>
        {
            var userId = GetUserId(user);
            var count = await notificationService.GetUnreadCountAsync(userId);
            return Results.Ok(new { count });
        })
        .WithName("GetUnreadCount")
        .WithSummary("Get unread notification count")
        .Produces<object>(200);

        // PUT /api/v1/notifications/{id}/read
        group.MapPut("/{id:guid}/read", async (
            Guid id,
            ClaimsPrincipal user,
            INotificationBffService notificationService) =>
        {
            var userId = GetUserId(user);
            await notificationService.MarkAsReadAsync(userId, id);
            return Results.NoContent();
        })
        .WithName("MarkNotificationRead")
        .WithSummary("Mark notification as read")
        .Produces(204);

        // PUT /api/v1/notifications/read-all
        group.MapPut("/read-all", async (
            ClaimsPrincipal user,
            INotificationBffService notificationService) =>
        {
            var userId = GetUserId(user);
            await notificationService.MarkAllAsReadAsync(userId);
            return Results.NoContent();
        })
        .WithName("MarkAllNotificationsRead")
        .WithSummary("Mark all notifications as read")
        .Produces(204);

        // Devices group for FCM registration
        var deviceGroup = app.MapGroup("/api/v1/devices")
            .WithTags("Devices")
            .RequireAuthorization();

        // POST /api/v1/devices/register
        deviceGroup.MapPost("/register", async (
            [FromBody] RegisterDeviceRequest request,
            ClaimsPrincipal user,
            INotificationBffService notificationService) =>
        {
            var userId = GetUserId(user);
            await notificationService.RegisterDeviceAsync(userId, request.FcmToken);
            return Results.NoContent();
        })
        .WithName("RegisterDevice")
        .WithSummary("Register device for push notifications")
        .Produces(204);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public record RegisterDeviceRequest
{
    public string FcmToken { get; init; } = string.Empty;
}
