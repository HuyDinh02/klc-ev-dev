using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Hubs;
using KLC.Auditing;
using KLC.Ocpp;
using KLC.Ocpp.Messages;
using KLC.Ocpp.Vendors;
using KLC.Sessions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Settings;
using Xunit;

namespace KLC.EntityFrameworkCore.Ocpp;

/// <summary>
/// Unit tests for OcppMessageHandler message dispatch and response framing.
/// Uses NSubstitute mocks for IOcppService, IMonitoringNotifier, etc.
/// </summary>
public class OcppMessageHandlerTests
{
    private readonly IOcppService _ocppService;
    private readonly IMonitoringNotifier _notifier;
    private readonly OcppMessageHandler _handler;
    private readonly OcppV16MessageParser _parser = new();

    public OcppMessageHandlerTests()
    {
        _ocppService = Substitute.For<IOcppService>();
        _notifier = Substitute.For<IMonitoringNotifier>();

        var connectionManager = new OcppConnectionManager(NullLogger<OcppConnectionManager>.Instance);
        var rawEventRepo = Substitute.For<IRepository<OcppRawEvent, Guid>>();
        var guidGenerator = Substitute.For<IGuidGenerator>();
        guidGenerator.Create().Returns(_ => Guid.NewGuid());

        var genericProfile = new GenericProfile(NullLogger<GenericProfile>.Instance);
        var vendorFactory = new VendorProfileFactory(
            new IVendorProfile[] { genericProfile },
            NullLogger<VendorProfileFactory>.Instance);

        var parserFactory = new OcppMessageParserFactory();

        var auditLogger = Substitute.For<IAuditEventLogger>();
        var settingProvider = Substitute.For<ISettingProvider>();

        _handler = new OcppMessageHandler(
            NullLogger<OcppMessageHandler>.Instance,
            connectionManager,
            _ocppService,
            _notifier,
            vendorFactory,
            rawEventRepo,
            guidGenerator,
            parserFactory,
            auditLogger,
            settingProvider,
            powerBalancingService: null);
    }

    private static OcppConnection CreateConnection(string chargePointId = "TEST-001")
    {
        var ws = Substitute.For<WebSocket>();
        ws.State.Returns(WebSocketState.Open);
        return new OcppConnection(chargePointId, ws, OcppProtocolVersion.Ocpp16J);
    }

    #region BootNotification

    [Fact]
    public async Task BootNotification_Should_Return_Accepted_For_Known_Station()
    {
        var stationId = Guid.NewGuid();
        _ocppService.HandleBootNotificationAsync(
                Arg.Is("TEST-001"), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(stationId);
        _ocppService.GetStationByChargePointIdAsync("TEST-001")
            .Returns((KLC.Stations.ChargingStation?)null);

        var message = """[2,"boot1","BootNotification",{"chargePointVendor":"ABB","chargePointModel":"Terra 54","chargePointSerialNumber":"SN-001","firmwareVersion":"2.0"}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![0].GetInt32().ShouldBe(OcppMessageType.CallResult);
        parsed[1].GetString().ShouldBe("boot1");

        var payload = parsed[2];
        payload.GetProperty("status").GetString().ShouldBe("Accepted");
        payload.TryGetProperty("currentTime", out _).ShouldBeTrue();
        payload.TryGetProperty("interval", out _).ShouldBeTrue();

        connection.IsRegistered.ShouldBeTrue();
        connection.StationId.ShouldBe(stationId);
    }

    [Fact]
    public async Task BootNotification_Should_Return_Rejected_For_Unknown_Station()
    {
        _ocppService.HandleBootNotificationAsync("UNKNOWN-CP", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns((Guid?)null);

        var message = """[2,"boot2","BootNotification",{"chargePointVendor":"Unknown","chargePointModel":"X"}]""";
        var connection = CreateConnection("UNKNOWN-CP");

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![2].GetProperty("status").GetString().ShouldBe("Rejected");

        connection.IsRegistered.ShouldBeFalse();
    }

    #endregion

    #region Heartbeat

    [Fact]
    public async Task Heartbeat_Should_Return_CurrentTime()
    {
        var message = """[2,"hb1","Heartbeat",{}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![0].GetInt32().ShouldBe(OcppMessageType.CallResult);
        parsed[2].TryGetProperty("currentTime", out _).ShouldBeTrue();

        await _ocppService.Received(1).HandleHeartbeatAsync("TEST-001");
    }

    #endregion

    #region StatusNotification

    [Fact]
    public async Task StatusNotification_Should_Dispatch_And_Notify()
    {
        var stationId = Guid.NewGuid();
        _ocppService.HandleStatusNotificationAsync("TEST-001", 1, ConnectorStatus.Available, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(new StatusNotificationResult(ConnectorStatus.Unavailable, ConnectorStatus.Available, stationId));

        var message = """[2,"sn1","StatusNotification",{"connectorId":1,"status":"Available","errorCode":"NoError"}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![0].GetInt32().ShouldBe(OcppMessageType.CallResult);

        await _notifier.Received(1).NotifyConnectorStatusChangedAsync(
            stationId, 1, ConnectorStatus.Unavailable, ConnectorStatus.Available);
    }

    [Fact]
    public async Task StatusNotification_Should_Not_Notify_For_Unknown_Station()
    {
        _ocppService.HandleStatusNotificationAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ConnectorStatus>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns((StatusNotificationResult?)null);

        var message = """[2,"sn2","StatusNotification",{"connectorId":1,"status":"Faulted","errorCode":"GroundFailure"}]""";
        var connection = CreateConnection("UNKNOWN-SN");

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        await _notifier.DidNotReceive().NotifyConnectorStatusChangedAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<ConnectorStatus>(), Arg.Any<ConnectorStatus>());
    }

    #endregion

    #region StartTransaction

    [Fact]
    public async Task StartTransaction_Should_Return_TransactionId_And_Accepted()
    {
        var sessionId = Guid.NewGuid();
        _ocppService.HandleStartTransactionAsync(
                Arg.Is("TEST-001"), Arg.Is(1), Arg.Is("user-123"), Arg.Is(1000), Arg.Any<int>())
            .Returns(sessionId);

        var message = """[2,"st1","StartTransaction",{"connectorId":1,"idTag":"user-123","meterStart":1000,"timestamp":"2026-03-08T10:00:00Z"}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![0].GetInt32().ShouldBe(OcppMessageType.CallResult);

        var payload = parsed[2];
        payload.GetProperty("transactionId").GetInt32().ShouldNotBe(0);
        payload.GetProperty("idTagInfo").GetProperty("status").GetString().ShouldBe("Accepted");
    }

    [Fact]
    public async Task StartTransaction_Should_Return_Invalid_For_Unknown_Station()
    {
        _ocppService.HandleStartTransactionAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((Guid?)null);

        var message = """[2,"st2","StartTransaction",{"connectorId":1,"idTag":"bad-user","meterStart":0,"timestamp":"2026-03-08T10:00:00Z"}]""";
        var connection = CreateConnection("UNKNOWN-ST");

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![2].GetProperty("idTagInfo").GetProperty("status").GetString().ShouldBe("Invalid");
    }

    #endregion

    #region StopTransaction

    [Fact]
    public async Task StopTransaction_Should_Return_Accepted_And_Notify()
    {
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        _ocppService.HandleStopTransactionAsync(Arg.Is(12345), Arg.Is(11000), Arg.Any<string?>())
            .Returns(new StopTransactionResult(sessionId, stationId, 1, 10m, 35000m));

        var message = """[2,"stop1","StopTransaction",{"transactionId":12345,"meterStop":11000,"reason":"EVDisconnected","timestamp":"2026-03-08T11:00:00Z"}]""";
        var connection = CreateConnection();
        connection.SetRegistered(stationId);

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![2].GetProperty("idTagInfo").GetProperty("status").GetString().ShouldBe("Accepted");

        await _notifier.Received(1).NotifySessionUpdatedAsync(
            sessionId, stationId, 1, SessionStatus.Completed, 10m, 35000m);
    }

    [Fact]
    public async Task StopTransaction_Should_Return_Accepted_For_Unknown_Transaction()
    {
        _ocppService.HandleStopTransactionAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns((StopTransactionResult?)null);

        var message = """[2,"stop2","StopTransaction",{"transactionId":99999,"meterStop":5000}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![2].GetProperty("idTagInfo").GetProperty("status").GetString().ShouldBe("Accepted");

        await _notifier.DidNotReceive().NotifySessionUpdatedAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<SessionStatus>(), Arg.Any<decimal>(), Arg.Any<decimal>());
    }

    #endregion

    #region MeterValues

    [Fact]
    public async Task MeterValues_Should_Process_And_Notify()
    {
        var sessionId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        _ocppService.HandleMeterValuesAsync(
                "TEST-001", 1, 12345,
                Arg.Any<decimal>(), Arg.Any<string>(),
                Arg.Any<decimal?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>())
            .Returns(new MeterValuesResult(sessionId, stationId, 1, 5m, 17500m, 7.36m, 45m));

        var message = """[2,"mv1","MeterValues",{"connectorId":1,"transactionId":12345,"meterValue":[{"timestamp":"2026-03-08T10:30:00Z","sampledValue":[{"value":"6000","measurand":"Energy.Active.Import.Register","unit":"Wh"},{"value":"32","measurand":"Current.Import"},{"value":"230","measurand":"Voltage"},{"value":"7360","measurand":"Power.Active.Import","unit":"W"},{"value":"45","measurand":"SoC"}]}]}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![0].GetInt32().ShouldBe(OcppMessageType.CallResult);

        await _notifier.Received(1).NotifySessionUpdatedAsync(
            sessionId, stationId, 1, SessionStatus.InProgress, 5m, 17500m);
        await _notifier.Received(1).NotifyMeterValueReceivedAsync(
            sessionId, 5m, 7.36m, 45m);
    }

    [Fact]
    public async Task MeterValues_Should_Handle_Empty_MeterValue_Array()
    {
        var message = """[2,"mv2","MeterValues",{"connectorId":1,"transactionId":12345,"meterValue":[]}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        // Should return empty CallResult without errors
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![0].GetInt32().ShouldBe(OcppMessageType.CallResult);
    }

    #endregion

    #region Authorize

    [Fact]
    public async Task Authorize_Should_Return_Accepted_For_Valid_Tag()
    {
        _ocppService.ValidateIdTagAsync("VALID-TAG").Returns(true);

        var message = """[2,"auth1","Authorize",{"idTag":"VALID-TAG"}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![2].GetProperty("idTagInfo").GetProperty("status").GetString().ShouldBe("Accepted");
    }

    [Fact]
    public async Task Authorize_Should_Return_Invalid_For_Bad_Tag()
    {
        _ocppService.ValidateIdTagAsync("BAD-TAG").Returns(false);

        var message = """[2,"auth2","Authorize",{"idTag":"BAD-TAG"}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![2].GetProperty("idTagInfo").GetProperty("status").GetString().ShouldBe("Invalid");
    }

    #endregion

    #region DataTransfer

    [Fact]
    public async Task DataTransfer_Should_Return_Accepted()
    {
        var message = """[2,"dt1","DataTransfer",{"vendorId":"TestVendor","data":"custom payload"}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![2].GetProperty("status").GetString().ShouldBe("Accepted");
    }

    #endregion

    #region Unknown Action

    [Fact]
    public async Task Unknown_Action_Should_Return_NotImplemented_Error()
    {
        var message = """[2,"unk1","CustomAction",{}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldNotBeNull();
        var parsed = JsonSerializer.Deserialize<JsonElement[]>(response!);
        parsed![0].GetInt32().ShouldBe(OcppMessageType.CallError);
        parsed[2].GetString().ShouldBe("NotImplemented");
    }

    #endregion

    #region CallResult / CallError Handling

    [Fact]
    public async Task CallResult_Should_Return_Null()
    {
        var message = """[3,"pending1",{"status":"Accepted"}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldBeNull();
    }

    [Fact]
    public async Task CallError_Should_Return_Null()
    {
        var message = """[4,"pending2","InternalError","Something failed",{}]""";
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, message);

        response.ShouldBeNull();
    }

    #endregion

    #region Malformed Messages

    [Fact]
    public async Task Malformed_Json_Should_Return_Null()
    {
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, "not valid json");

        response.ShouldBeNull();
    }

    [Fact]
    public async Task Too_Short_Array_Should_Return_Null()
    {
        var connection = CreateConnection();

        var response = await _handler.HandleMessageAsync(connection, """[2]""");

        response.ShouldBeNull();
    }

    #endregion
}
