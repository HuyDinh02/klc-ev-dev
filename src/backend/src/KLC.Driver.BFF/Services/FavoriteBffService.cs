using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Stations;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;

namespace KLC.Driver.Services;

public interface IFavoriteBffService
{
    Task<List<FavoriteStationDto>> GetFavoritesAsync(Guid userId);
    Task<FavoriteStationDto> AddFavoriteAsync(Guid userId, Guid stationId);
    Task RemoveFavoriteAsync(Guid userId, Guid stationId);
}

public class FavoriteBffService : IFavoriteBffService
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<FavoriteBffService> _logger;

    public FavoriteBffService(
        KLCDbContext dbContext,
        ICacheService cache,
        ILogger<FavoriteBffService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<FavoriteStationDto>> GetFavoritesAsync(Guid userId)
    {
        var cacheKey = $"user:{userId}:favorites";

        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var favorites = await _dbContext.FavoriteStations
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Join(
                    _dbContext.ChargingStations
                        .AsNoTracking()
                        .Where(s => !s.IsDeleted)
                        .Include(s => s.Connectors.Where(c => c.IsEnabled && !c.IsDeleted)),
                    f => f.StationId,
                    s => s.Id,
                    (f, s) => new FavoriteStationDto
                    {
                        FavoriteId = f.Id,
                        StationId = s.Id,
                        Name = s.Name,
                        Address = s.Address,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        Status = s.Status,
                        AvailableConnectors = s.Connectors.Count(c => c.Status == ConnectorStatus.Available),
                        TotalConnectors = s.Connectors.Count(),
                        AddedAt = f.CreationTime
                    })
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();

            return favorites;
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<FavoriteStationDto> AddFavoriteAsync(Guid userId, Guid stationId)
    {
        // Check if station exists
        var station = await _dbContext.ChargingStations
            .AsNoTracking()
            .Include(s => s.Connectors.Where(c => c.IsEnabled && !c.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == stationId && !s.IsDeleted);

        if (station == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Station.NotFound);
        }

        // Check if already favorited
        var existing = await _dbContext.FavoriteStations
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.StationId == stationId);

        if (existing != null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Favorite.AlreadyFavorited);
        }

        var favorite = new FavoriteStation(Guid.NewGuid(), userId, stationId);
        await _dbContext.FavoriteStations.AddAsync(favorite);
        await _dbContext.SaveChangesAsync();

        // Invalidate cache
        await _cache.RemoveAsync($"user:{userId}:favorites");

        _logger.LogInformation(
            "User {UserId} added station {StationId} to favorites",
            userId, stationId);

        return new FavoriteStationDto
        {
            FavoriteId = favorite.Id,
            StationId = station.Id,
            Name = station.Name,
            Address = station.Address,
            Latitude = station.Latitude,
            Longitude = station.Longitude,
            Status = station.Status,
            AvailableConnectors = station.Connectors.Count(c => c.Status == ConnectorStatus.Available),
            TotalConnectors = station.Connectors.Count(),
            AddedAt = favorite.CreationTime
        };
    }

    public async Task RemoveFavoriteAsync(Guid userId, Guid stationId)
    {
        var favorite = await _dbContext.FavoriteStations
            .FirstOrDefaultAsync(f => f.UserId == userId && f.StationId == stationId);

        if (favorite == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.Favorite.NotFavorited);
        }

        _dbContext.FavoriteStations.Remove(favorite);
        await _dbContext.SaveChangesAsync();

        // Invalidate cache
        await _cache.RemoveAsync($"user:{userId}:favorites");

        _logger.LogInformation(
            "User {UserId} removed station {StationId} from favorites",
            userId, stationId);
    }
}

// DTOs
public record FavoriteStationDto
{
    public Guid FavoriteId { get; init; }
    public Guid StationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public StationStatus Status { get; init; }
    public int AvailableConnectors { get; init; }
    public int TotalConnectors { get; init; }
    public DateTime AddedAt { get; init; }
}
