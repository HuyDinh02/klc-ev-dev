using KLC.EntityFrameworkCore;
using KLC.Enums;
using Microsoft.EntityFrameworkCore;

namespace KLC.Driver.Services;

public interface IStationBffService
{
    Task<List<NearbyStationDto>> GetNearbyStationsAsync(double latitude, double longitude, double radiusKm, int limit);
    Task<StationDetailDto?> GetStationDetailAsync(Guid stationId);
    Task<List<ConnectorStatusDto>> GetConnectorStatusAsync(Guid stationId);
}

public class StationBffService : IStationBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<StationBffService> _logger;

    public StationBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<StationBffService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<NearbyStationDto>> GetNearbyStationsAsync(
        double latitude, double longitude, double radiusKm, int limit)
    {
        // For simplicity, using bounding box approximation
        // In production, use PostGIS for accurate distance calculations
        var latDelta = radiusKm / 111.0; // ~111km per degree latitude
        var lonDelta = radiusKm / (111.0 * Math.Cos(latitude * Math.PI / 180));

        var stations = await _dbContext.ChargingStations
            .AsNoTracking()
            .Where(s => s.IsEnabled && !s.IsDeleted)
            .Where(s => s.Latitude >= latitude - latDelta && s.Latitude <= latitude + latDelta)
            .Where(s => s.Longitude >= longitude - lonDelta && s.Longitude <= longitude + lonDelta)
            .Include(s => s.Connectors.Where(c => c.IsEnabled && !c.IsDeleted))
            .Take(limit)
            .Select(s => new NearbyStationDto
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Status = s.Status,
                AvailableConnectors = s.Connectors.Count(c => c.Status == ConnectorStatus.Available),
                TotalConnectors = s.Connectors.Count(),
                Distance = CalculateDistance(latitude, longitude, s.Latitude, s.Longitude)
            })
            .ToListAsync();

        return stations.OrderBy(s => s.Distance).ToList();
    }

    public async Task<StationDetailDto?> GetStationDetailAsync(Guid stationId)
    {
        var cacheKey = $"station:{stationId}:detail";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var station = await _dbContext.ChargingStations
                .AsNoTracking()
                .Include(s => s.Connectors.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(s => s.Id == stationId && !s.IsDeleted);

            if (station == null) return null;

            // Get tariff info
            var tariff = station.TariffPlanId.HasValue
                ? await _dbContext.TariffPlans.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == station.TariffPlanId.Value)
                : await _dbContext.TariffPlans.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.IsDefault && t.IsActive);

            return new StationDetailDto
            {
                Id = station.Id,
                StationCode = station.StationCode,
                Name = station.Name,
                Address = station.Address,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                Status = station.Status,
                IsEnabled = station.IsEnabled,
                Vendor = station.Vendor,
                Model = station.Model,
                RatePerKwh = tariff?.BaseRatePerKwh ?? 0,
                TaxRatePercent = tariff?.TaxRatePercent ?? 0,
                Connectors = station.Connectors.Select(c => new ConnectorStatusDto
                {
                    Id = c.Id,
                    ConnectorNumber = c.ConnectorNumber,
                    Type = c.ConnectorType,
                    Status = c.Status,
                    MaxPowerKw = c.MaxPowerKw,
                    IsEnabled = c.IsEnabled
                }).OrderBy(c => c.ConnectorNumber).ToList()
            };
        }, TimeSpan.FromMinutes(1));
    }

    public async Task<List<ConnectorStatusDto>> GetConnectorStatusAsync(Guid stationId)
    {
        var cacheKey = $"station:{stationId}:connectors";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            return await _dbContext.Connectors
                .AsNoTracking()
                .Where(c => c.StationId == stationId && !c.IsDeleted)
                .Select(c => new ConnectorStatusDto
                {
                    Id = c.Id,
                    ConnectorNumber = c.ConnectorNumber,
                    Type = c.ConnectorType,
                    Status = c.Status,
                    MaxPowerKw = c.MaxPowerKw,
                    IsEnabled = c.IsEnabled
                })
                .OrderBy(c => c.ConnectorNumber)
                .ToListAsync();
        }, TimeSpan.FromSeconds(30));
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        const double R = 6371; // Earth radius in km
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(R * c, 2);
    }
}

// DTOs
public record NearbyStationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public StationStatus Status { get; init; }
    public int AvailableConnectors { get; init; }
    public int TotalConnectors { get; init; }
    public double Distance { get; init; }
}

public record StationDetailDto
{
    public Guid Id { get; init; }
    public string StationCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public StationStatus Status { get; init; }
    public bool IsEnabled { get; init; }
    public string? Vendor { get; init; }
    public string? Model { get; init; }
    public decimal RatePerKwh { get; init; }
    public decimal TaxRatePercent { get; init; }
    public List<ConnectorStatusDto> Connectors { get; init; } = new();
}

public record ConnectorStatusDto
{
    public Guid Id { get; init; }
    public int ConnectorNumber { get; init; }
    public ConnectorType Type { get; init; }
    public ConnectorStatus Status { get; init; }
    public decimal MaxPowerKw { get; init; }
    public bool IsEnabled { get; init; }
}
