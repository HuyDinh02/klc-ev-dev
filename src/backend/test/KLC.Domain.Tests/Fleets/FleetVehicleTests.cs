using System;
using KLC.Enums;
using Shouldly;
using Xunit;

namespace KLC.Fleets;

public class FleetVehicleTests
{
    [Fact]
    public void Create_Via_Fleet_Should_Set_Properties()
    {
        var fleet = CreateFleet();
        var vehicleId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var fv = fleet.AddVehicle(vehicleId, driverId, 100m);

        fv.FleetId.ShouldBe(fleet.Id);
        fv.VehicleId.ShouldBe(vehicleId);
        fv.DriverUserId.ShouldBe(driverId);
        fv.DailyChargingLimitKwh.ShouldBe(100m);
        fv.CurrentDayEnergyKwh.ShouldBe(0m);
        fv.CurrentMonthEnergyKwh.ShouldBe(0m);
        fv.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_Via_Fleet_Should_Default_NullDriver_And_NoLimit()
    {
        var fleet = CreateFleet();
        var vehicleId = Guid.NewGuid();

        var fv = fleet.AddVehicle(vehicleId);

        fv.DriverUserId.ShouldBeNull();
        fv.DailyChargingLimitKwh.ShouldBeNull();
    }

    [Fact]
    public void AssignDriver_Should_Update()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid());
        var driverId = Guid.NewGuid();

        fv.AssignDriver(driverId);

        fv.DriverUserId.ShouldBe(driverId);
    }

    [Fact]
    public void AssignDriver_Should_Allow_Null_To_Unassign()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid(), Guid.NewGuid());

        fv.AssignDriver(null);

        fv.DriverUserId.ShouldBeNull();
    }

    [Fact]
    public void SetDailyLimit_Should_Update()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid());

        fv.SetDailyLimit(75.5m);

        fv.DailyChargingLimitKwh.ShouldBe(75.5m);
    }

    [Fact]
    public void SetDailyLimit_Should_Allow_Null_For_Unlimited()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid(), dailyLimitKwh: 50m);

        fv.SetDailyLimit(null);

        fv.DailyChargingLimitKwh.ShouldBeNull();
    }

    [Fact]
    public void RecordEnergy_Should_Accumulate_Daily_And_Monthly()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid());

        fv.RecordEnergy(10m);
        fv.RecordEnergy(15m);

        fv.CurrentDayEnergyKwh.ShouldBe(25m);
        fv.CurrentMonthEnergyKwh.ShouldBe(25m);
    }

    [Fact]
    public void ResetDailyEnergy_Should_Clear_Daily_Only()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid());

        fv.RecordEnergy(30m);
        fv.ResetDailyEnergy();

        fv.CurrentDayEnergyKwh.ShouldBe(0m);
        fv.CurrentMonthEnergyKwh.ShouldBe(30m);
    }

    [Fact]
    public void ResetMonthlyEnergy_Should_Clear_Both()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid());

        fv.RecordEnergy(50m);
        fv.ResetMonthlyEnergy();

        // ResetMonthlyEnergy only clears monthly; daily is separate
        fv.CurrentMonthEnergyKwh.ShouldBe(0m);
        // Daily is not cleared by ResetMonthlyEnergy
        fv.CurrentDayEnergyKwh.ShouldBe(50m);
    }

    [Fact]
    public void IsDailyLimitExceeded_Should_Return_True_When_Over()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid(), dailyLimitKwh: 20m);

        fv.RecordEnergy(20m);

        fv.IsDailyLimitExceeded().ShouldBeTrue();
    }

    [Fact]
    public void IsDailyLimitExceeded_Should_Return_True_When_Exceeded()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid(), dailyLimitKwh: 20m);

        fv.RecordEnergy(25m);

        fv.IsDailyLimitExceeded().ShouldBeTrue();
    }

    [Fact]
    public void IsDailyLimitExceeded_Should_Return_False_When_Under()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid(), dailyLimitKwh: 20m);

        fv.RecordEnergy(10m);

        fv.IsDailyLimitExceeded().ShouldBeFalse();
    }

    [Fact]
    public void IsDailyLimitExceeded_Should_Return_False_When_No_Limit()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid(), dailyLimitKwh: null);

        fv.RecordEnergy(1000m);

        fv.IsDailyLimitExceeded().ShouldBeFalse();
    }

    [Fact]
    public void ResetDailyEnergy_Then_Record_Should_Track_Correctly()
    {
        var fleet = CreateFleet();
        var fv = fleet.AddVehicle(Guid.NewGuid());

        fv.RecordEnergy(30m);
        fv.ResetDailyEnergy();
        fv.RecordEnergy(10m);

        fv.CurrentDayEnergyKwh.ShouldBe(10m);
        fv.CurrentMonthEnergyKwh.ShouldBe(40m);
    }

    private static Fleet CreateFleet()
    {
        return new Fleet(
            Guid.NewGuid(),
            "Test Fleet",
            Guid.NewGuid(),
            maxMonthlyBudgetVnd: 5_000_000m);
    }
}
