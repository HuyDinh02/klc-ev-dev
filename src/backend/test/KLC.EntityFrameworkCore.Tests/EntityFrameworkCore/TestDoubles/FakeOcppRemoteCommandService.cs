using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Ocpp;

namespace KLC.TestDoubles;

public class FakeOcppRemoteCommandService : IOcppRemoteCommandService
{
    public List<(string StationCode, int ConnectorId, string IdTag)> RemoteStartCalls { get; } = [];
    public List<(string StationCode, int TransactionId)> RemoteStopCalls { get; } = [];

    public RemoteCommandResult RemoteStartResult { get; set; } = new(true);
    public RemoteCommandResult RemoteStopResult { get; set; } = new(true);

    public Func<string, int, string, Task<RemoteCommandResult>>? RemoteStartHandler { get; set; }
    public Func<string, int, Task<RemoteCommandResult>>? RemoteStopHandler { get; set; }

    public void Reset()
    {
        RemoteStartCalls.Clear();
        RemoteStopCalls.Clear();
        RemoteStartResult = new RemoteCommandResult(true);
        RemoteStopResult = new RemoteCommandResult(true);
        RemoteStartHandler = null;
        RemoteStopHandler = null;
    }

    public async Task<RemoteCommandResult> SendRemoteStartTransactionAsync(string stationCode, int connectorId, string idTag)
    {
        RemoteStartCalls.Add((stationCode, connectorId, idTag));
        if (RemoteStartHandler != null)
        {
            return await RemoteStartHandler(stationCode, connectorId, idTag);
        }

        return RemoteStartResult;
    }

    public async Task<RemoteCommandResult> SendRemoteStopTransactionAsync(string stationCode, int transactionId)
    {
        RemoteStopCalls.Add((stationCode, transactionId));
        if (RemoteStopHandler != null)
        {
            return await RemoteStopHandler(stationCode, transactionId);
        }

        return RemoteStopResult;
    }

    public Task<RemoteCommandResult> SendResetAsync(string stationCode, string resetType) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<RemoteCommandResult> SendUnlockConnectorAsync(string stationCode, int connectorId) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<RemoteCommandResult> SendChangeAvailabilityAsync(string stationCode, int connectorId, string availabilityType) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<ConfigurationResult> SendGetConfigurationAsync(string stationCode, List<string>? keys = null) =>
        Task.FromResult(new ConfigurationResult(true, [], []));

    public Task<RemoteCommandResult> SendChangeConfigurationAsync(string stationCode, string key, string value) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<RemoteCommandResult> SendTriggerMessageAsync(string stationCode, string requestedMessage, int? connectorId = null) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<RemoteCommandResult> SendSetChargingProfileAsync(string stationCode, int connectorId, ChargingProfilePayload profile) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<RemoteCommandResult> SendClearChargingProfileAsync(string stationCode, int? id = null, int? connectorId = null, string? chargingProfilePurpose = null, int? stackLevel = null) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<RemoteCommandResult> SendUpdateFirmwareAsync(string stationCode, string location, DateTime retrieveDate, int? retries = null, int? retryInterval = null) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<RemoteCommandResult> SendGetDiagnosticsAsync(string stationCode, string location, DateTime? startTime = null, DateTime? stopTime = null, int? retries = null, int? retryInterval = null) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<LocalListVersionResult> SendGetLocalListVersionAsync(string stationCode) =>
        Task.FromResult(new LocalListVersionResult(true, 1));

    public Task<SendLocalListResult> SendSendLocalListAsync(string stationCode, int listVersion, string updateType, List<LocalAuthEntry>? localAuthorizationList = null) =>
        Task.FromResult(new SendLocalListResult(true, "Accepted"));

    public Task<RemoteCommandResult> SendReserveNowAsync(string stationCode, int connectorId, DateTime expiryDate, string idTag, int reservationId) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<RemoteCommandResult> SendCancelReservationAsync(string stationCode, int reservationId) =>
        Task.FromResult(new RemoteCommandResult(true));

    public Task<RemoteCommandResult> SendDataTransferAsync(string stationCode, string vendorId, string? messageId, string? data) =>
        Task.FromResult(new RemoteCommandResult(true));
}
