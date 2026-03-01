using KCharge.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KCharge.Driver.Endpoints;

public static class StationEndpoints
{
    public static void MapStationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/stations")
            .WithTags("Stations")
            .RequireAuthorization();

        // GET /api/v1/stations/nearby?lat=...&lon=...&radius=...&limit=...
        group.MapGet("/nearby", async (
            [FromQuery] double lat,
            [FromQuery] double lon,
            [FromQuery] double radius,
            [FromQuery] int limit,
            IStationBffService stationService) =>
        {
            if (radius <= 0) radius = 10; // Default 10km
            if (limit <= 0 || limit > 100) limit = 20;

            var stations = await stationService.GetNearbyStationsAsync(lat, lon, radius, limit);
            return Results.Ok(new { data = stations });
        })
        .WithName("GetNearbyStations")
        .WithSummary("Find nearby charging stations")
        .Produces<object>(200);

        // GET /api/v1/stations/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IStationBffService stationService) =>
        {
            var station = await stationService.GetStationDetailAsync(id);
            return station is null
                ? Results.NotFound(new { error = new { code = "STATION_NOT_FOUND", message = "Station not found" } })
                : Results.Ok(station);
        })
        .WithName("GetStationDetail")
        .WithSummary("Get station details")
        .Produces<StationDetailDto>(200)
        .Produces(404);

        // GET /api/v1/stations/{id}/connectors
        group.MapGet("/{id:guid}/connectors", async (
            Guid id,
            IStationBffService stationService) =>
        {
            var connectors = await stationService.GetConnectorStatusAsync(id);
            return Results.Ok(new { data = connectors });
        })
        .WithName("GetConnectorStatus")
        .WithSummary("Get connector status for a station")
        .Produces<object>(200);
    }
}
