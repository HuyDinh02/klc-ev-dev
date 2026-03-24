using System;
using System.Linq;
using KLC.Enums;
using KLC.Stations;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Stations;

/// <summary>
/// Tests for station business logic exercised by StationAppService.
/// Validates domain rules for station creation, status management, and validation.
/// </summary>
public class StationAppServiceTests
{
    private static ChargingStation CreateTestStation(
        string stationCode = "KC-HN-001",
        string name = "Test Station",
        string address = "123 Test Street, Hanoi",
        double latitude = 21.0285,
        double longitude = 105.8542)
    {
        return new ChargingStation(
            Guid.NewGuid(),
            stationCode,
            name,
            address,
            latitude,
            longitude);
    }

    [Fact]
    public void CreateStation_Should_Set_Default_Values()
    {
        var station = CreateTestStation();

        station.StationCode.ShouldBe("KC-HN-001");
        station.Name.ShouldBe("Test Station");
        station.Address.ShouldBe("123 Test Street, Hanoi");
        station.Latitude.ShouldBe(21.0285);
        station.Longitude.ShouldBe(105.8542);
        station.Status.ShouldBe(StationStatus.Offline);
        station.IsEnabled.ShouldBeTrue();
        station.Connectors.ShouldBeEmpty();
        station.Location.ShouldNotBeNull();
        station.Location!.SRID.ShouldBe(4326);
    }

    [Fact]
    public void CreateStation_With_GroupAndTariff_Should_Set_References()
    {
        var groupId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();

        var station = new ChargingStation(
            Guid.NewGuid(), "KC-HN-002", "Station 2",
            "456 Test St", 21.03, 105.85,
            groupId, tariffId);

        station.StationGroupId.ShouldBe(groupId);
        station.TariffPlanId.ShouldBe(tariffId);
    }

    [Fact]
    public void CreateStation_With_EmptyCode_Should_Throw()
    {
        Should.Throw<Exception>(() =>
            CreateTestStation(stationCode: ""));
    }

    [Fact]
    public void CreateStation_With_EmptyName_Should_Throw()
    {
        Should.Throw<Exception>(() =>
            CreateTestStation(name: ""));
    }

    [Fact]
    public void CreateStation_With_InvalidLatitude_Should_Throw()
    {
        var ex = Should.Throw<BusinessException>(() =>
            CreateTestStation(latitude: 91.0));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Station.InvalidLatitude);
    }

    [Fact]
    public void CreateStation_With_InvalidLongitude_Should_Throw()
    {
        var ex = Should.Throw<BusinessException>(() =>
            CreateTestStation(longitude: 181.0));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Station.InvalidLongitude);
    }

    [Fact]
    public void UpdateStation_SetName_Should_Update()
    {
        var station = CreateTestStation();

        station.SetName("Updated Station");

        station.Name.ShouldBe("Updated Station");
    }

    [Fact]
    public void UpdateStation_SetAddress_Should_Update()
    {
        var station = CreateTestStation();

        station.SetAddress("789 New Address, HCMC");

        station.Address.ShouldBe("789 New Address, HCMC");
    }

    [Fact]
    public void UpdateStation_SetLocation_Should_Update_LatLngAndPoint()
    {
        var station = CreateTestStation();

        station.SetLocation(10.762622, 106.660172);

        station.Latitude.ShouldBe(10.762622);
        station.Longitude.ShouldBe(106.660172);
        station.Location.ShouldNotBeNull();
    }

    [Fact]
    public void Enable_Should_Set_IsEnabled_True()
    {
        var station = CreateTestStation();
        station.Disable();
        station.IsEnabled.ShouldBeFalse();

        station.Enable();

        station.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Disable_Should_Set_Disabled()
    {
        var station = CreateTestStation();

        station.Disable();

        station.IsEnabled.ShouldBeFalse();
        station.Status.ShouldBe(StationStatus.Disabled);
    }

    [Fact]
    public void RecordHeartbeat_Should_Update_LastHeartbeat()
    {
        var station = CreateTestStation();
        station.LastHeartbeat.ShouldBeNull();

        station.RecordHeartbeat();

        station.LastHeartbeat.ShouldNotBeNull();
    }

    [Fact]
    public void RecordHeartbeat_Should_Transition_Offline_To_Online()
    {
        var station = CreateTestStation();
        station.Status.ShouldBe(StationStatus.Offline);

        station.RecordHeartbeat();

        station.Status.ShouldBe(StationStatus.Online);
    }

    [Fact]
    public void MarkOffline_Should_Set_Status_Offline()
    {
        var station = CreateTestStation();
        station.RecordHeartbeat(); // Goes Online
        station.Status.ShouldBe(StationStatus.Online);

        station.MarkOffline();

        station.Status.ShouldBe(StationStatus.Offline);
    }

    [Fact]
    public void UpdateStatus_Should_Change_Status()
    {
        var station = CreateTestStation();

        station.UpdateStatus(StationStatus.Disabled);

        station.Status.ShouldBe(StationStatus.Disabled);
    }

    [Fact]
    public void Decommission_Should_Set_Status_And_Disable()
    {
        var station = CreateTestStation();

        station.Decommission();

        station.Status.ShouldBe(StationStatus.Decommissioned);
        station.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Enable_Should_Throw_For_Decommissioned_Station()
    {
        var station = CreateTestStation();
        station.Decommission();

        var ex = Should.Throw<BusinessException>(() => station.Enable());

        ex.Code.ShouldBe(KLCDomainErrorCodes.Station.CannotEnableDecommissioned);
    }

    [Fact]
    public void SetStationInfo_Should_Update_Vendor_Fields()
    {
        var station = CreateTestStation();

        station.SetStationInfo("ABB", "Terra 54", "SN12345", "1.5.3");

        station.Vendor.ShouldBe("ABB");
        station.Model.ShouldBe("Terra 54");
        station.SerialNumber.ShouldBe("SN12345");
        station.FirmwareVersion.ShouldBe("1.5.3");
    }

    [Fact]
    public void SetTariffPlan_Should_Update_Reference()
    {
        var station = CreateTestStation();
        var tariffId = Guid.NewGuid();

        station.SetTariffPlan(tariffId);

        station.TariffPlanId.ShouldBe(tariffId);
    }

    [Fact]
    public void SetStationGroup_Should_Update_Reference()
    {
        var station = CreateTestStation();
        var groupId = Guid.NewGuid();

        station.SetStationGroup(groupId);

        station.StationGroupId.ShouldBe(groupId);
    }

    [Fact]
    public void AddConnector_Should_Add_To_Collection()
    {
        var station = CreateTestStation();

        var connector = station.AddConnector(
            Guid.NewGuid(), 1, ConnectorType.CCS2, 150m);

        station.Connectors.Count.ShouldBe(1);
        connector.ConnectorNumber.ShouldBe(1);
        connector.ConnectorType.ShouldBe(ConnectorType.CCS2);
        connector.MaxPowerKw.ShouldBe(150m);
        connector.StationId.ShouldBe(station.Id);
        connector.Status.ShouldBe(ConnectorStatus.Unavailable);
        connector.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void AddMultipleConnectors_Should_Track_All()
    {
        var station = CreateTestStation();

        station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 150m);
        station.AddConnector(Guid.NewGuid(), 2, ConnectorType.CHAdeMO, 50m);
        station.AddConnector(Guid.NewGuid(), 3, ConnectorType.Type2, 22m);

        station.Connectors.Count.ShouldBe(3);
        station.Connectors.Select(c => c.ConnectorNumber).ShouldBe(
            new[] { 1, 2, 3 }, ignoreOrder: true);
    }
}
