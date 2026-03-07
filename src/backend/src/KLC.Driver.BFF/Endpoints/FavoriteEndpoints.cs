using System.Security.Claims;
using KLC.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class FavoriteEndpoints
{
    public static void MapFavoriteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/favorites")
            .WithTags("Favorites")
            .RequireAuthorization();

        // GET /api/v1/favorites
        group.MapGet("/", async (
            ClaimsPrincipal user,
            IFavoriteBffService favoriteService) =>
        {
            var userId = GetUserId(user);
            var favorites = await favoriteService.GetFavoritesAsync(userId);
            return Results.Ok(favorites);
        })
        .WithName("GetFavorites")
        .WithSummary("Get user's favorite stations")
        .Produces<List<FavoriteStationDto>>(200);

        // POST /api/v1/favorites/{stationId}
        group.MapPost("/{stationId:guid}", async (
            Guid stationId,
            ClaimsPrincipal user,
            IFavoriteBffService favoriteService) =>
        {
            var userId = GetUserId(user);
            var result = await favoriteService.AddFavoriteAsync(userId, stationId);
            return Results.Created($"/api/v1/favorites/{stationId}", result);
        })
        .WithName("AddFavorite")
        .WithSummary("Add a station to favorites")
        .Produces<FavoriteStationDto>(201)
        .Produces(400)
        .Produces(404);

        // DELETE /api/v1/favorites/{stationId}
        group.MapDelete("/{stationId:guid}", async (
            Guid stationId,
            ClaimsPrincipal user,
            IFavoriteBffService favoriteService) =>
        {
            var userId = GetUserId(user);
            await favoriteService.RemoveFavoriteAsync(userId, stationId);
            return Results.NoContent();
        })
        .WithName("RemoveFavorite")
        .WithSummary("Remove a station from favorites")
        .Produces(204)
        .Produces(404);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
