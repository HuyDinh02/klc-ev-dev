using KLC.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Driver.Endpoints;

public static class StationEndpoints
{
    public static void MapStationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/stations")
            .WithTags("Stations");

        // GET /api/v1/stations/nearby?lat=...&lon=...&radius=...&limit=...
        // Public endpoint - no auth required
        group.MapGet("/nearby", async (
            [FromQuery] double lat,
            [FromQuery] double lon,
            [FromQuery] double radius = 10,
            [FromQuery] int limit = 20,
            IStationBffService stationService = null!) =>
        {
            if (radius <= 0) radius = 10;
            if (limit <= 0 || limit > 100) limit = 20;

            var stations = await stationService.GetNearbyStationsAsync(lat, lon, radius, limit);
            return Results.Ok(new { data = stations });
        })
        .WithName("GetNearbyStations")
        .WithSummary("Find nearby charging stations")
        .Produces<object>(200);

        // GET /api/v1/stations/search?q=...&limit=...
        // Public endpoint - no auth required (station discovery)
        group.MapGet("/search", async (
            [FromQuery] string q,
            [FromQuery] int limit = 20,
            IStationBffService stationService = null!) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.Ok(new { data = Array.Empty<NearbyStationDto>() });
            if (limit <= 0 || limit > 100) limit = 20;

            var stations = await stationService.SearchStationsAsync(q, limit);
            return Results.Ok(new { data = stations });
        })
        .WithName("SearchStations")
        .WithSummary("Search stations by name, address, or code")
        .Produces<object>(200);

        // GET /api/v1/stations/{id}
        // Public endpoint - no auth required (station discovery)
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

        // GET /api/v1/stations/{code} — Fallback: lookup by station code when ID is not a GUID
        // Mobile app sometimes uses station code (e.g., "cong-hoa-001") instead of UUID
        group.MapGet("/{code}", async (
            string code,
            IStationBffService stationService) =>
        {
            var station = await stationService.GetStationByCodeAsync(code);
            return station is null
                ? Results.NotFound(new { error = new { code = "STATION_NOT_FOUND", message = "Station not found" } })
                : Results.Ok(station);
        })
        .WithName("GetStationByCodeFallback")
        .WithSummary("Get station details by code (fallback for non-GUID identifiers)")
        .Produces<StationDetailDto>(200)
        .Produces(404);

        // GET /api/v1/stations/by-code/{code} — Lookup by station code (charger serial number)
        // Used when QR codes contain the charger's serial number instead of our station UUID
        group.MapGet("/by-code/{code}", async (
            string code,
            IStationBffService stationService) =>
        {
            var station = await stationService.GetStationByCodeAsync(code);
            return station is null
                ? Results.NotFound(new { error = new { code = "STATION_NOT_FOUND", message = $"Station with code '{code}' not found" } })
                : Results.Ok(station);
        })
        .WithName("GetStationByCode")
        .WithSummary("Get station details by station code (serial number)")
        .Produces<StationDetailDto>(200)
        .Produces(404);

        // GET /api/v1/stations/{id}/connectors
        // Public endpoint - no auth required (station discovery)
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
