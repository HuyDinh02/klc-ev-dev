using System;
using System.Reflection;
using System.Threading.Tasks;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Services;
using KLC.Sessions;
using KLC.Stations;
using KLC.TestDoubles;
using KLC.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace KLC.EntityFrameworkCore.Ocpp;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class WalletBalanceMonitorServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly FakeOcppRemoteCommandService _remoteCommandService;

    public WalletBalanceMonitorServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _serviceProvider = ServiceProvider;
        _remoteCommandService = GetRequiredService<FakeOcppRemoteCommandService>();
        _remoteCommandService.Reset();
    }

    [Fact]
    public async Task Monitor_Should_Look_Up_AppUser_By_IdentityUserId()
    {
        var stationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var appUserId = Guid.NewGuid();
        var identityUserId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "KC-WALLET-001", "Wallet Test", "123 Wallet St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var user = new AppUser(appUserId, identityUserId, "Wallet User", "0900000001");
            user.AddToWallet(5_000m);
            await _dbContext.AppUsers.AddAsync(user);

            var session = new ChargingSession(sessionId, identityUserId, stationId, 1, ratePerKwh: 3500m);
            session.MarkStarting();
            session.RecordStart(7890, 0);
            session.UpdateTotalEnergy(3m);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        var service = new WalletBalanceMonitorService(_serviceProvider, NullLogger<WalletBalanceMonitorService>.Instance);
        var method = typeof(WalletBalanceMonitorService)
            .GetMethod("CheckActiveSessionsAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        method.ShouldNotBeNull();
        var task = (Task)method!.Invoke(service, null)!;
        await task;

        _remoteCommandService.RemoteStopCalls.Count.ShouldBe(1);
        _remoteCommandService.RemoteStopCalls[0].TransactionId.ShouldBe(7890);
    }
}
