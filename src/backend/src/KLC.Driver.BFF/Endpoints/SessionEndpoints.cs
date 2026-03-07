using System.Security.Claims;
using KLC.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sessions")
            .WithTags("Sessions")
            .RequireAuthorization();

        // POST /api/v1/sessions/start
        group.MapPost("/start", async (
            [FromBody] StartSessionRequest request,
            ClaimsPrincipal user,
            ISessionBffService sessionService) =>
        {
            var userId = GetUserId(user);
            var result = await sessionService.StartSessionAsync(userId, request);

            return result.Success
                ? Results.Created($"/api/v1/sessions/{result.SessionId}", result)
                : Results.BadRequest(new { error = new { code = "SESSION_START_FAILED", message = result.Error } });
        })
        .WithName("StartSession")
        .WithSummary("Start a charging session")
        .Produces<SessionResponseDto>(201)
        .Produces(400);

        // POST /api/v1/sessions/{id}/stop
        group.MapPost("/{id:guid}/stop", async (
            Guid id,
            ClaimsPrincipal user,
            ISessionBffService sessionService) =>
        {
            var userId = GetUserId(user);
            var result = await sessionService.StopSessionAsync(userId, id);

            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(new { error = new { code = "SESSION_STOP_FAILED", message = result.Error } });
        })
        .WithName("StopSession")
        .WithSummary("Stop a charging session")
        .Produces<SessionResponseDto>(200)
        .Produces(400);

        // GET /api/v1/sessions/active
        group.MapGet("/active", async (
            ClaimsPrincipal user,
            ISessionBffService sessionService) =>
        {
            var userId = GetUserId(user);
            var session = await sessionService.GetActiveSessionAsync(userId);

            return session is null
                ? Results.NoContent()
                : Results.Ok(session);
        })
        .WithName("GetActiveSession")
        .WithSummary("Get current active charging session")
        .Produces<ActiveSessionDto>(200)
        .Produces(204);

        // GET /api/v1/sessions/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            ISessionBffService sessionService) =>
        {
            var userId = GetUserId(user);
            var session = await sessionService.GetSessionDetailAsync(userId, id);

            return session is null
                ? Results.NotFound(new { error = new { code = "SESSION_NOT_FOUND", message = "Session not found" } })
                : Results.Ok(session);
        })
        .WithName("GetSessionDetail")
        .WithSummary("Get session details")
        .Produces<SessionDetailDto>(200)
        .Produces(404);

        // GET /api/v1/sessions/history
        group.MapGet("/history", async (
            [FromQuery] Guid? cursor,
            [FromQuery] int pageSize = 20,
            ClaimsPrincipal user = null!,
            ISessionBffService sessionService = null!) =>
        {
            var userId = GetUserId(user);
            if (pageSize <= 0 || pageSize > 50) pageSize = 20;

            var result = await sessionService.GetSessionHistoryAsync(userId, cursor, pageSize);
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
        .WithName("GetSessionHistory")
        .WithSummary("Get charging session history")
        .Produces<object>(200);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
