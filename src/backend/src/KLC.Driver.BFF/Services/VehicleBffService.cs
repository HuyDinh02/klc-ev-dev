using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Services;

public interface IVehicleBffService
{
    Task<List<VehicleDto>> GetVehiclesAsync(Guid userId);
    Task<VehicleDto?> GetVehicleAsync(Guid userId, Guid vehicleId);
    Task<VehicleDto?> GetDefaultVehicleAsync(Guid userId);
    Task<VehicleDto> AddVehicleAsync(Guid userId, AddVehicleRequest request);
    Task<VehicleDto> UpdateVehicleAsync(Guid userId, Guid vehicleId, UpdateVehicleRequest request);
    Task DeleteVehicleAsync(Guid userId, Guid vehicleId);
    Task SetDefaultVehicleAsync(Guid userId, Guid vehicleId);
}

public class VehicleBffService : IVehicleBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<VehicleBffService> _logger;

    public VehicleBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<VehicleBffService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<VehicleDto>> GetVehiclesAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:vehicles";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            return await _dbContext.Vehicles
                .AsNoTracking()
                .Where(v => v.UserId == userId && v.IsActive)
                .OrderByDescending(v => v.IsDefault)
                .ThenByDescending(v => v.CreationTime)
                .Select(v => new VehicleDto
                {
                    Id = v.Id,
                    Make = v.Make,
                    Model = v.Model,
                    Year = v.Year,
                    LicensePlate = v.LicensePlate,
                    Color = v.Color,
                    Nickname = v.Nickname,
                    BatteryCapacityKwh = v.BatteryCapacityKwh,
                    PreferredConnectorType = v.PreferredConnectorType,
                    IsDefault = v.IsDefault
                })
                .ToListAsync();
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<VehicleDto?> GetVehicleAsync(Guid userId, Guid vehicleId)
    {
        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId);

        if (vehicle == null) return null;

        return new VehicleDto
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Color = vehicle.Color,
            Nickname = vehicle.Nickname,
            BatteryCapacityKwh = vehicle.BatteryCapacityKwh,
            PreferredConnectorType = vehicle.PreferredConnectorType,
            IsDefault = vehicle.IsDefault
        };
    }

    public async Task<VehicleDto?> GetDefaultVehicleAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:default-vehicle";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var vehicle = await _dbContext.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == userId && v.IsDefault && v.IsActive);

            if (vehicle == null) return null;

            return new VehicleDto
            {
                Id = vehicle.Id,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                LicensePlate = vehicle.LicensePlate,
                Color = vehicle.Color,
                Nickname = vehicle.Nickname,
                BatteryCapacityKwh = vehicle.BatteryCapacityKwh,
                PreferredConnectorType = vehicle.PreferredConnectorType,
                IsDefault = vehicle.IsDefault
            };
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<VehicleDto> AddVehicleAsync(Guid userId, AddVehicleRequest request)
    {
        var vehicle = new Vehicle(
            Guid.NewGuid(),
            userId,
            request.Make,
            request.Model,
            request.LicensePlate,
            request.BatteryCapacityKwh,
            request.PreferredConnectorType);

        vehicle.SetDetails(request.Color, request.Year, request.Nickname);

        // If first vehicle, make it default
        var hasOther = await _dbContext.Vehicles
            .AnyAsync(v => v.UserId == userId && v.IsActive);

        if (!hasOther)
        {
            vehicle.SetAsDefault();
        }

        await _dbContext.Vehicles.AddAsync(vehicle);
        await _dbContext.SaveChangesAsync();

        await InvalidateVehicleCache(userId);

        return new VehicleDto
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Color = vehicle.Color,
            Nickname = vehicle.Nickname,
            BatteryCapacityKwh = vehicle.BatteryCapacityKwh,
            PreferredConnectorType = vehicle.PreferredConnectorType,
            IsDefault = vehicle.IsDefault
        };
    }

    public async Task<VehicleDto> UpdateVehicleAsync(Guid userId, Guid vehicleId, UpdateVehicleRequest request)
    {
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId);

        if (vehicle == null)
            throw new InvalidOperationException("Vehicle not found");

        if (!string.IsNullOrEmpty(request.LicensePlate))
            vehicle.SetLicensePlate(request.LicensePlate);

        // Update details (color, year, nickname) together
        vehicle.SetDetails(
            request.Color ?? vehicle.Color,
            request.Year ?? vehicle.Year,
            request.Nickname ?? vehicle.Nickname);

        if (request.BatteryCapacityKwh.HasValue)
            vehicle.SetBatteryCapacity(request.BatteryCapacityKwh.Value);

        await _dbContext.SaveChangesAsync();
        await InvalidateVehicleCache(userId);

        return new VehicleDto
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            LicensePlate = vehicle.LicensePlate,
            Color = vehicle.Color,
            Nickname = vehicle.Nickname,
            BatteryCapacityKwh = vehicle.BatteryCapacityKwh,
            PreferredConnectorType = vehicle.PreferredConnectorType,
            IsDefault = vehicle.IsDefault
        };
    }

    public async Task DeleteVehicleAsync(Guid userId, Guid vehicleId)
    {
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId);

        if (vehicle != null)
        {
            vehicle.Deactivate();
            await _dbContext.SaveChangesAsync();
            await InvalidateVehicleCache(userId);
        }
    }

    public async Task SetDefaultVehicleAsync(Guid userId, Guid vehicleId)
    {
        var vehicles = await _dbContext.Vehicles
            .Where(v => v.UserId == userId && v.IsActive)
            .ToListAsync();

        foreach (var vehicle in vehicles)
        {
            if (vehicle.Id == vehicleId)
            {
                vehicle.SetAsDefault();
            }
            else if (vehicle.IsDefault)
            {
                vehicle.RemoveDefault();
            }
        }

        await _dbContext.SaveChangesAsync();
        await InvalidateVehicleCache(userId);
    }

    private async Task InvalidateVehicleCache(Guid userId)
    {
        await _cache.RemoveAsync($"user:{userId}:vehicles");
        await _cache.RemoveAsync($"user:{userId}:default-vehicle");
    }
}

// DTOs
public record VehicleDto
{
    public Guid Id { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int? Year { get; init; }
    public string? LicensePlate { get; init; }
    public string? Color { get; init; }
    public string? Nickname { get; init; }
    public decimal? BatteryCapacityKwh { get; init; }
    public ConnectorType? PreferredConnectorType { get; init; }
    public bool IsDefault { get; init; }
}

public record AddVehicleRequest
{
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int? Year { get; init; }
    public string? LicensePlate { get; init; }
    public string? Color { get; init; }
    public string? Nickname { get; init; }
    public decimal? BatteryCapacityKwh { get; init; }
    public ConnectorType? PreferredConnectorType { get; init; }
}

public record UpdateVehicleRequest
{
    public string? LicensePlate { get; init; }
    public string? Color { get; init; }
    public string? Nickname { get; init; }
    public int? Year { get; init; }
    public decimal? BatteryCapacityKwh { get; init; }
}
