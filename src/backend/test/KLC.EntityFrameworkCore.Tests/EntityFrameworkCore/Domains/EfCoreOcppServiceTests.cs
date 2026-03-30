using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.EntityFrameworkCore;
using KLC.Enums;
using KLC.Ocpp;
using KLC.Sessions;
using KLC.Stations;
using KLC.Tariffs;
using KLC.Users;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace KLC.Domains;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class EfCoreOcppServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly IOcppService _ocppService;

    public EfCoreOcppServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _ocppService = GetRequiredService<IOcppService>();
    }

    #region HandleBootNotificationAsync

    [Fact]
    public async Task BootNotification_Should_Return_StationId_For_Known_Station()
    {
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-BOOT-001", "Boot Test", "123 Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleBootNotificationAsync(
                "OCPP-BOOT-001", "TestVendor", "TestModel", "SN-001", "1.0.0");

            result.ShouldNotBeNull();
            result.Value.ShouldBe(stationId);
        });
    }

    [Fact]
    public async Task BootNotification_Should_Update_Station_Info()
    {
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-BOOT-002", "Boot Test 2", "456 Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleBootNotificationAsync(
                "OCPP-BOOT-002", "ABB", "Terra 54", "SN-12345", "2.1.0");
            result.ShouldNotBeNull();
            result.Value.ShouldBe(stationId);
        });

        // Detach all tracked entities so the next query hits the DB
        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = await _dbContext.ChargingStations.FirstAsync(s => s.Id == stationId);
            station.Vendor.ShouldBe("ABB");
            station.Model.ShouldBe("Terra 54");
            station.SerialNumber.ShouldBe("SN-12345");
            station.FirmwareVersion.ShouldBe("2.1.0");
            station.LastHeartbeat.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task BootNotification_Should_Return_Null_For_Unknown_Station()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleBootNotificationAsync(
                "UNKNOWN-STATION", "Vendor", "Model", null, null);

            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task BootNotification_Should_Return_Null_For_Disabled_Station()
    {
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-BOOT-DISABLED", "Disabled Test", "123 Test St", 21.0, 105.8);
            station.Disable();
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleBootNotificationAsync(
                "OCPP-BOOT-DISABLED", "Vendor", "Model", null, null);

            result.ShouldBeNull();
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = await _dbContext.ChargingStations.FirstAsync(s => s.Id == stationId);
            station.Status.ShouldBe(StationStatus.Disabled);
            station.LastHeartbeat.ShouldBeNull();
        });
    }

    [Fact]
    public async Task BootNotification_Should_Return_Null_For_Decommissioned_Station()
    {
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-BOOT-DECOM", "Decom Test", "123 Test St", 21.0, 105.8);
            station.Decommission();
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleBootNotificationAsync(
                "OCPP-BOOT-DECOM", "Vendor", "Model", null, null);

            result.ShouldBeNull();
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = await _dbContext.ChargingStations.FirstAsync(s => s.Id == stationId);
            station.Status.ShouldBe(StationStatus.Decommissioned);
            station.LastHeartbeat.ShouldBeNull();
        });
    }

    #endregion

    #region HandleHeartbeatAsync

    [Fact]
    public async Task Heartbeat_Should_Record_Timestamp()
    {
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-HB-001", "HB Test", "789 St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _ocppService.HandleHeartbeatAsync("OCPP-HB-001");
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = await _dbContext.ChargingStations.FirstAsync(s => s.Id == stationId);
            station.LastHeartbeat.ShouldNotBeNull();
        });
    }

    #endregion

    #region HandleStatusNotificationAsync

    [Fact]
    public async Task StatusNotification_Should_Update_Connector_Status()
    {
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-SN-001", "SN Test", "Test St", 21.0, 105.8);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleStatusNotificationAsync(
                "OCPP-SN-001", 1, ConnectorStatus.Available, null);

            result.ShouldNotBeNull();
            result!.PreviousStatus.ShouldBe(ConnectorStatus.Unavailable);
            result.NewStatus.ShouldBe(ConnectorStatus.Available);
            result.StationId.ShouldBe(stationId);
        });
    }

    [Fact]
    public async Task StatusNotification_ConnectorZero_Should_Update_Station_Status()
    {
        var stationId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-SN-002", "SN Test 2", "Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleStatusNotificationAsync(
                "OCPP-SN-002", 0, ConnectorStatus.Faulted, "GroundFailure");

            result.ShouldNotBeNull();
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = await _dbContext.ChargingStations.FirstAsync(s => s.Id == stationId);
            station.Status.ShouldBe(StationStatus.Offline);
        });
    }

    [Fact]
    public async Task StatusNotification_Should_Return_Null_For_Unknown_Station()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleStatusNotificationAsync(
                "UNKNOWN-SN", 1, ConnectorStatus.Available, null);

            result.ShouldBeNull();
        });
    }

    #endregion

    #region HandleStartTransactionAsync

    [Fact]
    public async Task StartTransaction_Should_Create_Session()
    {
        var stationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-ST-001", "ST Test", "Test St", 21.0, 105.8);
            station.AddConnector(Guid.NewGuid(), 1, ConnectorType.CCS2, 50);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        Guid? sessionId = null;
        await WithUnitOfWorkAsync(async () =>
        {
            sessionId = await _ocppService.HandleStartTransactionAsync(
                "OCPP-ST-001", 1, userId.ToString(), 1000, 12345);

            sessionId.ShouldNotBeNull();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var session = await _dbContext.ChargingSessions.FirstAsync(s => s.Id == sessionId!.Value);
            session.StationId.ShouldBe(stationId);
            session.ConnectorNumber.ShouldBe(1);
            session.UserId.ShouldBe(userId);
            session.OcppTransactionId.ShouldBe(12345);
            session.MeterStart.ShouldBe(1000);
            session.Status.ShouldBe(SessionStatus.InProgress);
            session.IdTag.ShouldBe(userId.ToString());
        });
    }

    [Fact]
    public async Task StartTransaction_Should_Return_Null_For_Unknown_Station()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleStartTransactionAsync(
                "UNKNOWN-ST", 1, Guid.NewGuid().ToString(), 0, 99999);

            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task StartTransaction_Duplicate_Should_Return_Existing_Session()
    {
        var stationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        const int txId = 55555;

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-ST-002", "ST Test 2", "Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(sessionId, Guid.NewGuid(), stationId, 1);
            session.RecordStart(txId, 0);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleStartTransactionAsync(
                "OCPP-ST-002", 1, Guid.NewGuid().ToString(), 500, txId);

            result.ShouldNotBeNull();
            result.Value.ShouldBe(sessionId);
        });
    }

    [Fact]
    public async Task StartTransaction_Should_Resolve_RFID_Tag()
    {
        var stationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-ST-003", "ST Test 3", "Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var idTag = new UserIdTag(Guid.NewGuid(), userId, "RFID-ABC123", IdTagType.Rfid);
            await _dbContext.UserIdTags.AddAsync(idTag);
            await _dbContext.SaveChangesAsync();
        });

        Guid? sessionId = null;
        await WithUnitOfWorkAsync(async () =>
        {
            sessionId = await _ocppService.HandleStartTransactionAsync(
                "OCPP-ST-003", 1, "RFID-ABC123", 0, 77777);

            sessionId.ShouldNotBeNull();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var session = await _dbContext.ChargingSessions.FirstAsync(s => s.Id == sessionId!.Value);
            session.UserId.ShouldBe(userId);
            session.IdTag.ShouldBe("RFID-ABC123");
        });
    }

    [Fact]
    public async Task StartTransaction_Should_Resolve_Tariff_Rate()
    {
        var stationId = Guid.NewGuid();
        var tariffId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var tariff = new TariffPlan(tariffId, "Test Tariff", 3500, 10,
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));
            await _dbContext.TariffPlans.AddAsync(tariff);

            var station = new ChargingStation(stationId, "OCPP-ST-004", "ST Test 4", "Test St", 21.0, 105.8);
            station.SetTariffPlan(tariffId);
            await _dbContext.ChargingStations.AddAsync(station);
            await _dbContext.SaveChangesAsync();
        });

        Guid? sessionId = null;
        await WithUnitOfWorkAsync(async () =>
        {
            sessionId = await _ocppService.HandleStartTransactionAsync(
                "OCPP-ST-004", 1, Guid.NewGuid().ToString(), 0, 88888);

            sessionId.ShouldNotBeNull();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var session = await _dbContext.ChargingSessions.FirstAsync(s => s.Id == sessionId!.Value);
            session.RatePerKwh.ShouldBeGreaterThan(0);
        });
    }

    #endregion

    #region HandleStopTransactionAsync

    [Fact]
    public async Task StopTransaction_Should_Complete_Session()
    {
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        const int txId = 11111;

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-STOP-001", "Stop Test", "Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(sessionId, Guid.NewGuid(), stationId, 1, ratePerKwh: 3500);
            session.RecordStart(txId, 1000);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleStopTransactionAsync(txId, 11000, "EVDisconnected");

            result.ShouldNotBeNull();
            result!.SessionId.ShouldBe(sessionId);
            result.StationId.ShouldBe(stationId);
            result.TotalEnergyKwh.ShouldBe(10m); // (11000 - 1000) / 1000 = 10 kWh
            result.TotalCost.ShouldBe(35000m); // 10 kWh * 3500 VND
        });
    }

    [Fact]
    public async Task StopTransaction_Should_Return_Null_For_Unknown_Transaction()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleStopTransactionAsync(99999, 5000, null);

            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task StopTransaction_Duplicate_Should_Return_Null()
    {
        var sessionId = Guid.NewGuid();
        const int txId = 22222;

        await WithUnitOfWorkAsync(async () =>
        {
            var session = new ChargingSession(sessionId, Guid.NewGuid(), Guid.NewGuid(), 1);
            session.RecordStart(txId, 0);
            session.RecordStop(5000, "Normal");
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleStopTransactionAsync(txId, 5000, "Normal");

            result.ShouldBeNull(); // Already completed
        });
    }

    #endregion

    #region HandleMeterValuesAsync

    [Fact]
    public async Task MeterValues_Should_Record_And_Return_Result()
    {
        var stationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        const int txId = 33333;

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-MV-001", "MV Test", "Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(sessionId, Guid.NewGuid(), stationId, 1, ratePerKwh: 3500);
            session.RecordStart(txId, 1000);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleMeterValuesAsync(
                "OCPP-MV-001", 1, txId,
                energyWh: 6000, // 6 kWh in Wh
                timestamp: DateTime.UtcNow.ToString("o"),
                currentAmps: 32, voltage: 230, power: 7360, soc: 45);

            result.ShouldNotBeNull();
            result!.SessionId.ShouldBe(sessionId);
            result.TotalEnergyKwh.ShouldBe(5m); // (6000 - 1000) / 1000 = 5 kWh
        });
    }

    [Fact]
    public async Task MeterValues_Should_Reject_Backward_Reading()
    {
        var stationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        const int txId = 44444;

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-MV-002", "MV Test 2", "Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(sessionId, Guid.NewGuid(), stationId, 1);
            session.RecordStart(txId, 5000); // MeterStart = 5000 Wh
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            // energyWh (3000) < MeterStart (5000) → should be rejected
            var result = await _ocppService.HandleMeterValuesAsync(
                "OCPP-MV-002", 1, txId,
                energyWh: 3000,
                timestamp: DateTime.UtcNow.ToString("o"),
                currentAmps: null, voltage: null, power: null, soc: null);

            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task MeterValues_Should_Reject_Unreasonable_Jump()
    {
        var stationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        const int txId = 55556;

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-MV-003", "MV Test 3", "Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(sessionId, Guid.NewGuid(), stationId, 1);
            session.RecordStart(txId, 0);
            // Add an initial meter value at 10 kWh
            session.AddMeterValue(Guid.NewGuid(), 10m, DateTime.UtcNow.AddMinutes(-5), null, null, null, null);
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            // 600 kWh jump (> 500 kWh threshold) → should be rejected
            var result = await _ocppService.HandleMeterValuesAsync(
                "OCPP-MV-003", 1, txId,
                energyWh: 610_000, // 610 kWh in Wh
                timestamp: DateTime.UtcNow.ToString("o"),
                currentAmps: null, voltage: null, power: null, soc: null);

            result.ShouldBeNull();
        });
    }

    [Fact]
    public async Task MeterValues_Should_Return_Null_For_Unknown_Station()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.HandleMeterValuesAsync(
                "UNKNOWN-MV", 1, null,
                energyWh: 1000,
                timestamp: DateTime.UtcNow.ToString("o"),
                currentAmps: null, voltage: null, power: null, soc: null);

            result.ShouldBeNull();
        });
    }

    #endregion

    #region ValidateIdTagAsync

    [Fact]
    public async Task ValidateIdTag_Should_Accept_Valid_Guid()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.ValidateIdTagAsync(Guid.NewGuid().ToString());
            result.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task ValidateIdTag_Should_Accept_Registered_RFID()
    {
        var userId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var idTag = new UserIdTag(Guid.NewGuid(), userId, "RFID-VALID-001", IdTagType.Rfid);
            await _dbContext.UserIdTags.AddAsync(idTag);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.ValidateIdTagAsync("RFID-VALID-001");
            result.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task ValidateIdTag_Should_Accept_Inactive_RFID_When_AllowUnregistered()
    {
        // With AllowUnregisteredIdTags=true (default), inactive RFID tags are accepted
        // to allow real chargers to start sessions for billing reconciliation
        await WithUnitOfWorkAsync(async () =>
        {
            var idTag = new UserIdTag(Guid.NewGuid(), Guid.NewGuid(), "RFID-INACTIVE-001", IdTagType.Rfid);
            idTag.Deactivate();
            await _dbContext.UserIdTags.AddAsync(idTag);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.ValidateIdTagAsync("RFID-INACTIVE-001");
            result.ShouldBeTrue(); // Accepted as walk-in when AllowUnregisteredIdTags=true
        });
    }

    [Fact]
    public async Task ValidateIdTag_Should_Accept_TEST_Prefix()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.ValidateIdTagAsync("TEST-12345");
            result.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task ValidateIdTag_Should_Accept_DEMO_Prefix()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.ValidateIdTagAsync("DEMO-CHARGER");
            result.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task ValidateIdTag_Should_Reject_Empty()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.ValidateIdTagAsync("");
            result.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task ValidateIdTag_Should_Accept_Unknown_Tag_When_AllowUnregistered()
    {
        // With AllowUnregisteredIdTags=true (default), unknown RFID tags are accepted
        // to prevent real chargers from stopping mid-charge
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _ocppService.ValidateIdTagAsync("UNKNOWN-TAG-XYZ");
            result.ShouldBeTrue(); // Accepted as walk-in when AllowUnregisteredIdTags=true
        });
    }

    #endregion

    #region HandleStationDisconnectAsync

    [Fact]
    public async Task StationDisconnect_Should_Mark_Orphaned_Sessions_Failed()
    {
        var stationId = Guid.NewGuid();
        var sessionId1 = Guid.NewGuid();
        var sessionId2 = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-DC-001", "DC Test", "Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            // Active session (InProgress)
            var session1 = new ChargingSession(sessionId1, Guid.NewGuid(), stationId, 1);
            session1.RecordStart(1, 0);
            await _dbContext.ChargingSessions.AddAsync(session1);

            // Pending session
            var session2 = new ChargingSession(sessionId2, Guid.NewGuid(), stationId, 2);
            await _dbContext.ChargingSessions.AddAsync(session2);

            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _ocppService.HandleStationDisconnectAsync("OCPP-DC-001");
        });

        _dbContext.ChangeTracker.Clear();

        await WithUnitOfWorkAsync(async () =>
        {
            var s1 = await _dbContext.ChargingSessions.FirstAsync(s => s.Id == sessionId1);
            s1.Status.ShouldBe(SessionStatus.Failed);
            s1.StopReason.ShouldBe("Station disconnected");

            var s2 = await _dbContext.ChargingSessions.FirstAsync(s => s.Id == sessionId2);
            s2.Status.ShouldBe(SessionStatus.Failed);
        });
    }

    [Fact]
    public async Task StationDisconnect_Should_Not_Affect_Completed_Sessions()
    {
        var stationId = Guid.NewGuid();
        var completedSessionId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            var station = new ChargingStation(stationId, "OCPP-DC-002", "DC Test 2", "Test St", 21.0, 105.8);
            await _dbContext.ChargingStations.AddAsync(station);

            var session = new ChargingSession(completedSessionId, Guid.NewGuid(), stationId, 1);
            session.RecordStart(1, 0);
            session.RecordStop(5000, "Normal");
            await _dbContext.ChargingSessions.AddAsync(session);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _ocppService.HandleStationDisconnectAsync("OCPP-DC-002");
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var session = await _dbContext.ChargingSessions.FirstAsync(s => s.Id == completedSessionId);
            session.Status.ShouldBe(SessionStatus.Completed); // Unchanged
        });
    }

    #endregion
}
