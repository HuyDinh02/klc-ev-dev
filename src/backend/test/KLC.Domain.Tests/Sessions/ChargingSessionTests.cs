using System;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Sessions;

public class ChargingSessionTests
{
    [Fact]
    public void Constructor_Should_Initialize_As_Pending()
    {
        var session = CreateSession();

        session.Status.ShouldBe(SessionStatus.Pending);
        session.TotalEnergyKwh.ShouldBe(0m);
        session.TotalCost.ShouldBe(0m);
    }

    [Fact]
    public void MarkStarting_Should_Transition_From_Pending()
    {
        var session = CreateSession();

        session.MarkStarting();

        session.Status.ShouldBe(SessionStatus.Starting);
    }

    [Fact]
    public void MarkStarting_Should_Throw_When_Not_Pending()
    {
        var session = CreateSession();
        session.MarkStarting();
        session.RecordStart(1, 0);

        var ex = Should.Throw<BusinessException>(() => session.MarkStarting());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Session.InvalidStateTransition);
    }

    [Fact]
    public void RecordStart_Should_Set_InProgress()
    {
        var session = CreateSession();
        session.MarkStarting();

        session.RecordStart(42, 1000);

        session.Status.ShouldBe(SessionStatus.InProgress);
        session.OcppTransactionId.ShouldBe(42);
        session.MeterStart.ShouldBe(1000);
        session.StartTime.ShouldNotBeNull();
    }

    [Fact]
    public void Suspend_Should_Transition_From_InProgress()
    {
        var session = CreateInProgressSession();

        session.Suspend();

        session.Status.ShouldBe(SessionStatus.Suspended);
    }

    [Fact]
    public void Suspend_Should_Throw_When_Not_InProgress()
    {
        var session = CreateSession();

        var ex = Should.Throw<BusinessException>(() => session.Suspend());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Session.InvalidStateTransition);
    }

    [Fact]
    public void Resume_Should_Transition_From_Suspended()
    {
        var session = CreateInProgressSession();
        session.Suspend();

        session.Resume();

        session.Status.ShouldBe(SessionStatus.InProgress);
    }

    [Fact]
    public void Resume_Should_Throw_When_Not_Suspended()
    {
        var session = CreateInProgressSession();

        var ex = Should.Throw<BusinessException>(() => session.Resume());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Session.InvalidStateTransition);
    }

    [Fact]
    public void MarkStopping_Should_Transition_From_InProgress()
    {
        var session = CreateInProgressSession();

        session.MarkStopping();

        session.Status.ShouldBe(SessionStatus.Stopping);
    }

    [Fact]
    public void MarkStopping_Should_Transition_From_Suspended()
    {
        var session = CreateInProgressSession();
        session.Suspend();

        session.MarkStopping();

        session.Status.ShouldBe(SessionStatus.Stopping);
    }

    [Fact]
    public void MarkStopping_Should_Throw_When_Pending()
    {
        var session = CreateSession();

        var ex = Should.Throw<BusinessException>(() => session.MarkStopping());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Session.InvalidStateTransition);
    }

    [Fact]
    public void RecordStop_Should_Calculate_Energy_And_Cost()
    {
        var session = CreateInProgressSession(meterStart: 1000, ratePerKwh: 3500);

        session.RecordStop(meterStop: 11000, stopReason: "EVDisconnected");

        session.Status.ShouldBe(SessionStatus.Completed);
        session.MeterStop.ShouldBe(11000);
        session.EndTime.ShouldNotBeNull();
        session.StopReason.ShouldBe("EVDisconnected");
        // Energy: (11000 - 1000) Wh = 10000 Wh = 10.000 kWh
        session.TotalEnergyKwh.ShouldBe(10.000m);
        // Cost: 10 kWh * 3500 VND/kWh = 35000 VND
        session.TotalCost.ShouldBe(35000m);
    }

    [Fact]
    public void RecordStop_Should_Handle_Zero_Energy()
    {
        var session = CreateInProgressSession(meterStart: 5000, ratePerKwh: 3500);

        session.RecordStop(meterStop: 5000);

        session.TotalEnergyKwh.ShouldBe(0m);
        session.TotalCost.ShouldBe(0m);
    }

    [Fact]
    public void MarkFailed_Should_Set_Failed_Status()
    {
        var session = CreateSession();

        session.MarkFailed("Charger error");

        session.Status.ShouldBe(SessionStatus.Failed);
        session.StopReason.ShouldBe("Charger error");
        session.EndTime.ShouldNotBeNull();
    }

    [Fact]
    public void AddMeterValue_Should_Add_To_Collection()
    {
        var session = CreateInProgressSession();
        var mvId = Guid.NewGuid();

        var mv = session.AddMeterValue(mvId, 5.5m, DateTime.UtcNow, 32m, 400m, 12.8m, 45m);

        session.MeterValues.Count.ShouldBe(1);
        mv.ShouldNotBeNull();
        mv.EnergyKwh.ShouldBe(5.5m);
    }

    [Fact]
    public void UpdateTotalEnergy_Should_Recalculate_Cost()
    {
        var session = CreateInProgressSession(ratePerKwh: 2000);

        session.UpdateTotalEnergy(15.5m);

        session.TotalEnergyKwh.ShouldBe(15.5m);
        session.TotalCost.ShouldBe(31000m); // 15.5 * 2000 = 31000
    }

    [Fact]
    public void Full_Session_Lifecycle_Should_Work()
    {
        var session = CreateSession(ratePerKwh: 4000);

        // Pending → Starting → InProgress → Suspended → InProgress → Stopping → Completed
        session.MarkStarting();
        session.Status.ShouldBe(SessionStatus.Starting);

        session.RecordStart(100, 0);
        session.Status.ShouldBe(SessionStatus.InProgress);

        session.Suspend();
        session.Status.ShouldBe(SessionStatus.Suspended);

        session.Resume();
        session.Status.ShouldBe(SessionStatus.InProgress);

        session.MarkStopping();
        session.Status.ShouldBe(SessionStatus.Stopping);

        session.RecordStop(20000); // 20 kWh
        session.Status.ShouldBe(SessionStatus.Completed);
        session.TotalEnergyKwh.ShouldBe(20.000m);
        session.TotalCost.ShouldBe(80000m); // 20 * 4000
    }

    private static ChargingSession CreateSession(decimal ratePerKwh = 3500)
    {
        return new ChargingSession(
            Guid.NewGuid(),
            userId: Guid.NewGuid(),
            stationId: Guid.NewGuid(),
            connectorNumber: 1,
            ratePerKwh: ratePerKwh);
    }

    private static ChargingSession CreateInProgressSession(
        int meterStart = 0,
        decimal ratePerKwh = 3500)
    {
        var session = CreateSession(ratePerKwh);
        session.MarkStarting();
        session.RecordStart(1, meterStart);
        return session;
    }
}
