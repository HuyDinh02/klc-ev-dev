using System;
using System.Threading.Tasks;
using KCharge.Enums;
using KCharge.Stations;

namespace KCharge.Ocpp;

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
    /// </summary>
    Task HandleStatusNotificationAsync(
        string chargePointId,
        int connectorId,
        ConnectorStatus status,
        string? errorCode);

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
    /// </summary>
    Task HandleStopTransactionAsync(
        int ocppTransactionId,
        int meterStop,
        string? stopReason);

    /// <summary>
    /// Handles MeterValues - stores meter readings.
    /// </summary>
    Task HandleMeterValuesAsync(
        string chargePointId,
        int connectorId,
        int? transactionId,
        decimal energyWh,
        decimal? currentAmps,
        decimal? voltage,
        decimal? power,
        decimal? soc);

    /// <summary>
    /// Gets a station by its charge point ID (station code).
    /// </summary>
    Task<ChargingStation?> GetStationByChargePointIdAsync(string chargePointId);
}
