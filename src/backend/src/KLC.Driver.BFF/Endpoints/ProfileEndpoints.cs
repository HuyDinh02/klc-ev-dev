using System.Security.Claims;
using KLC.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        // GET /api/v1/profile
        group.MapGet("/", async (
            ClaimsPrincipal user,
            IProfileBffService profileService) =>
        {
            var userId = GetUserId(user);
            var profile = await profileService.GetProfileAsync(userId);

            return profile is null
                ? Results.NotFound(new { error = new { code = "PROFILE_NOT_FOUND", message = "Profile not found" } })
                : Results.Ok(profile);
        })
        .WithName("GetProfile")
        .WithSummary("Get user profile")
        .Produces<ProfileDto>(200)
        .Produces(404);

        // PUT /api/v1/profile
        group.MapPut("/", async (
            [FromBody] UpdateProfileRequest request,
            ClaimsPrincipal user,
            IProfileBffService profileService) =>
        {
            var userId = GetUserId(user);
            var profile = await profileService.UpdateProfileAsync(userId, request);
            return Results.Ok(profile);
        })
        .WithName("UpdateProfile")
        .WithSummary("Update user profile")
        .Produces<ProfileDto>(200);

        // GET /api/v1/profile/statistics
        group.MapGet("/statistics", async (
            ClaimsPrincipal user,
            IProfileBffService profileService) =>
        {
            var userId = GetUserId(user);
            var stats = await profileService.GetUserStatsAsync(userId);
            return Results.Ok(stats);
        })
        .WithName("GetUserStatistics")
        .WithSummary("Get user charging statistics")
        .Produces<UserStatsDto>(200);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
