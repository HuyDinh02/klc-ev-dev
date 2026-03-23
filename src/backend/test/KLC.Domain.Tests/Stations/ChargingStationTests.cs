using System;
using KLC.Enums;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.Stations;

public class ChargingStationTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        var id = Guid.NewGuid();
        var station = new ChargingStation(id, "KLC-HN-001", "Station 1", "123 Street", 21.03, 105.85);

        station.Id.ShouldBe(id);
        station.StationCode.ShouldBe("KLC-HN-001");
        station.Name.ShouldBe("Station 1");
        station.Address.ShouldBe("123 Street");
        station.Latitude.ShouldBe(21.03);
        station.Longitude.ShouldBe(105.85);
        station.Status.ShouldBe(StationStatus.Offline);
        station.IsEnabled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    [InlineData(-200)]
    [InlineData(200)]
    public void SetLocation_Should_Throw_On_Invalid_Latitude(double latitude)
    {
        var station = CreateStation();

        var ex = Should.Throw<BusinessException>(() => station.SetLocation(latitude, 105.85));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Station.InvalidLatitude);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    [InlineData(-300)]
    [InlineData(300)]
    public void SetLocation_Should_Throw_On_Invalid_Longitude(double longitude)
    {
        var station = CreateStation();

        var ex = Should.Throw<BusinessException>(() => station.SetLocation(21.03, longitude));
        ex.Code.ShouldBe(KLCDomainErrorCodes.Station.InvalidLongitude);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    [InlineData(21.03, 105.85)]
    public void SetLocation_Should_Accept_Valid_Coordinates(double lat, double lon)
    {
        var station = CreateStation();
        station.SetLocation(lat, lon);

        station.Latitude.ShouldBe(lat);
        station.Longitude.ShouldBe(lon);
    }

    [Fact]
    public void RecordHeartbeat_Should_Set_LastHeartbeat_And_Transition_From_Offline()
    {
        var station = CreateStation();
        station.Status.ShouldBe(StationStatus.Offline);

        station.RecordHeartbeat();

        station.LastHeartbeat.ShouldNotBeNull();
        station.Status.ShouldBe(StationStatus.Online);
    }

    [Fact]
    public void RecordHeartbeat_Should_Not_Change_Status_If_Not_Offline()
    {
        var station = CreateStation();
        station.RecordHeartbeat(); // Now Online
        station.UpdateStatus(StationStatus.Disabled);

        station.RecordHeartbeat();

        station.Status.ShouldBe(StationStatus.Disabled);
    }

    [Fact]
    public void Disable_Should_Set_Disabled()
    {
        var station = CreateStation();
        station.RecordHeartbeat(); // Now Online

        station.Disable();

        station.IsEnabled.ShouldBeFalse();
        station.Status.ShouldBe(StationStatus.Disabled);
    }

    [Fact]
    public void Enable_Should_Set_Enabled()
    {
        var station = CreateStation();
        station.Disable();

        station.Enable();

        station.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void MarkOffline_Should_Set_Status()
    {
        var station = CreateStation();
        station.RecordHeartbeat(); // Now Available

        station.MarkOffline();

        station.Status.ShouldBe(StationStatus.Offline);
    }

    [Fact]
    public void AddConnector_Should_Add_And_Return_Connector()
    {
        var station = CreateStation();
        var connectorId = Guid.NewGuid();

        var connector = station.AddConnector(connectorId, 1, ConnectorType.CCS2, 150);

        station.Connectors.Count.ShouldBe(1);
        connector.Id.ShouldBe(connectorId);
        connector.ConnectorNumber.ShouldBe(1);
    }

    private static ChargingStation CreateStation()
    {
        return new ChargingStation(
            Guid.NewGuid(), "KLC-HN-001", "Test Station", "123 Test St", 21.03, 105.85);
    }
}
