using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Stations;
using KLC.Tariffs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace KLC.BffServices;

/// <summary>
/// Tests for StationBffService cache layer behavior.
/// Uses NSubstitute mocks for ICacheService to verify cache interactions.
/// </summary>
[Collection(KLCTestConsts.CollectionDefinitionName)]
public class StationBffServiceCacheTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly StationBffService _service;

    public StationBffServiceCacheTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _cache = Substitute.For<ICacheService>();
        var logger = Substitute.For<ILogger<StationBffService>>();
        _service = new StationBffService(_dbContext, _cache, logger);
    }

    [Fact]
    public async Task GetStationDetail_Should_Return_Cached_Result_On_Cache_Hit()
    {
        // Arrange
        var stationId = Guid.NewGuid();
        var cachedDetail = new StationDetailDto
        {
            Id = stationId,
            StationCode = "CACHED-001",
            Name = "Cached Station",
            Address = "123 Cache St",
            Latitude = 21.0,
            Longitude = 105.8,
            Status = StationStatus.Online,
            IsEnabled = true,
            RatePerKwh = 3500,
            Connectors = new List<ConnectorStatusDto>
            {
                new() { Id = Guid.NewGuid(), ConnectorNumber = 1, Type = ConnectorType.CCS2, Status = ConnectorStatus.Available, MaxPowerKw = 50, IsEnabled = true }
            }
        };

        var cacheKey = $"station:{stationId}:detail";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<StationDetailDto?>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedDetail);

        // Act
        var result = await _service.GetStationDetailAsync(stationId);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(stationId);
        result.StationCode.ShouldBe("CACHED-001");
        result.Name.ShouldBe("Cached Station");
        result.Connectors.Count.ShouldBe(1);

        // Verify cache was called with correct key
        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<StationDetailDto?>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetStationDetail_Should_Query_DB_On_Cache_Miss_And_Cache_Result()
    {
        // Arrange
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "DB-001", "DB Station", "456 DB St", 21.0, 105.8);
            station.UpdateStatus(StationStatus.Online);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        // Configure cache to pass through to factory (simulating cache miss)
        var cacheKey = $"station:{stationId}:detail";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<StationDetailDto?>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<StationDetailDto?>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetStationDetailAsync(stationId);

            // Assert - data comes from DB
            result.ShouldNotBeNull();
            result!.Id.ShouldBe(stationId);
            result.StationCode.ShouldBe("DB-001");
            result.Name.ShouldBe("DB Station");
            result.Connectors.Count.ShouldBe(1);
        });

        // Verify cache was invoked (GetOrSetAsync handles both get and set)
        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<StationDetailDto?>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetStationDetail_Should_Return_Null_When_Station_Not_Found()
    {
        // Arrange
        var stationId = Guid.NewGuid();
        var cacheKey = $"station:{stationId}:detail";

        // Cache miss - pass through to factory
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<StationDetailDto?>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<StationDetailDto?>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetStationDetailAsync(stationId);

            // Assert
            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task GetStationDetail_Should_Include_Connectors_In_Cached_Detail()
    {
        // Arrange
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "STN-CONN", "Multi-Connector Station", "789 St", 21.0, 105.8);
            var c1 = station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            c1.UpdateStatus(ConnectorStatus.Available);
            var c2 = station.AddConnector(Guid.NewGuid(), 2, ConnectorType.CHAdeMO, 50);
            c2.UpdateStatus(ConnectorStatus.Charging);
            var c3 = station.AddConnector(Guid.NewGuid(), 3, ConnectorType.Type2, 22);
            c3.UpdateStatus(ConnectorStatus.Available);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        var cacheKey = $"station:{stationId}:detail";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<StationDetailDto?>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<StationDetailDto?>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetStationDetailAsync(stationId);

            // Assert
            result.ShouldNotBeNull();
            result!.Connectors.Count.ShouldBe(3);
            result.Connectors[0].ConnectorNumber.ShouldBe(1);
            result.Connectors[1].ConnectorNumber.ShouldBe(2);
            result.Connectors[2].ConnectorNumber.ShouldBe(3);
        });
    }

    [Fact]
    public async Task GetConnectorStatus_Should_Return_Cached_Connectors_On_Hit()
    {
        // Arrange
        var stationId = Guid.NewGuid();
        var cachedConnectors = new List<ConnectorStatusDto>
        {
            new() { Id = Guid.NewGuid(), ConnectorNumber = 1, Type = ConnectorType.CCS2, Status = ConnectorStatus.Available, MaxPowerKw = 50, IsEnabled = true },
            new() { Id = Guid.NewGuid(), ConnectorNumber = 2, Type = ConnectorType.CHAdeMO, Status = ConnectorStatus.Charging, MaxPowerKw = 50, IsEnabled = true }
        };

        var cacheKey = $"station:{stationId}:connectors";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<ConnectorStatusDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(cachedConnectors);

        // Act
        var result = await _service.GetConnectorStatusAsync(stationId);

        // Assert
        result.Count.ShouldBe(2);
        result[0].Status.ShouldBe(ConnectorStatus.Available);
        result[1].Status.ShouldBe(ConnectorStatus.Charging);

        await _cache.Received(1).GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<ConnectorStatusDto>>>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task GetConnectorStatus_Should_Query_DB_On_Cache_Miss()
    {
        // Arrange
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "STN-MISS", "Cache Miss Station", "999 St", 21.0, 105.8);
            var c1 = station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            c1.UpdateStatus(ConnectorStatus.Available);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        var cacheKey = $"station:{stationId}:connectors";
        _cache.GetOrSetAsync(cacheKey, Arg.Any<Func<Task<List<ConnectorStatusDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<Task<List<ConnectorStatusDto>>>>(1);
                return factory();
            });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetConnectorStatusAsync(stationId);

            // Assert
            result.Count.ShouldBe(1);
            result[0].ConnectorNumber.ShouldBe(1);
            result[0].Type.ShouldBe(ConnectorType.CCS2);
        });
    }

    [Fact]
    public async Task SearchStations_Should_Bypass_Cache()
    {
        // Arrange - search does not use cache, queries DB directly
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "STN-SEARCH", "Searchable Station", "123 Search Ave", 21.0, 105.8);
            station.Enable();
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        // Act
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.SearchStationsAsync("Searchable", 10);

            // Assert
            result.ShouldNotBeEmpty();
            result[0].Name.ShouldBe("Searchable Station");
        });

        // Verify cache was NOT called for search
        await _cache.DidNotReceive().GetOrSetAsync(
            Arg.Any<string>(),
            Arg.Any<Func<Task<List<NearbyStationDto>>>>(),
            Arg.Any<TimeSpan?>());
        await _cache.DidNotReceive().GetAsync<List<NearbyStationDto>>(Arg.Any<string>());
    }

    [Fact]
    public async Task GetConnectorStatus_Should_Use_Correct_Cache_Key_Format()
    {
        // Arrange
        var stationId = Guid.NewGuid();
        var expectedCacheKey = $"station:{stationId}:connectors";

        _cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<Task<List<ConnectorStatusDto>>>>(), Arg.Any<TimeSpan?>())
            .Returns(new List<ConnectorStatusDto>());

        // Act
        await _service.GetConnectorStatusAsync(stationId);

        // Assert - verify exact cache key format
        await _cache.Received(1).GetOrSetAsync(
            expectedCacheKey,
            Arg.Any<Func<Task<List<ConnectorStatusDto>>>>(),
            Arg.Any<TimeSpan?>());
    }
}
