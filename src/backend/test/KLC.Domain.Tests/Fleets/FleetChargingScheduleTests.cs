using System;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Fleets;

public class FleetChargingScheduleTests
{
    [Fact]
    public void Create_Should_Set_Properties()
    {
        var id = Guid.NewGuid();
        var fleetId = Guid.NewGuid();
        var start = TimeSpan.FromHours(8);
        var end = TimeSpan.FromHours(17);

        var schedule = new FleetChargingSchedule(id, fleetId, 1, start, end);

        schedule.Id.ShouldBe(id);
        schedule.FleetId.ShouldBe(fleetId);
        schedule.DayOfWeek.ShouldBe(1);
        schedule.StartTimeUtc.ShouldBe(start);
        schedule.EndTimeUtc.ShouldBe(end);
    }

    [Fact]
    public void Create_Should_Accept_Sunday()
    {
        var schedule = new FleetChargingSchedule(
            Guid.NewGuid(), Guid.NewGuid(), 0, TimeSpan.FromHours(9), TimeSpan.FromHours(18));

        schedule.DayOfWeek.ShouldBe(0);
    }

    [Fact]
    public void Create_Should_Accept_Saturday()
    {
        var schedule = new FleetChargingSchedule(
            Guid.NewGuid(), Guid.NewGuid(), 6, TimeSpan.FromHours(10), TimeSpan.FromHours(14));

        schedule.DayOfWeek.ShouldBe(6);
    }

    [Fact]
    public void Create_Should_Reject_Negative_DayOfWeek()
    {
        Should.Throw<BusinessException>(() =>
            new FleetChargingSchedule(
                Guid.NewGuid(), Guid.NewGuid(), -1,
                TimeSpan.FromHours(8), TimeSpan.FromHours(17)));
    }

    [Fact]
    public void Create_Should_Reject_DayOfWeek_Greater_Than_6()
    {
        Should.Throw<BusinessException>(() =>
            new FleetChargingSchedule(
                Guid.NewGuid(), Guid.NewGuid(), 7,
                TimeSpan.FromHours(8), TimeSpan.FromHours(17)));
    }

    [Fact]
    public void Create_Should_Reject_Start_Equal_To_End()
    {
        var time = TimeSpan.FromHours(12);

        Should.Throw<BusinessException>(() =>
            new FleetChargingSchedule(
                Guid.NewGuid(), Guid.NewGuid(), 1, time, time));
    }

    [Fact]
    public void Create_Should_Reject_Start_After_End()
    {
        Should.Throw<BusinessException>(() =>
            new FleetChargingSchedule(
                Guid.NewGuid(), Guid.NewGuid(), 1,
                TimeSpan.FromHours(17), TimeSpan.FromHours(8)));
    }
}
