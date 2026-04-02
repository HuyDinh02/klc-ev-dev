using System;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Stations;
using Volo.Abp.Domain.Services;

namespace KLC.Ocpp;

/// <summary>
/// Thin facade that delegates OCPP operations to focused handler classes.
/// Implements IOcppService to keep the public contract unchanged.
/// </summary>
public class OcppService : DomainService, IOcppService
{
    private readonly OcppStationHandler _stationHandler;
    private readonly OcppTransactionHandler _transactionHandler;
    private readonly OcppMeterValuesHandler _meterValuesHandler;

    public OcppService(
        OcppStationHandler stationHandler,
        OcppTransactionHandler transactionHandler,
        OcppMeterValuesHandler meterValuesHandler)
    {
        _stationHandler = stationHandler;
        _transactionHandler = transactionHandler;
        _meterValuesHandler = meterValuesHandler;
    }

    public Task<Guid?> HandleBootNotificationAsync(
        string chargePointId, string vendor, string model,
        string? serialNumber, string? firmwareVersion)
        => _stationHandler.HandleBootNotificationAsync(chargePointId, vendor, model, serialNumber, firmwareVersion);

    public Task<StatusNotificationResult?> HandleStatusNotificationAsync(
        string chargePointId, int connectorId, ConnectorStatus status,
        string? errorCode, string? errorInfo = null, string? vendorErrorCode = null)
        => _stationHandler.HandleStatusNotificationAsync(chargePointId, connectorId, status, errorCode, errorInfo, vendorErrorCode);

    public Task HandleHeartbeatAsync(string chargePointId)
        => _stationHandler.HandleHeartbeatAsync(chargePointId);

    public Task<Guid?> HandleStartTransactionAsync(
        string chargePointId, int connectorId, string idTag, int meterStart, int ocppTransactionId)
        => _transactionHandler.HandleStartTransactionAsync(chargePointId, connectorId, idTag, meterStart, ocppTransactionId);

    public Task<StopTransactionResult?> HandleStopTransactionAsync(
        int ocppTransactionId, int meterStop, string? stopReason)
        => _transactionHandler.HandleStopTransactionAsync(ocppTransactionId, meterStop, stopReason);

    public Task<MeterValuesResult?> HandleMeterValuesAsync(
        string chargePointId, int connectorId, int? transactionId,
        decimal energyWh, string? timestamp, decimal? currentAmps,
        decimal? voltage, decimal? power, decimal? soc)
        => _meterValuesHandler.HandleMeterValuesAsync(chargePointId, connectorId, transactionId, energyWh, timestamp, currentAmps, voltage, power, soc);

    public Task<ChargingStation?> GetStationByChargePointIdAsync(string chargePointId)
        => _stationHandler.GetStationByChargePointIdAsync(chargePointId);

    public Task<bool> ValidateIdTagAsync(string idTag)
        => _stationHandler.ValidateIdTagAsync(idTag);

    public Task HandleFirmwareStatusAsync(string chargePointId, string status)
        => _stationHandler.HandleFirmwareStatusAsync(chargePointId, status);

    public Task HandleDiagnosticsStatusAsync(string chargePointId, string status)
        => _stationHandler.HandleDiagnosticsStatusAsync(chargePointId, status);

    public Task HandleStationDisconnectAsync(string chargePointId)
        => _stationHandler.HandleStationDisconnectAsync(chargePointId);
}
