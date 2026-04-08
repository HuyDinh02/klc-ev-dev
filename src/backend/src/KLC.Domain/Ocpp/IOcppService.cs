using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Stations;

namespace KLC.Ocpp;

/// <summary>
/// Result data returned from StopTransaction handling.
/// </summary>
public record StopTransactionResult(
    Guid SessionId,
    Guid UserId,
    Guid StationId,
    int ConnectorNumber,
    decimal TotalEnergyKwh,
    decimal TotalCost);

/// <summary>
/// Result data returned from MeterValues handling.
/// </summary>
public record MeterValuesResult(
    Guid SessionId,
    Guid StationId,
    int ConnectorNumber,
    decimal TotalEnergyKwh,
    decimal TotalCost,
    decimal? PowerKw,
    decimal? SocPercent);

/// <summary>
/// Result data returned from StatusNotification handling.
/// </summary>
public record StatusNotificationResult(
    ConnectorStatus PreviousStatus,
    ConnectorStatus NewStatus,
    Guid StationId);

/// <summary>
/// Domain service for OCPP-related operations.
/// Handles persistence of OCPP messages to the database.
/// </summary>
public interface IOcppService
{
    /// <summary>
    /// Handles BootNotification - creates or updates station info.
    /// Returns the station ID if found/created.
    /// </summary>
    Task<Guid?> HandleBootNotificationAsync(
        string chargePointId,
        string vendor,
        string model,
        string? serialNumber,
        string? firmwareVersion);

    /// <summary>
    /// Handles StatusNotification - updates connector status.
    /// Returns the previous and new status for SignalR notifications.
    /// </summary>
    Task<StatusNotificationResult?> HandleStatusNotificationAsync(
        string chargePointId,
        int connectorId,
        ConnectorStatus status,
        string? errorCode,
        string? errorInfo = null,
        string? vendorErrorCode = null);

    /// <summary>
    /// Handles Heartbeat - records heartbeat time.
    /// </summary>
    Task HandleHeartbeatAsync(string chargePointId);

    /// <summary>
    /// Handles StartTransaction - creates a charging session.
    /// Returns the session ID.
    /// </summary>
    Task<Guid?> HandleStartTransactionAsync(
        string chargePointId,
        int connectorId,
        string idTag,
        int meterStart,
        int ocppTransactionId);

    /// <summary>
    /// Handles StopTransaction - completes the charging session.
    /// Returns session data for SignalR notifications.
    /// </summary>
    Task<StopTransactionResult?> HandleStopTransactionAsync(
        int ocppTransactionId,
        int meterStop,
        string? stopReason);

    /// <summary>
    /// Handles MeterValues - stores meter readings.
    /// Returns session data for SignalR notifications.
    /// </summary>
    Task<MeterValuesResult?> HandleMeterValuesAsync(
        string chargePointId,
        int connectorId,
        int? transactionId,
        decimal energyWh,
        string? timestamp,
        decimal? currentAmps,
        decimal? voltage,
        decimal? power,
        decimal? soc);

    /// <summary>
    /// Gets a station by its charge point ID (station code).
    /// </summary>
    Task<ChargingStation?> GetStationByChargePointIdAsync(string chargePointId);

    /// <summary>
    /// Validates an OCPP idTag.
    /// Returns true if the idTag is a valid user GUID or registered RFID tag.
    /// </summary>
    Task<bool> ValidateIdTagAsync(string idTag);

    /// <summary>
    /// Handles station disconnect - marks orphaned sessions as failed.
    /// </summary>
    Task HandleStationDisconnectAsync(string chargePointId);

    /// <summary>
    /// Handles FirmwareStatusNotification - updates station firmware update status.
    /// </summary>
    Task HandleFirmwareStatusAsync(string chargePointId, string status);

    /// <summary>
    /// Handles DiagnosticsStatusNotification - updates station diagnostics upload status.
    /// </summary>
    Task HandleDiagnosticsStatusAsync(string chargePointId, string status);

    /// <summary>
    /// Gets the active InProgress session for a specific connector.
    /// Used for auto-stop when connector goes Available/Finishing during active session.
    /// </summary>
    Task<Sessions.ChargingSession?> GetActiveSessionForConnectorAsync(string chargePointId, int connectorNumber);

    /// <summary>
    /// Gets all stations currently marked Online in the database.
    /// Used by the connection status endpoint to support multi-instance deployments where
    /// the HTTP request may land on a different Cloud Run instance than the one holding
    /// a charger's WebSocket connection.
    /// </summary>
    Task<IList<Stations.ChargingStation>> GetOnlineStationsAsync();
}
