using System.Security.Claims;
using KCharge.Driver.Services;
using Microsoft.AspNetCore.Mvc;

namespace KCharge.Driver.Endpoints;

public static class VehicleEndpoints
{
    public static void MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/vehicles")
            .WithTags("Vehicles")
            .RequireAuthorization();

        // GET /api/v1/vehicles
        group.MapGet("/", async (
            ClaimsPrincipal user,
            IVehicleBffService vehicleService) =>
        {
            var userId = GetUserId(user);
            var vehicles = await vehicleService.GetVehiclesAsync(userId);
            return Results.Ok(new { data = vehicles });
        })
        .WithName("GetVehicles")
        .WithSummary("Get user's vehicles")
        .Produces<object>(200);

        // GET /api/v1/vehicles/default
        group.MapGet("/default", async (
            ClaimsPrincipal user,
            IVehicleBffService vehicleService) =>
        {
            var userId = GetUserId(user);
            var vehicle = await vehicleService.GetDefaultVehicleAsync(userId);

            return vehicle is null
                ? Results.NoContent()
                : Results.Ok(vehicle);
        })
        .WithName("GetDefaultVehicle")
        .WithSummary("Get default vehicle")
        .Produces<VehicleDto>(200)
        .Produces(204);

        // GET /api/v1/vehicles/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IVehicleBffService vehicleService) =>
        {
            var userId = GetUserId(user);
            var vehicle = await vehicleService.GetVehicleAsync(userId, id);

            return vehicle is null
                ? Results.NotFound(new { error = new { code = "VEHICLE_NOT_FOUND", message = "Vehicle not found" } })
                : Results.Ok(vehicle);
        })
        .WithName("GetVehicle")
        .WithSummary("Get vehicle details")
        .Produces<VehicleDto>(200)
        .Produces(404);

        // POST /api/v1/vehicles
        group.MapPost("/", async (
            [FromBody] AddVehicleRequest request,
            ClaimsPrincipal user,
            IVehicleBffService vehicleService) =>
        {
            var userId = GetUserId(user);
            var vehicle = await vehicleService.AddVehicleAsync(userId, request);
            return Results.Created($"/api/v1/vehicles/{vehicle.Id}", vehicle);
        })
        .WithName("AddVehicle")
        .WithSummary("Add a vehicle")
        .Produces<VehicleDto>(201);

        // PUT /api/v1/vehicles/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateVehicleRequest request,
            ClaimsPrincipal user,
            IVehicleBffService vehicleService) =>
        {
            var userId = GetUserId(user);
            try
            {
                var vehicle = await vehicleService.UpdateVehicleAsync(userId, id, request);
                return Results.Ok(vehicle);
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound(new { error = new { code = "VEHICLE_NOT_FOUND", message = "Vehicle not found" } });
            }
        })
        .WithName("UpdateVehicle")
        .WithSummary("Update a vehicle")
        .Produces<VehicleDto>(200)
        .Produces(404);

        // DELETE /api/v1/vehicles/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            IVehicleBffService vehicleService) =>
        {
            var userId = GetUserId(user);
            await vehicleService.DeleteVehicleAsync(userId, id);
            return Results.NoContent();
        })
        .WithName("DeleteVehicle")
        .WithSummary("Delete a vehicle")
        .Produces(204);

        // POST /api/v1/vehicles/{id}/set-default
        group.MapPost("/{id:guid}/set-default", async (
            Guid id,
            ClaimsPrincipal user,
            IVehicleBffService vehicleService) =>
        {
            var userId = GetUserId(user);
            await vehicleService.SetDefaultVehicleAsync(userId, id);
            return Results.NoContent();
        })
        .WithName("SetDefaultVehicle")
        .WithSummary("Set default vehicle")
        .Produces(204);
    }

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
