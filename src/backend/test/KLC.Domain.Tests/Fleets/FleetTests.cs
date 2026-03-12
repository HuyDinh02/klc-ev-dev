using System;
using System.Linq;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Fleets;

public class FleetTests
{
    [Fact]
    public void Create_Should_Set_Properties()
    {
        var id = Guid.NewGuid();
        var operatorUserId = Guid.NewGuid();

        var fleet = new Fleet(
            id,
            "Test Fleet",
            operatorUserId,
            "Fleet description",
            5_000_000m,
            ChargingPolicyType.ScheduledOnly,
            90);

        fleet.Id.ShouldBe(id);
        fleet.Name.ShouldBe("Test Fleet");
        fleet.OperatorUserId.ShouldBe(operatorUserId);
        fleet.Description.ShouldBe("Fleet description");
        fleet.MaxMonthlyBudgetVnd.ShouldBe(5_000_000m);
        fleet.ChargingPolicy.ShouldBe(ChargingPolicyType.ScheduledOnly);
        fleet.BudgetAlertThresholdPercent.ShouldBe(90);
        fleet.IsActive.ShouldBeTrue();
        fleet.CurrentMonthSpentVnd.ShouldBe(0m);
        fleet.Vehicles.ShouldBeEmpty();
    }

    [Fact]
    public void Create_Should_Have_Default_Policy_And_Budget()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid());

        fleet.ChargingPolicy.ShouldBe(ChargingPolicyType.AnytimeAnywhere);
        fleet.MaxMonthlyBudgetVnd.ShouldBe(0m);
        fleet.BudgetAlertThresholdPercent.ShouldBe(80);
    }

    [Fact]
    public void Create_Should_Reject_Empty_Name()
    {
        Should.Throw<ArgumentException>(() =>
            new Fleet(Guid.NewGuid(), "", Guid.NewGuid()));
    }

    [Fact]
    public void SetName_Should_Update()
    {
        var fleet = CreateFleet();
        fleet.SetName("Updated Fleet");

        fleet.Name.ShouldBe("Updated Fleet");
    }

    [Fact]
    public void SetName_Should_Reject_Empty()
    {
        var fleet = CreateFleet();

        Should.Throw<ArgumentException>(() => fleet.SetName(""));
    }

    [Fact]
    public void SetBudget_Should_Update()
    {
        var fleet = CreateFleet();
        fleet.SetBudget(10_000_000m, 75);

        fleet.MaxMonthlyBudgetVnd.ShouldBe(10_000_000m);
        fleet.BudgetAlertThresholdPercent.ShouldBe(75);
    }

    [Fact]
    public void SetBudget_Should_Reject_Negative()
    {
        var fleet = CreateFleet();

        var ex = Should.Throw<BusinessException>(() => fleet.SetBudget(-1m, 80));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Fleet.InvalidBudget);
    }

    [Fact]
    public void SetBudget_Should_Reject_Invalid_Threshold_Over_100()
    {
        var fleet = CreateFleet();

        var ex = Should.Throw<BusinessException>(() => fleet.SetBudget(1_000_000m, 101));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Fleet.InvalidBudget);
    }

    [Fact]
    public void SetBudget_Should_Reject_Negative_Threshold()
    {
        var fleet = CreateFleet();

        var ex = Should.Throw<BusinessException>(() => fleet.SetBudget(1_000_000m, -1));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Fleet.InvalidBudget);
    }

    [Fact]
    public void SetChargingPolicy_Should_Update()
    {
        var fleet = CreateFleet();
        fleet.SetChargingPolicy(ChargingPolicyType.ApprovedStationsOnly);

        fleet.ChargingPolicy.ShouldBe(ChargingPolicyType.ApprovedStationsOnly);
    }

    [Fact]
    public void AddVehicle_Should_Add_To_Collection()
    {
        var fleet = CreateFleet();
        var vehicleId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var result = fleet.AddVehicle(vehicleId, driverId, 50m);

        fleet.Vehicles.Count.ShouldBe(1);
        result.FleetId.ShouldBe(fleet.Id);
        result.VehicleId.ShouldBe(vehicleId);
        result.DriverUserId.ShouldBe(driverId);
        result.DailyChargingLimitKwh.ShouldBe(50m);
    }

    [Fact]
    public void AddVehicle_Should_Reject_Duplicate()
    {
        var fleet = CreateFleet();
        var vehicleId = Guid.NewGuid();
        fleet.AddVehicle(vehicleId);

        var ex = Should.Throw<BusinessException>(() => fleet.AddVehicle(vehicleId));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Fleet.VehicleAlreadyInFleet);
    }

    [Fact]
    public void RemoveVehicle_Should_Soft_Delete()
    {
        var fleet = CreateFleet();
        var vehicleId = Guid.NewGuid();
        fleet.AddVehicle(vehicleId);

        fleet.RemoveVehicle(vehicleId);

        // Vehicle is soft deleted (IsActive = false)
        var vehicle = fleet.Vehicles.First(v => v.VehicleId == vehicleId);
        vehicle.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void RemoveVehicle_Should_Throw_If_Not_Found()
    {
        var fleet = CreateFleet();

        var ex = Should.Throw<BusinessException>(() => fleet.RemoveVehicle(Guid.NewGuid()));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Fleet.VehicleNotInFleet);
    }

    [Fact]
    public void RecordSpending_Should_Accumulate()
    {
        var fleet = CreateFleet();
        fleet.RecordSpending(100_000m);
        fleet.RecordSpending(200_000m);

        fleet.CurrentMonthSpentVnd.ShouldBe(300_000m);
    }

    [Fact]
    public void ResetMonthlySpending_Should_Clear()
    {
        var fleet = CreateFleet();
        fleet.RecordSpending(500_000m);

        fleet.ResetMonthlySpending();

        fleet.CurrentMonthSpentVnd.ShouldBe(0m);
    }

    [Fact]
    public void IsBudgetExceeded_Should_Return_True_When_Over()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid(),
            maxMonthlyBudgetVnd: 1_000_000m);
        fleet.RecordSpending(1_000_000m);

        fleet.IsBudgetExceeded().ShouldBeTrue();
    }

    [Fact]
    public void IsBudgetExceeded_Should_Return_True_When_Exceeded()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid(),
            maxMonthlyBudgetVnd: 1_000_000m);
        fleet.RecordSpending(1_500_000m);

        fleet.IsBudgetExceeded().ShouldBeTrue();
    }

    [Fact]
    public void IsBudgetExceeded_Should_Return_False_When_Under()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid(),
            maxMonthlyBudgetVnd: 1_000_000m);
        fleet.RecordSpending(500_000m);

        fleet.IsBudgetExceeded().ShouldBeFalse();
    }

    [Fact]
    public void IsBudgetExceeded_Should_Return_False_When_No_Budget()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid(),
            maxMonthlyBudgetVnd: 0);
        fleet.RecordSpending(500_000m);

        fleet.IsBudgetExceeded().ShouldBeFalse();
    }

    [Fact]
    public void GetBudgetUtilizationPercent_Should_Calculate()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid(),
            maxMonthlyBudgetVnd: 1_000_000m);
        fleet.RecordSpending(750_000m);

        fleet.GetBudgetUtilizationPercent().ShouldBe(75.00m);
    }

    [Fact]
    public void GetBudgetUtilizationPercent_Should_Return_Zero_When_No_Budget()
    {
        var fleet = new Fleet(Guid.NewGuid(), "Fleet", Guid.NewGuid(),
            maxMonthlyBudgetVnd: 0);

        fleet.GetBudgetUtilizationPercent().ShouldBe(0m);
    }

    [Fact]
    public void Activate_Deactivate_Should_Toggle()
    {
        var fleet = CreateFleet();
        fleet.IsActive.ShouldBeTrue();

        fleet.Deactivate();
        fleet.IsActive.ShouldBeFalse();

        fleet.Activate();
        fleet.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void RemoveVehicle_Then_ReAdd_Should_Allow()
    {
        var fleet = CreateFleet();
        var vehicleId = Guid.NewGuid();

        fleet.AddVehicle(vehicleId);
        fleet.RemoveVehicle(vehicleId);

        // SoftDelete sets IsDeleted=true, so re-adding should succeed
        var newVehicle = fleet.AddVehicle(vehicleId);
        newVehicle.ShouldNotBeNull();
        newVehicle.VehicleId.ShouldBe(vehicleId);
    }

    private static Fleet CreateFleet()
    {
        return new Fleet(
            Guid.NewGuid(),
            "Test Fleet",
            Guid.NewGuid(),
            "Test fleet description",
            5_000_000m,
            ChargingPolicyType.AnytimeAnywhere,
            80);
    }
}
