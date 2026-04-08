using KLC.EntityFrameworkCore;
using KLC.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace KLC.Driver.Services;

public interface IStationBffService
{
    Task<List<NearbyStationDto>> GetNearbyStationsAsync(double latitude, double longitude, double radiusKm, int limit);
    Task<List<NearbyStationDto>> SearchStationsAsync(string query, int limit);
    Task<StationDetailDto?> GetStationDetailAsync(Guid stationId);
    Task<StationDetailDto?> GetStationByCodeAsync(string stationCode);
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
        var userLocation = new Point(longitude, latitude) { SRID = 4326 };
        var radiusMeters = radiusKm * 1000;

        var stations = await _dbContext.ChargingStations
            .AsNoTracking()
            .Where(s => s.IsEnabled && !s.IsDeleted && s.Location != null)
            .Where(s => s.Location!.IsWithinDistance(userLocation, radiusMeters))
            .Include(s => s.Connectors.Where(c => c.IsEnabled && !c.IsDeleted))
            .OrderBy(s => s.Location!.Distance(userLocation))
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
                Distance = Math.Round(s.Location!.Distance(userLocation) / 1000, 2)
            })
            .ToListAsync();

        return stations;
    }

    public async Task<List<NearbyStationDto>> SearchStationsAsync(string query, int limit)
    {
        var normalizedQuery = query.Trim().ToLower();

        var stations = await _dbContext.ChargingStations
            .AsNoTracking()
            .Where(s => s.IsEnabled && !s.IsDeleted)
            .Where(s => s.Name.ToLower().Contains(normalizedQuery)
                     || s.Address.ToLower().Contains(normalizedQuery)
                     || s.StationCode.ToLower().Contains(normalizedQuery))
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
                Distance = 0
            })
            .ToListAsync();

        return stations;
    }

    public async Task<StationDetailDto?> GetStationDetailAsync(Guid stationId)
    {
        var cacheKey = CacheKeys.StationDetail(stationId);

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

            // Build tariff detail for mobile display
            TariffDetailDto? tariffDetail = null;
            if (tariff != null)
            {
                var baseRate = tariff.BaseRatePerKwh;
                var taxRate = tariff.TaxRatePercent;
                var totalRate = Math.Round(baseRate * (1 + taxRate / 100), 0);
                var isTou = tariff.TariffType == Enums.TariffType.TimeOfUse;

                string? touSummary = null;
                if (isTou && tariff.OffPeakRatePerKwh.HasValue && tariff.PeakRatePerKwh.HasValue)
                {
                    var offPeak = Math.Round(tariff.OffPeakRatePerKwh.Value * (1 + taxRate / 100), 0);
                    var normal = Math.Round((tariff.NormalRatePerKwh ?? baseRate) * (1 + taxRate / 100), 0);
                    var peak = Math.Round(tariff.PeakRatePerKwh.Value * (1 + taxRate / 100), 0);
                    touSummary = $"Thấp điểm: {offPeak:N0}đ · Bình thường: {normal:N0}đ · Cao điểm: {peak:N0}đ";
                }

                tariffDetail = new TariffDetailDto
                {
                    Name = tariff.Name,
                    Description = tariff.Description,
                    TariffType = tariff.TariffType.ToString(),
                    BaseRatePerKwh = baseRate,
                    TaxRatePercent = taxRate,
                    TotalRatePerKwh = totalRate,
                    OffPeakRatePerKwh = tariff.OffPeakRatePerKwh,
                    NormalRatePerKwh = tariff.NormalRatePerKwh,
                    PeakRatePerKwh = tariff.PeakRatePerKwh,
                    FormattedRate = $"{totalRate:N0}đ/kWh",
                    FormattedTouSummary = touSummary
                };
            }

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
                Tariff = tariffDetail,
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

    public async Task<StationDetailDto?> GetStationByCodeAsync(string stationCode)
    {
        var station = await _dbContext.ChargingStations
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StationCode == stationCode && !s.IsDeleted);

        if (station == null) return null;

        var detail = await GetStationDetailAsync(station.Id);

        if (detail != null)
        {
            _logger.LogInformation(
                "QR_SCAN: StationCode={StationCode}, StationId={StationId}, StationStatus={StationStatus}, " +
                "Connectors=[{Connectors}]",
                stationCode, station.Id, detail.Status,
                string.Join(", ", detail.Connectors.Select(c =>
                    $"#{c.ConnectorNumber}:{c.Status}(enabled={c.IsEnabled},id={c.Id})")));
        }

        return detail;
    }

    public async Task<List<ConnectorStatusDto>> GetConnectorStatusAsync(Guid stationId)
    {
        var cacheKey = CacheKeys.StationConnectors(stationId);

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
    public TariffDetailDto? Tariff { get; init; }
    public List<ConnectorStatusDto> Connectors { get; init; } = new();
}

/// <summary>
/// Thông tin bảng giá cho mobile app hiển thị trước khi sạc
/// </summary>
public record TariffDetailDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TariffType { get; init; } = "Flat"; // "Flat" | "TimeOfUse"
    public decimal BaseRatePerKwh { get; init; }
    public decimal TaxRatePercent { get; init; }
    public decimal TotalRatePerKwh { get; init; } // BaseRate + Tax
    public string Currency { get; init; } = "VND";

    // TOU rates (only when TariffType = "TimeOfUse")
    public decimal? OffPeakRatePerKwh { get; init; }   // Giờ thấp điểm (22:00-04:00)
    public decimal? NormalRatePerKwh { get; init; }     // Giờ bình thường
    public decimal? PeakRatePerKwh { get; init; }       // Giờ cao điểm (09:30-11:30, 17:00-20:00)

    // Display helpers for mobile
    public string FormattedRate { get; init; } = string.Empty;  // "4.000đ/kWh"
    public string? FormattedTouSummary { get; init; }           // "Thấp điểm: 3.500đ · Bình thường: 4.000đ · Cao điểm: 5.500đ"
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
