using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Ocpp;
using KLC.Sessions;
using KLC.Stations;
using KLC.TestDoubles;
using KLC.Vehicles;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.EntityFrameworkCore.Applications;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCoreSessionAppServiceReliabilityTests : KLCEntityFrameworkCoreTestBase
{
    private static readonly Guid CurrentUserId = Guid.Parse("2e701e62-0953-4dd3-910b-dc6cc93ccb0d");

    private readonly KLCDbContext _dbContext;
    private readonly ISessionAppService _sessionAppService;
    private readonly FakeOcppRemoteCommandService _remoteCommandService;

    public EfCoreSessionAppServiceReliabilityTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _sessionAppService = GetRequiredService<ISessionAppService>();
        _remoteCommandService = GetRequiredService<FakeOcppRemoteCommandService>();
        _remoteCommandService.Reset();
    }

    [Fact]
    public async Task StartAsync_Should_Block_When_User_Already_Has_Starting_Session()
    {
        var stationId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await SeedStartScenarioAsync(stationId, vehicleId);

        await WithUnitOfWorkAsync(async () =>
        {
            var existingSession = new ChargingSession(Guid.NewGuid(), CurrentUserId, stationId, 1, vehicleId);
            existingSession.MarkStarting();
            await _dbContext.ChargingSessions.AddAsync(existingSession);
            await _dbContext.SaveChangesAsync();
        });

        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _sessionAppService.StartAsync(new StartSessionDto
            {
                StationId = stationId,
                ConnectorNumber = 1,
                VehicleId = vehicleId
            });
        });

        ex.Code.ShouldBe(KLCDomainErrorCodes.Session.AlreadyActive);
    }

    [Fact]
    public async Task StartAsync_Should_Set_Starting_When_Remote_Start_Is_Accepted()
    {
        var stationId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await SeedStartScenarioAsync(stationId, vehicleId);
        _remoteCommandService.RemoteStartResult = new RemoteCommandResult(true);

        var result = await _sessionAppService.StartAsync(new StartSessionDto
        {
            StationId = stationId,
            ConnectorNumber = 1,
            VehicleId = vehicleId
        });

        result.Status.ShouldBe(SessionStatus.Starting);
        _remoteCommandService.RemoteStartCalls.Count.ShouldBe(1);

        await WithUnitOfWorkAsync(async () =>
        {
            var session = await _dbContext.ChargingSessions.FirstAsync(s => s.Id == result.Id);
            session.Status.ShouldBe(SessionStatus.Starting);
            session.UserId.ShouldBe(CurrentUserId);
        });
    }

    [Fact]
    public async Task StartAsync_Should_Mark_Session_Failed_When_Remote_Start_Is_Rejected()
    {
        var stationId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        await SeedStartScenarioAsync(stationId, vehicleId);
        _remoteCommandService.RemoteStartResult = new RemoteCommandResult(false, "Station not connected");

        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _sessionAppService.StartAsync(new StartSessionDto
            {
                StationId = stationId,
                ConnectorNumber = 1,
                VehicleId = vehicleId
            });
        });

        ex.Code.ShouldBe(KLCDomainErrorCodes.Session.StartCommandFailed);

        await WithUnitOfWorkAsync(async () =>
        {
            var session = await _dbContext.ChargingSessions
                .Where(s => s.StationId == stationId && s.UserId == CurrentUserId)
                .OrderByDescending(s => s.CreationTime)
                .FirstAsync();

            session.Status.ShouldBe(SessionStatus.Failed);
            session.StopReason.ShouldBe("Station not connected");
        });
    }

    [Fact]
    public async Task StopAsync_Should_Keep_Session_Active_When_Remote_Stop_Is_Rejected()
    {
        var stationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, $"KC-STOP-{stationId:N}", "Stop Test", "123 Stop St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(sessionId, CurrentUserId, stationId, 1);
            session.MarkStarting();
            session.RecordStart(4321, 0);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        _remoteCommandService.RemoteStopResult = new RemoteCommandResult(false, "Command timed out");

        var ex = await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _sessionAppService.StopAsync(sessionId);
        });

        ex.Code.ShouldBe(KLCDomainErrorCodes.Session.StopCommandFailed);

        await WithUnitOfWorkAsync(async () =>
        {
            var session = await _dbContext.ChargingSessions.FirstAsync(s => s.Id == sessionId);
            session.Status.ShouldBe(SessionStatus.InProgress);
        });
    }

    private async Task SeedStartScenarioAsync(Guid stationId, Guid vehicleId)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, $"KC-START-{stationId:N}", "Start Test", "123 Start St", 21.0, 105.8);
            var connector = station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            connector.UpdateStatus(ConnectorStatus.Available);

            var vehicle = new Vehicle(vehicleId, CurrentUserId, "Tesla", "Model 3", "30A12345", 75, ConnectorType.CCS2);
            vehicle.SetDetails(null, 2024, null);

            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.Vehicles.AddAsync(vehicle);
            await _dbContext.SaveChangesAsync();
        });
    }
}
