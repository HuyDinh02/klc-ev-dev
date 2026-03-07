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

        // POST /api/v1/profile/avatar
        group.MapPost("/avatar", async (
            IFormFile file,
            ClaimsPrincipal user,
            IProfileBffService profileService) =>
        {
            if (file.Length == 0 || file.Length > 5 * 1024 * 1024) // 5MB max
            {
                return Results.BadRequest(new { error = new { code = "INVALID_FILE", message = "File must be between 1 byte and 5MB" } });
            }

            var userId = GetUserId(user);
            await using var stream = file.OpenReadStream();
            var result = await profileService.UpdateAvatarAsync(userId, stream, file.FileName);
            return Results.Ok(result);
        })
        .WithName("UpdateAvatar")
        .WithSummary("Update profile avatar (multipart/form-data)")
        .Produces<ProfileDto>(200)
        .DisableAntiforgery();

        // POST /api/v1/profile/change-phone
        group.MapPost("/change-phone", async (
            [FromBody] ChangePhoneRequest request,
            ClaimsPrincipal user,
            IProfileBffService profileService) =>
        {
            var userId = GetUserId(user);
            await profileService.RequestPhoneChangeAsync(userId, request.NewPhoneNumber);
            return Results.Ok(new { message = "OTP sent to new phone number" });
        })
        .WithName("ChangePhone")
        .WithSummary("Request phone number change")
        .Produces<object>(200);

        // POST /api/v1/profile/verify-phone-change
        group.MapPost("/verify-phone-change", async (
            [FromBody] VerifyPhoneChangeRequest request,
            ClaimsPrincipal user,
            IProfileBffService profileService) =>
        {
            var userId = GetUserId(user);
            await profileService.VerifyPhoneChangeAsync(userId, request.NewPhoneNumber, request.Otp);
            return Results.Ok(new { message = "Phone number updated successfully" });
        })
        .WithName("VerifyPhoneChange")
        .WithSummary("Verify phone number change with OTP")
        .Produces<object>(200)
        .Produces(400);

        // DELETE /api/v1/profile
        group.MapDelete("/", async (
            ClaimsPrincipal user,
            IProfileBffService profileService) =>
        {
            var userId = GetUserId(user);
            await profileService.DeleteAccountAsync(userId);
            return Results.NoContent();
        })
        .WithName("DeleteAccount")
        .WithSummary("Soft-delete user account")
        .Produces(204)
        .Produces(400);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
