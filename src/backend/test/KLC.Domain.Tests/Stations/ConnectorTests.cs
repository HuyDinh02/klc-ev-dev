using System;
using System.Linq;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Stations;

public class ConnectorTests
{
    [Fact]
    public void AddConnector_Should_Create_With_Unavailable_Status()
    {
        var station = CreateStation();

        station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);

        station.Connectors.Count.ShouldBe(1);
        var connector = station.Connectors.First();
        connector.Status.ShouldBe(ConnectorStatus.Unavailable);
        connector.IsEnabled.ShouldBeTrue();
        connector.ConnectorNumber.ShouldBe(1);
        connector.ConnectorType.ShouldBe(ConnectorType.CCS2);
        connector.MaxPowerKw.ShouldBe(50m);
    }

    [Fact]
    public void UpdateStatus_Should_Change_Status()
    {
        var connector = CreateConnector();

        connector.UpdateStatus(ConnectorStatus.Available);

        connector.Status.ShouldBe(ConnectorStatus.Available);
    }

    [Fact]
    public void UpdateStatus_Should_Allow_Multiple_Transitions()
    {
        var connector = CreateConnector();

        connector.UpdateStatus(ConnectorStatus.Available);
        connector.UpdateStatus(ConnectorStatus.Preparing);
        connector.UpdateStatus(ConnectorStatus.Charging);
        connector.UpdateStatus(ConnectorStatus.Finishing);
        connector.UpdateStatus(ConnectorStatus.Available);

        connector.Status.ShouldBe(ConnectorStatus.Available);
    }

    [Fact]
    public void SetMaxPower_Should_Update_When_Valid()
    {
        var connector = CreateConnector();

        connector.SetMaxPower(120);

        connector.MaxPowerKw.ShouldBe(120m);
    }

    [Fact]
    public void SetMaxPower_Should_Throw_When_Zero()
    {
        var connector = CreateConnector();

        var ex = Should.Throw<BusinessException>(() => connector.SetMaxPower(0));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Connector.MaxPowerInvalid);
    }

    [Fact]
    public void SetMaxPower_Should_Throw_When_Negative()
    {
        var connector = CreateConnector();

        Should.Throw<BusinessException>(() => connector.SetMaxPower(-10));
    }

    [Fact]
    public void Enable_Should_Set_Available_When_Unavailable()
    {
        var connector = CreateConnector();
        connector.Status.ShouldBe(ConnectorStatus.Unavailable); // initial state

        connector.Enable();

        connector.IsEnabled.ShouldBeTrue();
        connector.Status.ShouldBe(ConnectorStatus.Available);
    }

    [Fact]
    public void Enable_Should_Not_Change_Status_When_Not_Unavailable()
    {
        var connector = CreateConnector();
        connector.UpdateStatus(ConnectorStatus.Charging);

        connector.Enable();

        connector.IsEnabled.ShouldBeTrue();
        connector.Status.ShouldBe(ConnectorStatus.Charging); // unchanged
    }

    [Fact]
    public void Disable_Should_Set_Unavailable()
    {
        var connector = CreateConnector();
        connector.UpdateStatus(ConnectorStatus.Available);

        connector.Disable();

        connector.IsEnabled.ShouldBeFalse();
        connector.Status.ShouldBe(ConnectorStatus.Unavailable);
    }

    [Fact]
    public void Disable_Then_Enable_Should_Restore_Available()
    {
        var connector = CreateConnector();
        connector.UpdateStatus(ConnectorStatus.Available);

        connector.Disable();
        connector.IsEnabled.ShouldBeFalse();
        connector.Status.ShouldBe(ConnectorStatus.Unavailable);

        connector.Enable();
        connector.IsEnabled.ShouldBeTrue();
        connector.Status.ShouldBe(ConnectorStatus.Available);
    }

    private static Connector CreateConnector()
    {
        var station = CreateStation();
        station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
        return station.Connectors.First();
    }

    private static ChargingStation CreateStation()
    {
        return new ChargingStation(
            Guid.NewGuid(), "TST-001", "Test Station", "123 Test St",
            21.0278, 105.8342, null, null);
    }
}
