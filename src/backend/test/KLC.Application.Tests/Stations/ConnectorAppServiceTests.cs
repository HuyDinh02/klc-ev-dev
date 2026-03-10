using System;
using System.Linq;
using KLC.Enums;
using KLC.Stations;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Stations;

/// <summary>
/// Tests for connector business logic exercised by ConnectorAppService.
/// Validates domain rules for connector CRUD, status transitions, and validation.
/// </summary>
public class ConnectorAppServiceTests
{
    private static ChargingStation CreateStationWithConnector(
        int connectorNumber = 1,
        ConnectorType type = ConnectorType.CCS2,
        decimal maxPowerKw = 150m)
    {
        var station = new ChargingStation(
            Guid.NewGuid(), "KC-TEST-001", "Test Station",
            "123 Test St", 21.03, 105.85);
        station.AddConnector(Guid.NewGuid(), connectorNumber, type, maxPowerKw);
        return station;
    }

    [Fact]
    public void CreateConnector_Should_Set_Properties()
    {
        var station = new ChargingStation(
            Guid.NewGuid(), "KC-TEST-001", "Test Station",
            "123 Test St", 21.03, 105.85);

        var connector = station.AddConnector(
            Guid.NewGuid(), 1, ConnectorType.Type2, 22m);

        connector.ConnectorNumber.ShouldBe(1);
        connector.ConnectorType.ShouldBe(ConnectorType.Type2);
        connector.MaxPowerKw.ShouldBe(22m);
        connector.Status.ShouldBe(ConnectorStatus.Unavailable);
        connector.IsEnabled.ShouldBeTrue();
        connector.StationId.ShouldBe(station.Id);
    }

    [Fact]
    public void GetConnectors_Station_Should_Return_All_Connectors()
    {
        var station = new ChargingStation(
            Guid.NewGuid(), "KC-TEST-002", "Station 2",
            "456 Test St", 21.04, 105.86);

        station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 150m);
        station.AddConnector(Guid.NewGuid(), 2, ConnectorType.CHAdeMO, 50m);

        station.Connectors.Count.ShouldBe(2);
        station.Connectors.Select(c => c.ConnectorNumber).ShouldContain(1);
        station.Connectors.Select(c => c.ConnectorNumber).ShouldContain(2);
    }

    [Fact]
    public void UpdateConnector_SetMaxPower_Should_Update()
    {
        var station = CreateStationWithConnector(maxPowerKw: 50m);
        var connector = station.Connectors.First();

        connector.SetMaxPower(100m);

        connector.MaxPowerKw.ShouldBe(100m);
    }

    [Fact]
    public void UpdateConnector_SetMaxPower_Zero_Should_Throw()
    {
        var station = CreateStationWithConnector();
        var connector = station.Connectors.First();

        var ex = Should.Throw<BusinessException>(() =>
            connector.SetMaxPower(0));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Connector.MaxPowerInvalid);
    }

    [Fact]
    public void UpdateConnector_SetMaxPower_Negative_Should_Throw()
    {
        var station = CreateStationWithConnector();
        var connector = station.Connectors.First();

        var ex = Should.Throw<BusinessException>(() =>
            connector.SetMaxPower(-10m));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Connector.MaxPowerInvalid);
    }

    [Fact]
    public void Enable_Should_Set_IsEnabled_And_Status_Available()
    {
        var station = CreateStationWithConnector();
        var connector = station.Connectors.First();
        connector.Disable(); // Start disabled

        connector.Enable();

        connector.IsEnabled.ShouldBeTrue();
        connector.Status.ShouldBe(ConnectorStatus.Available);
    }

    [Fact]
    public void Disable_Should_Set_IsEnabled_False_And_Status_Unavailable()
    {
        var station = CreateStationWithConnector();
        var connector = station.Connectors.First();
        connector.Enable();

        connector.Disable();

        connector.IsEnabled.ShouldBeFalse();
        connector.Status.ShouldBe(ConnectorStatus.Unavailable);
    }

    [Fact]
    public void UpdateStatus_Should_Change_Connector_Status()
    {
        var station = CreateStationWithConnector();
        var connector = station.Connectors.First();

        connector.UpdateStatus(ConnectorStatus.Charging);

        connector.Status.ShouldBe(ConnectorStatus.Charging);
    }

    [Fact]
    public void Enable_When_Already_Available_Should_Stay_Available()
    {
        var station = CreateStationWithConnector();
        var connector = station.Connectors.First();
        connector.Enable();
        connector.UpdateStatus(ConnectorStatus.Charging);

        // Enable when not Unavailable should not change status to Available
        connector.Enable();

        connector.IsEnabled.ShouldBeTrue();
        connector.Status.ShouldBe(ConnectorStatus.Charging);
    }

    [Fact]
    public void ConnectorTypes_Should_Cover_All_Standards()
    {
        var station = new ChargingStation(
            Guid.NewGuid(), "KC-MULTI-001", "Multi-type Station",
            "789 Test St", 21.05, 105.87);

        station.AddConnector(Guid.NewGuid(), 1, ConnectorType.Type2, 22m);
        station.AddConnector(Guid.NewGuid(), 2, ConnectorType.CCS2, 150m);
        station.AddConnector(Guid.NewGuid(), 3, ConnectorType.CHAdeMO, 50m);
        station.AddConnector(Guid.NewGuid(), 4, ConnectorType.GBT, 60m);
        station.AddConnector(Guid.NewGuid(), 5, ConnectorType.Type1, 7.4m);

        station.Connectors.Count.ShouldBe(5);
        station.Connectors.Select(c => c.ConnectorType).ShouldContain(ConnectorType.Type2);
        station.Connectors.Select(c => c.ConnectorType).ShouldContain(ConnectorType.CCS2);
        station.Connectors.Select(c => c.ConnectorType).ShouldContain(ConnectorType.CHAdeMO);
        station.Connectors.Select(c => c.ConnectorType).ShouldContain(ConnectorType.GBT);
        station.Connectors.Select(c => c.ConnectorType).ShouldContain(ConnectorType.Type1);
    }

    [Fact]
    public void StatusTransition_Available_To_Charging_To_Finishing()
    {
        var station = CreateStationWithConnector();
        var connector = station.Connectors.First();

        connector.UpdateStatus(ConnectorStatus.Available);
        connector.Status.ShouldBe(ConnectorStatus.Available);

        connector.UpdateStatus(ConnectorStatus.Preparing);
        connector.Status.ShouldBe(ConnectorStatus.Preparing);

        connector.UpdateStatus(ConnectorStatus.Charging);
        connector.Status.ShouldBe(ConnectorStatus.Charging);

        connector.UpdateStatus(ConnectorStatus.Finishing);
        connector.Status.ShouldBe(ConnectorStatus.Finishing);

        connector.UpdateStatus(ConnectorStatus.Available);
        connector.Status.ShouldBe(ConnectorStatus.Available);
    }
}
