using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Stations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace KLC.BffServices;

/// <summary>
/// Tests for FavoriteBffService cache layer behavior.
/// Uses NSubstitute mocks for ICacheService to verify cache interactions.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class FavoriteBffServiceCacheTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly FavoriteBffService _service;

    public FavoriteBffServiceCacheTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = Substitute.For<ICacheService>();
        var logger = Substitute.For<ILogger<FavoriteBffService>>();
        _service = new FavoriteBffService(_dbContext, _cache, logger);
    }

    [Fact]
    public async Task GetFavorites_Should_Return_Cached_Result_On_Cache_Hit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedFavorites = new List<FavoriteStationDto>
        {
            new()
            {
                FavoriteId = Guid.NewGuid(),
                StationId = Guid.NewGuid(),
                Name = "Cached Station",
                Address = "123 Cached St",
                Latitude = 21.0,
                Longitude = 105.8,
                Status = StationStatus.Available,
                AvailableConnectors = 2,
                TotalConnectors = 4,
                AddedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        var cacheKey = $"user:{userId}:favorites";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<FavoriteStationDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedFavorites);

        // Act
        var result = await _service.GetFavoritesAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Cached Station");
        result[0].Status.ShouldBe(StationStatus.Available);
        result[0].AvailableConnectors.ShouldBe(2);

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<FavoriteStationDto>>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetFavorites_Should_Query_DB_On_Cache_Miss()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "FAV-001", "Favorite Station", "456 Fav St", 21.0, 105.8);
            station.UpdateStatus(StationStatus.Available);
            var connector = station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            connector.UpdateStatus(ConnectorStatus.Available);
            await _dbContext.ChargingStations.AddAsync(station);

            var favorite = new FavoriteStation(Guid.NewGuid(), userId, stationId);
            await _dbContext.FavoriteStations.AddAsync(favorite);
            await _dbContext.SaveChangesAsync();
        });

        var cacheKey = $"user:{userId}:favorites";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<FavoriteStationDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<List<FavoriteStationDto>>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetFavoritesAsync(userId);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(1);
            result[0].StationId.ShouldBe(stationId);
            result[0].Name.ShouldBe("Favorite Station");
        });
    }

    [Fact]
    public async Task AddFavorite_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "FAV-ADD", "Add Favorite Station", "789 Add St", 21.0, 105.8);
            station.UpdateStatus(StationStatus.Available);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.AddFavoriteAsync(userId, stationId);

            // Assert
            result.ShouldNotBeNull();
            result.StationId.ShouldBe(stationId);
            result.Name.ShouldBe("Add Favorite Station");
        });

        // Verify cache invalidation
        await _cache.Received(1).RemoveAsync($"user:{userId}:favorites");
    }

    [Fact]
    public async Task RemoveFavorite_Should_Invalidate_Cache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "FAV-REM", "Remove Favorite Station", "111 Rem St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var favorite = new FavoriteStation(Guid.NewGuid(), userId, stationId);
            await _dbContext.FavoriteStations.AddAsync(favorite);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            await _service.RemoveFavoriteAsync(userId, stationId);
        });

        // Assert - cache invalidated
        await _cache.Received(1).RemoveAsync($"user:{userId}:favorites");
    }

    [Fact]
    public async Task GetFavorites_Should_Use_Correct_Cache_Key_Format()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedCacheKey = $"user:{userId}:favorites";

        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<List<FavoriteStationDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(new List<FavoriteStationDto>());

        // Act
        await _service.GetFavoritesAsync(userId);

        // Assert
        await _cache.Received(1).GetOrSetAsync(
            expectedCacheKey,
            Arg.Any<Func<Task<List<FavoriteStationDto>>>>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetFavorites_Should_Return_Empty_List_When_No_Favorites()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cacheKey = $"user:{userId}:favorites";

        // Cache miss - pass through to factory
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<FavoriteStationDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<List<FavoriteStationDto>>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetFavoritesAsync(userId);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(0);
        });
    }
}
