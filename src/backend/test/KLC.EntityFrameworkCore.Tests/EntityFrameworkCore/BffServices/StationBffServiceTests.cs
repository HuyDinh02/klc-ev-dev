using System;
using System.Threading.Tasks;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Stations;
using KLC.Tariffs;
using KLC.TestDoubles;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class StationBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly StationBffService _service;

    public StationBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        var cache = new PassthroughCacheService();
        var logger = Substitute.For<ILogger<StationBffService>>();
        _service = new StationBffService(_dbContext, cache, logger);
    }

    [Fact]
    public async Task GetStationDetail_Should_Return_Station_With_Connectors()
    {
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "STN-001", "Station 1", "123 Test St", 21.0, 105.8);
            station.UpdateStatus(StationStatus.Online);
            var c1 = station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            c1.UpdateStatus(ConnectorStatus.Available);
            station.AddConnector(Guid.NewGuid(), 2, ConnectorType.CHAdeMO, 50);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetStationDetailAsync(stationId);

            result.ShouldNotBeNull();
            result!.Id.ShouldBe(stationId);
            result.StationCode.ShouldBe("STN-001");
            result.Name.ShouldBe("Station 1");
            result.Connectors.Count.ShouldBe(2);
        });
    }

    [Fact]
    public async Task GetStationDetail_Should_Return_Null_For_Unknown_Station()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetStationDetailAsync(Guid.NewGuid());
            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task GetStationDetail_Should_Include_Tariff_Rate()
    {
        var stationId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var tariff = new TariffPlan(tariffId, "Test Tariff", 3500, 10,
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));
            await _dbContext.TariffPlans.AddAsync(tariff);

            var station = new ChargingStation(stationId, "STN-002", "Station 2", "456 St", 21.0, 105.8);
            station.SetTariffPlan(tariffId);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetStationDetailAsync(stationId);

            result.ShouldNotBeNull();
            result!.RatePerKwh.ShouldBe(3500);
            result.TaxRatePercent.ShouldBe(10);
        });
    }

    [Fact]
    public async Task GetConnectorStatus_Should_Return_Connectors()
    {
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "STN-003", "Station 3", "789 St", 21.0, 105.8);
            var c1 = station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            c1.UpdateStatus(ConnectorStatus.Available);
            var c2 = station.AddConnector(Guid.NewGuid(), 2, ConnectorType.Type2, 22);
            c2.UpdateStatus(ConnectorStatus.Charging);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetConnectorStatusAsync(stationId);

            result.Count.ShouldBe(2);
            result[0].ConnectorNumber.ShouldBe(1);
            result[0].Status.ShouldBe(ConnectorStatus.Available);
            result[1].ConnectorNumber.ShouldBe(2);
            result[1].Status.ShouldBe(ConnectorStatus.Charging);
        });
    }

    [Fact]
    public async Task GetConnectorStatus_Should_Return_Empty_For_Unknown_Station()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.GetConnectorStatusAsync(Guid.NewGuid());
            result.ShouldBeEmpty();
        });
    }

}
