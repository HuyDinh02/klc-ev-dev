using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Hubs;
using KLC.Ocpp;
using KLC.PowerSharing;
using KLC.Stations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Xunit;

namespace KLC.EntityFrameworkCore.Ocpp;

public class PowerBalancingServiceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRepository<PowerSharingGroup, Guid> _groupRepo;
    private readonly IRepository<ChargingStation, Guid> _stationRepo;
    private readonly IRepository<Connector, Guid> _connectorRepo;
    private readonly IPowerSharingService _powerSharingService;
    private readonly IOcppRemoteCommandService _remoteCommandService;
    private readonly IMonitoringNotifier _notifier;
    private readonly OcppConnectionManager _connectionManager;
    private readonly PowerBalancingService _service;

    public PowerBalancingServiceTests()
    {
        _groupRepo = Substitute.For<IRepository<PowerSharingGroup, Guid>>();
        _stationRepo = Substitute.For<IRepository<ChargingStation, Guid>>();
        _connectorRepo = Substitute.For<IRepository<Connector, Guid>>();
        _powerSharingService = Substitute.For<IPowerSharingService>();
        _remoteCommandService = Substitute.For<IOcppRemoteCommandService>();
        _notifier = Substitute.For<IMonitoringNotifier>();
        _connectionManager = new OcppConnectionManager(NullLogger<OcppConnectionManager>.Instance);

        var uowManager = Substitute.For<IUnitOfWorkManager>();
        var uow = Substitute.For<IUnitOfWork>();
        uowManager.Begin(Arg.Any<bool>()).Returns(uow);

        var services = new ServiceCollection();
        services.AddSingleton(_groupRepo);
        services.AddSingleton(_stationRepo);
        services.AddSingleton(_connectorRepo);
        services.AddSingleton(_powerSharingService);
        services.AddSingleton(_remoteCommandService);
        services.AddSingleton(uowManager);

        _serviceProvider = services.BuildServiceProvider();

        _service = new PowerBalancingService(
            _serviceProvider,
            _connectionManager,
            _notifier,
            NullLogger<PowerBalancingService>.Instance);
    }

    /// <summary>
    /// Runs a single rebalance cycle deterministically by triggering then
    /// waiting for the notifier or repo call that signals completion.
    /// </summary>
    private async Task RunOneCycleAsync(TaskCompletionSource completionSignal, int timeoutMs = 5000)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
        _service.TriggerRebalance();
        try
        {
            await _service.StartAsync(cts.Token);
            // Wait for the signal that the cycle completed, or timeout
            var completed = await Task.WhenAny(completionSignal.Task, Task.Delay(timeoutMs, cts.Token));
            if (completed != completionSignal.Task)
                throw new TimeoutException("Balancing cycle did not complete in time");
        }
        catch (OperationCanceledException) { }
        finally { await _service.StopAsync(CancellationToken.None); }
    }

    private static Connector CreateConnector(Guid id, Guid stationId, int connectorNumber, decimal maxPowerKw = 100m)
    {
        return new Connector(id, stationId, connectorNumber, ConnectorType.CCS2, maxPowerKw);
    }

    [Fact]
    public async Task Should_Skip_When_No_Active_Groups()
    {
        var done = new TaskCompletionSource();

        _groupRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PowerSharingGroup, bool>>>())
            .Returns(ci =>
            {
                done.TrySetResult();
                return new List<PowerSharingGroup>();
            });

        await RunOneCycleAsync(done);

        await _remoteCommandService.DidNotReceive()
            .SendSetChargingProfileAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ChargingProfilePayload>());
    }

    [Fact]
    public async Task Should_Dispatch_Profiles_For_Active_Group()
    {
        // Arrange
        var done = new TaskCompletionSource();
        var groupId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var stationCode = "CS-001";

        var group = new PowerSharingGroup(groupId, "Test Group", 100m, PowerSharingMode.Link);
        _groupRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PowerSharingGroup, bool>>>())
            .Returns(new List<PowerSharingGroup> { group });

        var allocations = new List<PowerAllocation>
        {
            new(connectorId, stationId, 50m, 100m)
        };
        _powerSharingService.RecalculateAllocationsAsync(groupId).Returns(allocations);

        var station = new ChargingStation(stationId, stationCode, "Test Station", "Address", 0, 0);
        _stationRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ChargingStation, bool>>>())
            .Returns(new List<ChargingStation> { station });

        var connector = CreateConnector(connectorId, stationId, 1);
        _connectorRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Connector, bool>>>())
            .Returns(new List<Connector> { connector });

        // Simulate station connected
        var ws = Substitute.For<System.Net.WebSockets.WebSocket>();
        ws.State.Returns(System.Net.WebSockets.WebSocketState.Open);
        _connectionManager.AddConnection(stationCode, ws, OcppProtocolVersion.Ocpp16J);

        _remoteCommandService.SendSetChargingProfileAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ChargingProfilePayload>())
            .Returns(new RemoteCommandResult(true));

        // Signal completion when notifier is called (last action in the cycle)
        _notifier.NotifyPowerAllocationChangedAsync(Arg.Any<PowerAllocationUpdate>())
            .Returns(ci => { done.TrySetResult(); return Task.CompletedTask; });

        // Act
        await RunOneCycleAsync(done);

        // Assert
        await _remoteCommandService.Received()
            .SendSetChargingProfileAsync(
                stationCode,
                1,
                Arg.Is<ChargingProfilePayload>(p =>
                    p.ChargingSchedule.ChargingRateUnit == "W" &&
                    p.ChargingSchedule.ChargingSchedulePeriod[0].Limit == 50000m));
    }

    [Fact]
    public async Task Should_Send_SignalR_Notification_After_Dispatch()
    {
        // Arrange
        var done = new TaskCompletionSource();
        var groupId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var stationCode = "CS-002";

        var group = new PowerSharingGroup(groupId, "Notify Group", 80m, PowerSharingMode.Link);
        _groupRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PowerSharingGroup, bool>>>())
            .Returns(new List<PowerSharingGroup> { group });

        _powerSharingService.RecalculateAllocationsAsync(groupId)
            .Returns(new List<PowerAllocation> { new(connectorId, stationId, 40m, 80m) });

        _stationRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ChargingStation, bool>>>())
            .Returns(new List<ChargingStation> { new(stationId, stationCode, "S2", "Addr", 0, 0) });

        var connector = CreateConnector(connectorId, stationId, 1);
        _connectorRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Connector, bool>>>())
            .Returns(new List<Connector> { connector });

        var ws = Substitute.For<System.Net.WebSockets.WebSocket>();
        ws.State.Returns(System.Net.WebSockets.WebSocketState.Open);
        _connectionManager.AddConnection(stationCode, ws, OcppProtocolVersion.Ocpp16J);

        _remoteCommandService.SendSetChargingProfileAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ChargingProfilePayload>())
            .Returns(new RemoteCommandResult(true));

        _notifier.NotifyPowerAllocationChangedAsync(Arg.Any<PowerAllocationUpdate>())
            .Returns(ci => { done.TrySetResult(); return Task.CompletedTask; });

        // Act
        await RunOneCycleAsync(done);

        // Assert
        await _notifier.Received().NotifyPowerAllocationChangedAsync(
            Arg.Is<PowerAllocationUpdate>(u =>
                u.GroupId == groupId &&
                u.GroupName == "Notify Group" &&
                u.TotalCapacityKw == 80m &&
                u.TotalAllocatedKw == 40m &&
                u.Allocations.Count == 1));
    }

    [Fact]
    public async Task Should_Skip_Disconnected_Stations()
    {
        // Arrange
        var done = new TaskCompletionSource();
        var groupId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();

        var group = new PowerSharingGroup(groupId, "Offline Group", 100m, PowerSharingMode.Link);
        _groupRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PowerSharingGroup, bool>>>())
            .Returns(new List<PowerSharingGroup> { group });

        _powerSharingService.RecalculateAllocationsAsync(groupId)
            .Returns(new List<PowerAllocation> { new(connectorId, stationId, 50m, 100m) });

        _stationRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ChargingStation, bool>>>())
            .Returns(new List<ChargingStation> { new(stationId, "CS-OFFLINE", "Offline", "Addr", 0, 0) });

        var connector = CreateConnector(connectorId, stationId, 1);
        _connectorRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Connector, bool>>>())
            .Returns(new List<Connector> { connector });

        // Station NOT connected to _connectionManager
        // Signal completion when notifier is called
        _notifier.NotifyPowerAllocationChangedAsync(Arg.Any<PowerAllocationUpdate>())
            .Returns(ci => { done.TrySetResult(); return Task.CompletedTask; });

        // Act
        await RunOneCycleAsync(done);

        // Assert - no profiles sent but SignalR notification still sent (with allocation info)
        await _remoteCommandService.DidNotReceive()
            .SendSetChargingProfileAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ChargingProfilePayload>());

        await _notifier.Received().NotifyPowerAllocationChangedAsync(
            Arg.Is<PowerAllocationUpdate>(u => u.ProfilesDispatched == 0));
    }

    [Fact]
    public void TriggerRebalance_Should_Not_Throw()
    {
        // Should be safe to call multiple times
        _service.TriggerRebalance();
        _service.TriggerRebalance();
        _service.TriggerRebalance();
    }
}
