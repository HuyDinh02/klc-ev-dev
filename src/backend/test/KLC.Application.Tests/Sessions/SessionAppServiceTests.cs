using System;
using KLC.Enums;
using KLC.Sessions;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Sessions;

/// <summary>
/// Tests for session business logic exercised by SessionAppService.
/// Validates domain rules for session lifecycle, cost calculation, and state transitions.
/// </summary>
public class SessionAppServiceTests
{
    private static ChargingSession CreateTestSession(
        Guid? userId = null,
        Guid? stationId = null,
        int connectorNumber = 1,
        decimal ratePerKwh = 3500m)
    {
        return new ChargingSession(
            Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            stationId ?? Guid.NewGuid(),
            connectorNumber,
            vehicleId: Guid.NewGuid(),
            tariffPlanId: Guid.NewGuid(),
            ratePerKwh: ratePerKwh);
    }

    [Fact]
    public void NewSession_Should_Have_Pending_Status()
    {
        var session = CreateTestSession();

        session.Status.ShouldBe(SessionStatus.Pending);
        session.TotalEnergyKwh.ShouldBe(0);
        session.TotalCost.ShouldBe(0);
        session.StartTime.ShouldBeNull();
        session.EndTime.ShouldBeNull();
    }

    [Fact]
    public void MarkStarting_From_Pending_Should_Succeed()
    {
        var session = CreateTestSession();

        session.MarkStarting();

        session.Status.ShouldBe(SessionStatus.Starting);
    }

    [Fact]
    public void MarkStarting_From_NonPending_Should_Throw()
    {
        var session = CreateTestSession();
        session.MarkStarting();
        session.RecordStart(1001, 0);

        // Session is InProgress, cannot MarkStarting again
        var ex = Should.Throw<BusinessException>(() => session.MarkStarting());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Session.InvalidStateTransition);
    }

    [Fact]
    public void RecordStart_Should_Set_InProgress_And_Timestamps()
    {
        var session = CreateTestSession();
        session.MarkStarting();

        session.RecordStart(1001, 5000);

        session.Status.ShouldBe(SessionStatus.InProgress);
        session.OcppTransactionId.ShouldBe(1001);
        session.MeterStart.ShouldBe(5000);
        session.StartTime.ShouldNotBeNull();
    }

    [Fact]
    public void RecordStop_Should_Calculate_Energy_And_Cost()
    {
        var ratePerKwh = 3500m;
        var session = CreateTestSession(ratePerKwh: ratePerKwh);
        session.MarkStarting();
        session.RecordStart(1001, 0);

        // 10 kWh consumed (10000 Wh)
        session.RecordStop(10000);

        session.Status.ShouldBe(SessionStatus.Completed);
        session.MeterStop.ShouldBe(10000);
        session.TotalEnergyKwh.ShouldBe(10m);
        session.TotalCost.ShouldBe(Math.Round(10m * ratePerKwh, 0));
        session.EndTime.ShouldNotBeNull();
    }

    [Fact]
    public void MarkStopping_From_InProgress_Should_Succeed()
    {
        var session = CreateTestSession();
        session.MarkStarting();
        session.RecordStart(1001, 0);

        session.MarkStopping();

        session.Status.ShouldBe(SessionStatus.Stopping);
    }

    [Fact]
    public void MarkStopping_From_Pending_Should_Throw()
    {
        var session = CreateTestSession();

        var ex = Should.Throw<BusinessException>(() => session.MarkStopping());
        ex.Code.ShouldBe(KLCDomainErrorCodes.Session.InvalidStateTransition);
    }

    [Fact]
    public void Suspend_And_Resume_Should_Toggle_Status()
    {
        var session = CreateTestSession();
        session.MarkStarting();
        session.RecordStart(1001, 0);

        session.Suspend();
        session.Status.ShouldBe(SessionStatus.Suspended);

        session.Resume();
        session.Status.ShouldBe(SessionStatus.InProgress);
    }

    [Fact]
    public void MarkFailed_Should_Set_Failed_Status_And_EndTime()
    {
        var session = CreateTestSession();
        session.MarkStarting();

        session.MarkFailed("Charger communication lost");

        session.Status.ShouldBe(SessionStatus.Failed);
        session.EndTime.ShouldNotBeNull();
        session.StopReason.ShouldBe("Charger communication lost");
    }

    [Fact]
    public void UpdateTotalEnergy_Should_Recalculate_Cost()
    {
        var ratePerKwh = 4000m;
        var session = CreateTestSession(ratePerKwh: ratePerKwh);
        session.MarkStarting();
        session.RecordStart(1001, 0);

        session.UpdateTotalEnergy(5.5m);

        session.TotalEnergyKwh.ShouldBe(5.5m);
        session.TotalCost.ShouldBe(Math.Round(5.5m * ratePerKwh, 0));
    }

    [Fact]
    public void AddMeterValue_Should_Deduplicate()
    {
        var session = CreateTestSession();
        session.MarkStarting();
        session.RecordStart(1001, 0);

        var timestamp = DateTime.UtcNow;
        var mv1 = session.AddMeterValue(Guid.NewGuid(), 1.5m, timestamp, 10m, 220m, 2.2m, 25m);
        mv1.ShouldNotBeNull();

        // Same timestamp + energy = duplicate, should return null
        var mv2 = session.AddMeterValue(Guid.NewGuid(), 1.5m, timestamp, 10m, 220m, 2.2m, 25m);
        mv2.ShouldBeNull();

        session.MeterValues.Count.ShouldBe(1);
    }

    [Fact]
    public void RecordStop_Should_Use_Higher_Of_Meter_Or_Running_Total()
    {
        var ratePerKwh = 3000m;
        var session = CreateTestSession(ratePerKwh: ratePerKwh);
        session.MarkStarting();
        session.RecordStart(1001, 0);

        // Simulate meter values accumulating more energy than final meter reading
        session.UpdateTotalEnergy(12m); // Running total = 12 kWh

        // But meterStop only shows 10 kWh (10000 Wh)
        session.RecordStop(10000);

        // Should keep the higher running total (12 kWh)
        session.TotalEnergyKwh.ShouldBe(12m);
        session.TotalCost.ShouldBe(Math.Round(12m * ratePerKwh, 0));
    }

    [Fact]
    public void Session_Should_Preserve_User_And_Station_References()
    {
        var userId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();

        var session = new ChargingSession(
            Guid.NewGuid(), userId, stationId, 2,
            vehicleId, tariffId, 3500m, "TAG123");

        session.UserId.ShouldBe(userId);
        session.StationId.ShouldBe(stationId);
        session.VehicleId.ShouldBe(vehicleId);
        session.TariffPlanId.ShouldBe(tariffId);
        session.ConnectorNumber.ShouldBe(2);
        session.IdTag.ShouldBe("TAG123");
        session.RatePerKwh.ShouldBe(3500m);
    }
}
