using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KLC.Ocpp;

/// <summary>
/// Result from a remote OCPP command.
/// </summary>
public record RemoteCommandResult(bool Accepted, string? ErrorMessage = null);

/// <summary>
/// Result from GetConfiguration command.
/// </summary>
public record ConfigurationResult(
    bool Accepted,
    List<ConfigurationEntry>? ConfigurationKey = null,
    List<string>? UnknownKey = null);

public record ConfigurationEntry(string Key, string? Value, bool Readonly);

/// <summary>
/// Result from GetLocalListVersion command.
/// </summary>
public record LocalListVersionResult(bool Accepted, int ListVersion = -1);

/// <summary>
/// Result from SendLocalList command.
/// </summary>
public record SendLocalListResult(bool Accepted, string? Status = null, string? ErrorMessage = null);

/// <summary>
/// An entry in the OCPP Local Authorization List.
/// </summary>
public record LocalAuthEntry(string IdTag, IdTagInfoPayload? IdTagInfo = null);

/// <summary>
/// idTagInfo payload within a Local Authorization List entry.
/// </summary>
public record IdTagInfoPayload(string Status, string? ExpiryDate = null, string? ParentIdTag = null);

/// <summary>
/// Service for sending remote OCPP commands to Charge Points.
/// Implemented in the Host layer where WebSocket connections are managed.
/// </summary>
public interface IOcppRemoteCommandService
{
    /// <summary>
    /// Send RemoteStartTransaction to a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendRemoteStartTransactionAsync(string stationCode, int connectorId, string idTag);

    /// <summary>
    /// Send RemoteStopTransaction to a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendRemoteStopTransactionAsync(string stationCode, int transactionId);

    /// <summary>
    /// Send Reset (Soft or Hard) to a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendResetAsync(string stationCode, string resetType);

    /// <summary>
    /// Send UnlockConnector to a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendUnlockConnectorAsync(string stationCode, int connectorId);

    /// <summary>
    /// Send ChangeAvailability (Operative/Inoperative) to a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendChangeAvailabilityAsync(string stationCode, int connectorId, string availabilityType);

    /// <summary>
    /// Send GetConfiguration to read charger settings.
    /// </summary>
    Task<ConfigurationResult> SendGetConfigurationAsync(string stationCode, List<string>? keys = null);

    /// <summary>
    /// Send ChangeConfiguration to update a charger setting.
    /// </summary>
    Task<RemoteCommandResult> SendChangeConfigurationAsync(string stationCode, string key, string value);

    /// <summary>
    /// Send TriggerMessage to request a specific message from the Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendTriggerMessageAsync(string stationCode, string requestedMessage, int? connectorId = null);

    /// <summary>
    /// Send SetChargingProfile to apply a charging profile to a connector.
    /// </summary>
    Task<RemoteCommandResult> SendSetChargingProfileAsync(string stationCode, int connectorId, ChargingProfilePayload profile);

    /// <summary>
    /// Send ClearChargingProfile to remove charging profile(s) from a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendClearChargingProfileAsync(string stationCode, int? id = null, int? connectorId = null, string? chargingProfilePurpose = null, int? stackLevel = null);

    /// <summary>
    /// Send UpdateFirmware to instruct the Charge Point to download and install new firmware.
    /// </summary>
    Task<RemoteCommandResult> SendUpdateFirmwareAsync(string stationCode, string location, DateTime retrieveDate, int? retries = null, int? retryInterval = null);

    /// <summary>
    /// Send GetDiagnostics to instruct the Charge Point to upload diagnostics.
    /// </summary>
    Task<RemoteCommandResult> SendGetDiagnosticsAsync(string stationCode, string location, DateTime? startTime = null, DateTime? stopTime = null, int? retries = null, int? retryInterval = null);

    /// <summary>
    /// Send GetLocalListVersion to retrieve the current local authorization list version.
    /// </summary>
    Task<LocalListVersionResult> SendGetLocalListVersionAsync(string stationCode);

    /// <summary>
    /// Send SendLocalList to push a local authorization list to the Charge Point.
    /// </summary>
    Task<SendLocalListResult> SendSendLocalListAsync(string stationCode, int listVersion, string updateType, List<LocalAuthEntry>? localAuthorizationList = null);

    /// <summary>
    /// Send ReserveNow to reserve a connector on a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendReserveNowAsync(string stationCode, int connectorId, DateTime expiryDate, string idTag, int reservationId);

    /// <summary>
    /// Send CancelReservation to cancel an existing reservation on a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendCancelReservationAsync(string stationCode, int reservationId);

    /// <summary>
    /// Send DataTransfer to a Charge Point with vendor-specific data.
    /// </summary>
    Task<RemoteCommandResult> SendDataTransferAsync(string stationCode, string vendorId, string? messageId, string? data);
}

/// <summary>
/// Represents an OCPP 1.6 ChargingProfile payload.
/// </summary>
public record ChargingProfilePayload(
    int ChargingProfileId,
    int? TransactionId,
    int StackLevel,
    string ChargingProfilePurpose,
    string ChargingProfileKind,
    ChargingSchedulePayload ChargingSchedule);

/// <summary>
/// Represents a ChargingSchedule within a ChargingProfile.
/// </summary>
public record ChargingSchedulePayload(
    string ChargingRateUnit,
    List<ChargingSchedulePeriodPayload> ChargingSchedulePeriod);

/// <summary>
/// Represents a single period within a ChargingSchedule.
/// </summary>
public record ChargingSchedulePeriodPayload(
    int StartPeriod,
    decimal Limit,
    int? NumberPhases = null);
