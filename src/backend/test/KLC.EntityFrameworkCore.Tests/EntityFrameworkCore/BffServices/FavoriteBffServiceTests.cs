using System;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Stations;
using KLC.TestDoubles;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class FavoriteBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly FavoriteBffService _service;

    public FavoriteBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        var cache = new PassthroughCacheService();
        var logger = Substitute.For<ILogger<FavoriteBffService>>();
        _service = new FavoriteBffService(_dbContext, cache, logger);
    }

    [Fact]
    public async Task AddFavorite_Should_Succeed()
    {
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "FAV-001", "Fav Test", "123 St", 21.0, 105.8);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.AddFavoriteAsync(userId, stationId);

            result.ShouldNotBeNull();
            result.StationId.ShouldBe(stationId);
            result.Name.ShouldBe("Fav Test");
        });
    }

    [Fact]
    public async Task AddFavorite_Should_Throw_When_Station_Not_Found()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ex = await Should.ThrowAsync<BusinessException>(
                () => _service.AddFavoriteAsync(Guid.NewGuid(), Guid.NewGuid()));
            ex.Code.ShouldBe(KLCDomainErrorCodes.Station.NotFound);
        });
    }

    [Fact]
    public async Task AddFavorite_Should_Throw_When_Already_Favorited()
    {
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "FAV-002", "Fav Test 2", "456 St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var fav = new FavoriteStation(Guid.NewGuid(), userId, stationId);
            await _dbContext.FavoriteStations.AddAsync(fav);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var ex = await Should.ThrowAsync<BusinessException>(
                () => _service.AddFavoriteAsync(userId, stationId));
            ex.Code.ShouldBe(KLCDomainErrorCodes.Favorite.AlreadyFavorited);
        });
    }

    [Fact]
    public async Task GetFavorites_Should_Return_User_Favorites()
    {
        var userId = Guid.NewGuid();
        var stationId1 = Guid.NewGuid();
        var stationId2 = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station1 = new ChargingStation(stationId1, "FAV-003", "Station A", "111 St", 21.0, 105.8);
            var station2 = new ChargingStation(stationId2, "FAV-004", "Station B", "222 St", 21.1, 105.9);
            await _dbContext.ChargingStations.AddRangeAsync(station1, station2);

            await _dbContext.FavoriteStations.AddRangeAsync(
                new FavoriteStation(Guid.NewGuid(), userId, stationId1),
                new FavoriteStation(Guid.NewGuid(), userId, stationId2));
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetFavoritesAsync(userId);

            result.Count.ShouldBe(2);
        });
    }

    [Fact]
    public async Task GetFavorites_Should_Return_Empty_When_No_Favorites()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetFavoritesAsync(Guid.NewGuid());
            result.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task RemoveFavorite_Should_Succeed()
    {
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "FAV-005", "Fav Remove", "333 St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var fav = new FavoriteStation(Guid.NewGuid(), userId, stationId);
            await _dbContext.FavoriteStations.AddAsync(fav);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _service.RemoveFavoriteAsync(userId, stationId);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetFavoritesAsync(userId);
            result.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task RemoveFavorite_Should_Throw_When_Not_Favorited()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var ex = await Should.ThrowAsync<BusinessException>(
                () => _service.RemoveFavoriteAsync(Guid.NewGuid(), Guid.NewGuid()));
            ex.Code.ShouldBe(KLCDomainErrorCodes.Favorite.NotFavorited);
        });
    }

}
