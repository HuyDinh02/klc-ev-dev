using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Stations;
using NSubstitute;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace KLC.Fleets;

public class FleetChargingPolicyServiceTests
{
    private readonly IRepository<FleetVehicle, Guid> _fleetVehicleRepo;
    private readonly IRepository<Fleet, Guid> _fleetRepo;
    private readonly IRepository<FleetChargingSchedule, Guid> _scheduleRepo;
    private readonly IRepository<FleetAllowedStation, Guid> _allowedStationRepo;
    private readonly IRepository<ChargingStation, Guid> _stationRepo;
    private readonly FleetChargingPolicyService _sut;

    public FleetChargingPolicyServiceTests()
    {
        _fleetVehicleRepo = Substitute.For<IRepository<FleetVehicle, Guid>>();
        _fleetRepo = Substitute.For<IRepository<Fleet, Guid>>();
        _scheduleRepo = Substitute.For<IRepository<FleetChargingSchedule, Guid>>();
        _allowedStationRepo = Substitute.For<IRepository<FleetAllowedStation, Guid>>();
        _stationRepo = Substitute.For<IRepository<ChargingStation, Guid>>();

        _sut = new FleetChargingPolicyService(
            _fleetVehicleRepo, _fleetRepo, _scheduleRepo, _allowedStationRepo, _stationRepo);
    }

    [Fact]
    public async Task Should_Allow_Non_Fleet_Vehicle()
    {
        SetupFleetVehicle(null);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeTrue();
        result.DenialReason.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Allow_When_Fleet_Not_Found()
    {
        var fv = CreateFleetVehicle(Guid.NewGuid(), Guid.NewGuid());
        SetupFleetVehicle(fv);
        SetupFleet(null);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Deny_When_Fleet_Inactive()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        fleet.Deactivate();

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        SetupFleetVehicle(fv);
        SetupFleet(fleet);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeFalse();
        result.DenialReason.ShouldBe(KLCDomainErrorCodes.Fleet.ChargingDenied);
    }

    [Fact]
    public async Task Should_Deny_When_Daily_Limit_Exceeded()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid(), dailyLimit: 50m);
        fv.RecordEnergy(60m);

        SetupFleetVehicle(fv);
        SetupFleet(fleet);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeFalse();
        result.DenialReason.ShouldBe(KLCDomainErrorCodes.Fleet.DailyLimitExceeded);
    }

    [Fact]
    public async Task Should_Deny_When_Budget_Exceeded()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid(),
            maxMonthlyBudgetVnd: 1_000_000m);
        fleet.RecordSpending(1_500_000m);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        SetupFleetVehicle(fv);
        SetupFleet(fleet);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeFalse();
        result.DenialReason.ShouldBe(KLCDomainErrorCodes.Fleet.BudgetExceeded);
    }

    [Fact]
    public async Task Should_Allow_AnytimeAnywhere_Policy()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        fleet.SetChargingPolicy(ChargingPolicyType.AnytimeAnywhere);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        SetupFleetVehicle(fv);
        SetupFleet(fleet);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Allow_DailyEnergyLimit_When_Under_Limit()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        fleet.SetChargingPolicy(ChargingPolicyType.DailyEnergyLimit);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid(), dailyLimit: 100m);
        fv.RecordEnergy(30m);

        SetupFleetVehicle(fv);
        SetupFleet(fleet);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Deny_ScheduledOnly_When_No_Schedules()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        fleet.SetChargingPolicy(ChargingPolicyType.ScheduledOnly);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        SetupFleetVehicle(fv);
        SetupFleet(fleet);
        SetupSchedules(new List<FleetChargingSchedule>());

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeFalse();
        result.DenialReason.ShouldBe(KLCDomainErrorCodes.Fleet.OutsideSchedule);
    }

    [Fact]
    public async Task Should_Allow_ScheduledOnly_When_Within_Schedule()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        fleet.SetChargingPolicy(ChargingPolicyType.ScheduledOnly);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        var now = DateTime.UtcNow;

        var schedule = new FleetChargingSchedule(
            Guid.NewGuid(), fleet.Id,
            (int)now.DayOfWeek,
            now.TimeOfDay.Add(TimeSpan.FromMinutes(-30)),
            now.TimeOfDay.Add(TimeSpan.FromMinutes(30)));

        SetupFleetVehicle(fv);
        SetupFleet(fleet);
        SetupSchedules(new List<FleetChargingSchedule> { schedule });

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Deny_ApprovedStationsOnly_When_Station_Not_Found()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        fleet.SetChargingPolicy(ChargingPolicyType.ApprovedStationsOnly);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        SetupFleetVehicle(fv);
        SetupFleet(fleet);
        SetupStation(null);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeFalse();
        result.DenialReason.ShouldBe(KLCDomainErrorCodes.Fleet.StationNotAllowed);
    }

    [Fact]
    public async Task Should_Deny_ApprovedStationsOnly_When_Station_Has_No_Group()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        fleet.SetChargingPolicy(ChargingPolicyType.ApprovedStationsOnly);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        var station = new ChargingStation(Guid.NewGuid(), "KC-001", "Station", "Address", 21.0, 105.8);

        SetupFleetVehicle(fv);
        SetupFleet(fleet);
        SetupStation(station);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeFalse();
        result.DenialReason.ShouldBe(KLCDomainErrorCodes.Fleet.StationNotAllowed);
    }

    [Fact]
    public async Task Should_Deny_ApprovedStationsOnly_When_Group_Not_In_Allowed_List()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        fleet.SetChargingPolicy(ChargingPolicyType.ApprovedStationsOnly);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        var station = new ChargingStation(Guid.NewGuid(), "KC-001", "Station", "Address", 21.0, 105.8, Guid.NewGuid());

        SetupFleetVehicle(fv);
        SetupFleet(fleet);
        SetupStation(station);
        SetupAllowedStations(new List<FleetAllowedStation>());

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeFalse();
        result.DenialReason.ShouldBe(KLCDomainErrorCodes.Fleet.StationNotAllowed);
    }

    [Fact]
    public async Task Should_Allow_ApprovedStationsOnly_When_Group_Is_Allowed()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());
        fleet.SetChargingPolicy(ChargingPolicyType.ApprovedStationsOnly);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        var station = new ChargingStation(Guid.NewGuid(), "KC-001", "Station", "Address", 21.0, 105.8, Guid.NewGuid());
        var allowed = new FleetAllowedStation(Guid.NewGuid(), fleet.Id, station.StationGroupId!.Value);

        SetupFleetVehicle(fv);
        SetupFleet(fleet);
        SetupStation(station);
        SetupAllowedStations(new List<FleetAllowedStation> { allowed });

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Allow_No_Budget_Even_With_Spending()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid(),
            maxMonthlyBudgetVnd: 0);
        fleet.RecordSpending(999_999m);

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid());
        SetupFleetVehicle(fv);
        SetupFleet(fleet);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Allow_No_Daily_Limit()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());

        var fv = CreateFleetVehicle(fleet.Id, Guid.NewGuid(), dailyLimit: null);
        fv.RecordEnergy(999m);

        SetupFleetVehicle(fv);
        SetupFleet(fleet);

        var result = await _sut.ValidateChargingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Allowed.ShouldBeTrue();
    }

    // Mock setup helpers using ABP interface methods (FindAsync, GetListAsync)
    private void SetupFleetVehicle(FleetVehicle? value)
    {
        _fleetVehicleRepo.FindAsync(
            Arg.Any<Expression<Func<FleetVehicle, bool>>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(value);
    }

    private void SetupFleet(Fleet? value)
    {
        _fleetRepo.FindAsync(
            Arg.Any<Expression<Func<Fleet, bool>>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(value);
    }

    private void SetupSchedules(List<FleetChargingSchedule> value)
    {
        _scheduleRepo.GetListAsync(
            Arg.Any<Expression<Func<FleetChargingSchedule, bool>>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(value);
    }

    private void SetupStation(ChargingStation? value)
    {
        _stationRepo.FindAsync(
            Arg.Any<Expression<Func<ChargingStation, bool>>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(value);
    }

    private void SetupAllowedStations(List<FleetAllowedStation> value)
    {
        _allowedStationRepo.GetListAsync(
            Arg.Any<Expression<Func<FleetAllowedStation, bool>>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(value);
    }

    private static FleetVehicle CreateFleetVehicle(
        Guid fleetId, Guid vehicleId, decimal? dailyLimit = null)
    {
        return new FleetVehicle(
            Guid.NewGuid(), fleetId, vehicleId,
            dailyChargingLimitKwh: dailyLimit);
    }
}
