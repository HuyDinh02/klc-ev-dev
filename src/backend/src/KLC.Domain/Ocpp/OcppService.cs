using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Sessions;
using KLC.Stations;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace KLC.Ocpp;

/// <summary>
/// Domain service implementation for OCPP operations.
/// </summary>
public class OcppService : DomainService, IOcppService
{
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<MeterValue, Guid> _meterValueRepository;
    private readonly ILogger<OcppService> _logger;

    public OcppService(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<MeterValue, Guid> meterValueRepository,
        ILogger<OcppService> logger)
    {
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _sessionRepository = sessionRepository;
        _meterValueRepository = meterValueRepository;
        _logger = logger;
    }

    public async Task<Guid?> HandleBootNotificationAsync(
        string chargePointId,
        string vendor,
        string model,
        string? serialNumber,
        string? firmwareVersion)
    {
        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);

        if (station == null)
        {
            _logger.LogWarning("BootNotification from unknown station: {ChargePointId}", chargePointId);
            return null;
        }

        station.SetStationInfo(vendor, model, serialNumber, firmwareVersion);
        station.RecordHeartbeat();

        await _stationRepository.UpdateAsync(station);

        _logger.LogInformation("Station {StationCode} updated: Vendor={Vendor}, Model={Model}, FW={Firmware}",
            chargePointId, vendor, model, firmwareVersion);

        return station.Id;
    }

    public async Task HandleStatusNotificationAsync(
        string chargePointId,
        int connectorId,
        ConnectorStatus status,
        string? errorCode)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = (await AsyncExecuter.ToListAsync(query.Where(s => s.StationCode == chargePointId))).FirstOrDefault();

        if (station == null)
        {
            _logger.LogWarning("StatusNotification from unknown station: {ChargePointId}", chargePointId);
            return;
        }

        // ConnectorId 0 means the station itself
        if (connectorId == 0)
        {
            // Map connector status to station status
            var stationStatus = status switch
            {
                ConnectorStatus.Available => StationStatus.Available,
                ConnectorStatus.Faulted => StationStatus.Faulted,
                ConnectorStatus.Unavailable => StationStatus.Unavailable,
                _ => StationStatus.Available
            };
            station.UpdateStatus(stationStatus);
            await _stationRepository.UpdateAsync(station);
        }
        else
        {
            var connector = station.Connectors.FirstOrDefault(c => c.ConnectorNumber == connectorId);
            if (connector != null)
            {
                connector.UpdateStatus(status);
                await _connectorRepository.UpdateAsync(connector);
            }
            else
            {
                _logger.LogWarning("StatusNotification for unknown connector {ConnectorId} on station {ChargePointId}",
                    connectorId, chargePointId);
            }
        }

        _logger.LogInformation("Status updated for {ChargePointId} connector {ConnectorId}: {Status}",
            chargePointId, connectorId, status);
    }

    public async Task HandleHeartbeatAsync(string chargePointId)
    {
        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);

        if (station == null)
        {
            _logger.LogWarning("Heartbeat from unknown station: {ChargePointId}", chargePointId);
            return;
        }

        station.RecordHeartbeat();
        await _stationRepository.UpdateAsync(station);
    }

    public async Task<Guid?> HandleStartTransactionAsync(
        string chargePointId,
        int connectorId,
        string idTag,
        int meterStart,
        int ocppTransactionId)
    {
        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);

        if (station == null)
        {
            _logger.LogWarning("StartTransaction from unknown station: {ChargePointId}", chargePointId);
            return null;
        }

        // Check for existing session with same transaction ID (idempotency)
        var existingSession = await _sessionRepository.FirstOrDefaultAsync(
            s => s.OcppTransactionId == ocppTransactionId);
        if (existingSession != null)
        {
            _logger.LogInformation("Duplicate StartTransaction {TransactionId}, returning existing session",
                ocppTransactionId);
            return existingSession.Id;
        }

        // Get tariff plan from station if available
        var tariffPlanId = station.TariffPlanId;

        // Create new session
        // Note: We use a system user ID for OCPP-initiated sessions
        // In production, this would be linked to the idTag owner
        var sessionId = GuidGenerator.Create();
        var session = new ChargingSession(
            sessionId,
            Guid.Empty, // System user - should be resolved from idTag
            station.Id,
            connectorId,
            null, // Vehicle ID - could be resolved from idTag
            tariffPlanId,
            0, // Rate per kWh - should come from tariff
            idTag
        );

        session.RecordStart(ocppTransactionId, meterStart);

        await _sessionRepository.InsertAsync(session);

        _logger.LogInformation("Session {SessionId} started for transaction {TransactionId}",
            sessionId, ocppTransactionId);

        return sessionId;
    }

    public async Task HandleStopTransactionAsync(
        int ocppTransactionId,
        int meterStop,
        string? stopReason)
    {
        var session = await _sessionRepository.FirstOrDefaultAsync(
            s => s.OcppTransactionId == ocppTransactionId);

        if (session == null)
        {
            _logger.LogWarning("StopTransaction for unknown transaction: {TransactionId}", ocppTransactionId);
            return;
        }

        if (session.Status == SessionStatus.Completed)
        {
            _logger.LogInformation("Duplicate StopTransaction {TransactionId}, session already completed",
                ocppTransactionId);
            return;
        }

        session.RecordStop(meterStop, stopReason);

        await _sessionRepository.UpdateAsync(session);

        _logger.LogInformation("Session {SessionId} completed: Energy={EnergyKwh}kWh, Cost={Cost}",
            session.Id, session.TotalEnergyKwh, session.TotalCost);
    }

    public async Task HandleMeterValuesAsync(
        string chargePointId,
        int connectorId,
        int? transactionId,
        decimal energyWh,
        decimal? currentAmps,
        decimal? voltage,
        decimal? power,
        decimal? soc)
    {
        ChargingSession? session = null;

        if (transactionId.HasValue)
        {
            session = await _sessionRepository.FirstOrDefaultAsync(
                s => s.OcppTransactionId == transactionId.Value);
        }

        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
        if (station == null)
        {
            _logger.LogWarning("MeterValues from unknown station: {ChargePointId}", chargePointId);
            return;
        }

        // Convert Wh to kWh
        var energyKwh = Math.Round(energyWh / 1000m, 3);

        if (session != null)
        {
            // Add meter value to session
            var meterValue = session.AddMeterValue(
                GuidGenerator.Create(),
                energyKwh,
                currentAmps,
                voltage,
                power != null ? power / 1000m : null, // Convert W to kW
                soc
            );

            await _sessionRepository.UpdateAsync(session);

            _logger.LogDebug("MeterValue recorded for session {SessionId}: {EnergyKwh}kWh",
                session.Id, energyKwh);
        }
        else
        {
            // Store standalone meter value (no active session)
            _logger.LogDebug("MeterValue received without active session for {ChargePointId}", chargePointId);
        }
    }

    public async Task<ChargingStation?> GetStationByChargePointIdAsync(string chargePointId)
    {
        return await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
    }
}
