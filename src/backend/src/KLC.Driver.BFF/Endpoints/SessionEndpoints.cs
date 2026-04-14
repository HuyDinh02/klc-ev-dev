using System.Security.Claims;
using KLC.Driver.Services;
using KLC.Enums;
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

        // GET /api/v1/sessions/history?period=1d|7d|30d|all
        group.MapGet("/history", async (
            [FromQuery] Guid? cursor,
            [FromQuery] int? pageSize,
            [FromQuery] int? limit,
            [FromQuery] string? period,
            ClaimsPrincipal user = null!,
            ISessionBffService sessionService = null!) =>
        {
            var userId = GetUserId(user);
            var size = pageSize ?? limit ?? 20;
            if (size <= 0 || size > 50) size = 20;

            // Parse period filter
            DateTime? fromDate = period switch
            {
                "1d" => DateTime.UtcNow.AddDays(-1),
                "7d" => DateTime.UtcNow.AddDays(-7),
                "30d" => DateTime.UtcNow.AddDays(-30),
                "90d" => DateTime.UtcNow.AddDays(-90),
                _ => null // "all" or no filter
            };

            var result = await sessionService.GetSessionHistoryAsync(userId, cursor, size, fromDate);
            return Results.Ok(new
            {
                items = result.Data.Select(s => new
                {
                    id = s.SessionId,
                    stationId = s.StationId,
                    stationName = s.StationName,
                    connectorType = s.ConnectorType,
                    status = MapStatus(s.Status),
                    startTime = s.StartTime?.AddHours(7),
                    endTime = s.EndTime?.AddHours(7),
                    energyKwh = s.EnergyKwh,
                    durationMinutes = s.DurationMinutes,
                    durationSeconds = s.StartTime.HasValue && s.EndTime.HasValue
                        ? (int)(s.EndTime.Value - s.StartTime.Value).TotalSeconds : 0,
                    actualCost = s.TotalCost,
                    estimatedCost = s.TotalCost,
                    ratePerKwh = s.RatePerKwh,
                    formattedRate = $"{s.RatePerKwh:N0}đ/kWh"
                }),
                nextCursor = result.NextCursor,
                hasMore = result.HasMore
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

    private static string MapStatus(SessionStatus status) => status switch
    {
        SessionStatus.Completed => "Completed",
        SessionStatus.Failed => "Failed",
        SessionStatus.InProgress or SessionStatus.Starting or SessionStatus.Stopping or SessionStatus.Suspended => "Active",
        _ => "Active"
    };
}
