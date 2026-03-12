using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Faults;
using KLC.Fleets;
using KLC.Sessions;
using KLC.Stations;
using KLC.Tariffs;
using KLC.Users;
using KLC.Vehicles;
using Microsoft.Extensions.Configuration;
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
    private readonly IRepository<TariffPlan, Guid> _tariffPlanRepository;
    private readonly IRepository<UserIdTag, Guid> _userIdTagRepository;
    private readonly IRepository<Fault, Guid> _faultRepository;
    private readonly IRepository<Vehicle, Guid> _vehicleRepository;
    private readonly IRepository<FleetVehicle, Guid> _fleetVehicleRepository;
    private readonly IRepository<Fleet, Guid> _fleetRepository;
    private readonly IFleetChargingPolicyService _fleetChargingPolicyService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcppService> _logger;

    public OcppService(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<MeterValue, Guid> meterValueRepository,
        IRepository<TariffPlan, Guid> tariffPlanRepository,
        IRepository<UserIdTag, Guid> userIdTagRepository,
        IRepository<Fault, Guid> faultRepository,
        IRepository<Vehicle, Guid> vehicleRepository,
        IRepository<FleetVehicle, Guid> fleetVehicleRepository,
        IRepository<Fleet, Guid> fleetRepository,
        IFleetChargingPolicyService fleetChargingPolicyService,
        IConfiguration configuration,
        ILogger<OcppService> logger)
    {
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _sessionRepository = sessionRepository;
        _meterValueRepository = meterValueRepository;
        _tariffPlanRepository = tariffPlanRepository;
        _userIdTagRepository = userIdTagRepository;
        _faultRepository = faultRepository;
        _vehicleRepository = vehicleRepository;
        _fleetVehicleRepository = fleetVehicleRepository;
        _fleetRepository = fleetRepository;
        _fleetChargingPolicyService = fleetChargingPolicyService;
        _configuration = configuration;
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

    public async Task<StatusNotificationResult?> HandleStatusNotificationAsync(
        string chargePointId,
        int connectorId,
        ConnectorStatus status,
        string? errorCode,
        string? errorInfo = null,
        string? vendorErrorCode = null)
    {
        var query = await _stationRepository.WithDetailsAsync(s => s.Connectors);
        var station = (await AsyncExecuter.ToListAsync(query.Where(s => s.StationCode == chargePointId))).FirstOrDefault();

        if (station == null)
        {
            _logger.LogWarning("StatusNotification from unknown station: {ChargePointId}", chargePointId);
            return null;
        }

        ConnectorStatus previousStatus = ConnectorStatus.Available;

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
                previousStatus = connector.Status;
                connector.UpdateStatus(status);
                await _connectorRepository.UpdateAsync(connector);
            }
            else
            {
                _logger.LogWarning("StatusNotification for unknown connector {ConnectorId} on station {ChargePointId}",
                    connectorId, chargePointId);
                return null;
            }
        }

        // Escalate error codes to Fault entities
        if (!string.IsNullOrEmpty(errorCode) &&
            !string.Equals(errorCode, "NoError", StringComparison.OrdinalIgnoreCase))
        {
            // Deduplicate: only create if no Open/Investigating fault for same station+connector+errorCode
            int? connNum = connectorId > 0 ? connectorId : null;
            var existingFault = await _faultRepository.FirstOrDefaultAsync(
                f => f.StationId == station.Id
                    && f.ConnectorNumber == connNum
                    && f.ErrorCode == errorCode
                    && (f.Status == FaultStatus.Open || f.Status == FaultStatus.Investigating));

            if (existingFault == null)
            {
                var fault = new Fault(
                    GuidGenerator.Create(),
                    station.Id,
                    connNum,
                    errorCode,
                    errorInfo,
                    vendorErrorCode);

                await _faultRepository.InsertAsync(fault);

                _logger.LogWarning(
                    "Fault created for {ChargePointId} connector {ConnectorId}: {ErrorCode} (Priority={Priority})",
                    chargePointId, connectorId, errorCode, fault.Priority);
            }
        }
        // Auto-resolve faults when error clears
        else if (string.Equals(errorCode, "NoError", StringComparison.OrdinalIgnoreCase))
        {
            int? connNum = connectorId > 0 ? connectorId : null;
            var openFaults = await AsyncExecuter.ToListAsync(
                (await _faultRepository.GetQueryableAsync())
                    .Where(f => f.StationId == station.Id
                        && f.ConnectorNumber == connNum
                        && (f.Status == FaultStatus.Open || f.Status == FaultStatus.Investigating)));

            foreach (var fault in openFaults)
            {
                fault.Close("Auto-resolved: charger reported NoError");
                await _faultRepository.UpdateAsync(fault);

                _logger.LogInformation("Fault {FaultId} auto-resolved for {ChargePointId} connector {ConnectorId}",
                    fault.Id, chargePointId, connectorId);
            }
        }

        _logger.LogInformation("Status updated for {ChargePointId} connector {ConnectorId}: {PreviousStatus} -> {Status}",
            chargePointId, connectorId, previousStatus, status);

        return new StatusNotificationResult(previousStatus, status, station.Id);
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

        // Resolve userId from idTag
        var userId = Guid.Empty;
        if (Guid.TryParse(idTag, out var parsedUserId) && parsedUserId != Guid.Empty)
        {
            // Mobile app sends userId as idTag
            userId = parsedUserId;
        }
        else
        {
            // Look up RFID/physical tag in UserIdTag registry
            var userIdTag = await _userIdTagRepository.FirstOrDefaultAsync(
                t => t.IdTag == idTag && t.IsActive);
            if (userIdTag != null && userIdTag.IsValid())
            {
                userId = userIdTag.UserId;
                _logger.LogInformation("Resolved idTag {IdTag} to user {UserId}", idTag, userId);
            }
        }

        // Reject transaction if idTag could not be resolved to a valid user
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("StartTransaction rejected: idTag {IdTag} could not be resolved to a valid user", idTag);
            return null;
        }

        // Resolve user's default vehicle for fleet policy validation
        Guid? vehicleId = null;
        var defaultVehicle = await _vehicleRepository.FirstOrDefaultAsync(
            v => v.UserId == userId && v.IsDefault && v.IsActive);
        if (defaultVehicle != null)
        {
            vehicleId = defaultVehicle.Id;

            // Validate fleet charging policy
            var policyResult = await _fleetChargingPolicyService.ValidateChargingAsync(defaultVehicle.Id, station.Id);
            if (!policyResult.Allowed)
            {
                _logger.LogWarning(
                    "StartTransaction rejected by fleet policy: idTag={IdTag}, vehicleId={VehicleId}, reason={Reason}",
                    idTag, defaultVehicle.Id, policyResult.DenialReason);
                return null;
            }
        }

        // Resolve tariff rate from station's tariff plan
        var tariffPlanId = station.TariffPlanId;
        decimal ratePerKwh = 0;
        if (tariffPlanId.HasValue)
        {
            var tariffPlan = await _tariffPlanRepository.FirstOrDefaultAsync(t => t.Id == tariffPlanId.Value);
            if (tariffPlan != null && tariffPlan.IsCurrentlyEffective())
            {
                ratePerKwh = tariffPlan.GetTotalRatePerKwh();
            }
        }

        var sessionId = GuidGenerator.Create();
        var session = new ChargingSession(
            sessionId,
            userId,
            station.Id,
            connectorId,
            vehicleId,
            tariffPlanId,
            ratePerKwh,
            idTag
        );

        session.RecordStart(ocppTransactionId, meterStart);

        await _sessionRepository.InsertAsync(session);

        _logger.LogInformation("Session {SessionId} started for transaction {TransactionId}",
            sessionId, ocppTransactionId);

        return sessionId;
    }

    public async Task<StopTransactionResult?> HandleStopTransactionAsync(
        int ocppTransactionId,
        int meterStop,
        string? stopReason)
    {
        // Load session with MeterValues for TOU calculation
        var query = await _sessionRepository.WithDetailsAsync(s => s.MeterValues);
        var session = (await AsyncExecuter.ToListAsync(
            query.Where(s => s.OcppTransactionId == ocppTransactionId))).FirstOrDefault();

        if (session == null)
        {
            _logger.LogWarning("StopTransaction for unknown transaction: {TransactionId}", ocppTransactionId);
            return null;
        }

        if (session.Status == SessionStatus.Completed)
        {
            _logger.LogInformation("Duplicate StopTransaction {TransactionId}, session already completed",
                ocppTransactionId);
            return null;
        }

        // Resolve tariff plan for TOU calculation
        Tariffs.TariffPlan? tariffPlan = null;
        if (session.TariffPlanId.HasValue)
        {
            tariffPlan = await _tariffPlanRepository.FirstOrDefaultAsync(
                t => t.Id == session.TariffPlanId.Value);
        }

        session.RecordStop(meterStop, stopReason, tariffPlan);

        await _sessionRepository.UpdateAsync(session);

        // Record fleet energy consumption and spending after session completion
        if (session.VehicleId.HasValue)
        {
            var fleetVehicle = await _fleetVehicleRepository.FirstOrDefaultAsync(
                fv => fv.VehicleId == session.VehicleId.Value && fv.IsActive);
            if (fleetVehicle != null)
            {
                fleetVehicle.RecordEnergy(session.TotalEnergyKwh);
                await _fleetVehicleRepository.UpdateAsync(fleetVehicle);

                var fleet = await _fleetRepository.FirstOrDefaultAsync(f => f.Id == fleetVehicle.FleetId);
                if (fleet != null)
                {
                    fleet.RecordSpending(session.TotalCost);
                    await _fleetRepository.UpdateAsync(fleet);

                    _logger.LogInformation(
                        "Fleet {FleetId} recorded: Energy={EnergyKwh}kWh on vehicle {VehicleId}, Spending={Cost}VND",
                        fleet.Id, session.TotalEnergyKwh, session.VehicleId, session.TotalCost);
                }
            }
        }

        _logger.LogInformation("Session {SessionId} completed: Energy={EnergyKwh}kWh, Cost={Cost}, TariffType={TariffType}",
            session.Id, session.TotalEnergyKwh, session.TotalCost,
            tariffPlan?.TariffType.ToString() ?? "None");

        return new StopTransactionResult(
            session.Id,
            session.StationId,
            session.ConnectorNumber,
            session.TotalEnergyKwh,
            session.TotalCost);
    }

    public async Task<MeterValuesResult?> HandleMeterValuesAsync(
        string chargePointId,
        int connectorId,
        int? transactionId,
        decimal energyWh,
        string? timestamp,
        decimal? currentAmps,
        decimal? voltage,
        decimal? power,
        decimal? soc)
    {
        ChargingSession? session = null;

        if (transactionId.HasValue)
        {
            // Use WithDetailsAsync to load MeterValues navigation property
            var query = await _sessionRepository.WithDetailsAsync(s => s.MeterValues);
            session = (await AsyncExecuter.ToListAsync(
                query.Where(s => s.OcppTransactionId == transactionId.Value))).FirstOrDefault();
        }

        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
        if (station == null)
        {
            _logger.LogWarning("MeterValues from unknown station: {ChargePointId}", chargePointId);
            return null;
        }

        // Convert Wh to kWh
        var energyKwh = Math.Round(energyWh / 1000m, 3);

        // Parse OCPP timestamp, fall back to UTC now
        var meterTimestamp = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(timestamp) && DateTime.TryParse(timestamp, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
        {
            meterTimestamp = parsed.ToUniversalTime();
        }

        if (session != null)
        {
            // Monotonic validation: reject backward readings
            if (session.MeterStart.HasValue && energyWh < session.MeterStart.Value)
            {
                _logger.LogWarning(
                    "Rejected MeterValue for session {SessionId}: energyWh {EnergyWh} < MeterStart {MeterStart}",
                    session.Id, energyWh, session.MeterStart.Value);
                return null;
            }

            // Monotonic validation: reject non-monotonic readings
            var lastReading = session.MeterValues
                .OrderByDescending(mv => mv.Timestamp)
                .FirstOrDefault();
            if (lastReading != null && energyKwh < lastReading.EnergyKwh)
            {
                _logger.LogWarning(
                    "Rejected non-monotonic MeterValue for session {SessionId}: {EnergyKwh}kWh < last {LastKwh}kWh",
                    session.Id, energyKwh, lastReading.EnergyKwh);
                return null;
            }

            // Reject unreasonable jumps (> 500 kWh delta from last reading)
            if (lastReading != null && (energyKwh - lastReading.EnergyKwh) > 500m)
            {
                _logger.LogWarning(
                    "Rejected unreasonable MeterValue jump for session {SessionId}: {EnergyKwh}kWh, last={LastKwh}kWh",
                    session.Id, energyKwh, lastReading.EnergyKwh);
                return null;
            }

            // Add meter value to session (returns null if duplicate)
            var powerKw = power != null ? power / 1000m : null; // Convert W to kW
            var meterValue = session.AddMeterValue(
                GuidGenerator.Create(),
                energyKwh,
                meterTimestamp,
                currentAmps,
                voltage,
                powerKw,
                soc
            );

            if (meterValue == null)
            {
                _logger.LogDebug("Duplicate MeterValue skipped for session {SessionId}", session.Id);
                return null;
            }

            // Update running energy total during charging
            if (session.MeterStart.HasValue && energyWh > 0)
            {
                var totalEnergyKwh = Math.Round((energyWh - session.MeterStart.Value) / 1000m, 3);
                if (totalEnergyKwh > 0)
                {
                    session.UpdateTotalEnergy(totalEnergyKwh);
                }
            }

            await _sessionRepository.UpdateAsync(session);

            _logger.LogInformation("MeterValue recorded for session {SessionId}: {EnergyKwh}kWh, Total={TotalKwh}kWh",
                session.Id, energyKwh, session.TotalEnergyKwh);

            return new MeterValuesResult(
                session.Id,
                session.StationId,
                session.ConnectorNumber,
                session.TotalEnergyKwh,
                session.TotalCost,
                powerKw,
                soc);
        }
        else
        {
            // Store standalone meter value (no active session)
            _logger.LogDebug("MeterValue received without active session for {ChargePointId}", chargePointId);
            return null;
        }
    }

    public async Task<ChargingStation?> GetStationByChargePointIdAsync(string chargePointId)
    {
        return await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
    }

    public async Task<bool> ValidateIdTagAsync(string idTag)
    {
        if (string.IsNullOrWhiteSpace(idTag))
            return false;

        // Check if idTag is a valid user GUID (mobile app sessions use userId as idTag)
        if (Guid.TryParse(idTag, out var userId) && userId != Guid.Empty)
        {
            return true;
        }

        // Look up RFID/physical tag in UserIdTag registry
        var userIdTag = await _userIdTagRepository.FirstOrDefaultAsync(
            t => t.IdTag == idTag && t.IsActive);
        if (userIdTag != null && userIdTag.IsValid())
        {
            return true;
        }

        // Accept known test/demo idTags only when explicitly enabled via configuration
        if (idTag.StartsWith("TEST") || idTag.StartsWith("DEMO"))
        {
            var allowTestIdTags = _configuration.GetValue<bool>("Ocpp:AllowTestIdTags", false);
            if (allowTestIdTags)
            {
                _logger.LogInformation("Accepted test/demo idTag: {IdTag} (Ocpp:AllowTestIdTags is enabled)", idTag);
                return true;
            }
        }

        _logger.LogWarning("IdTag validation failed for: {IdTag}", idTag);
        return false;
    }

    public async Task HandleFirmwareStatusAsync(string chargePointId, string status)
    {
        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
        if (station == null)
        {
            _logger.LogWarning("FirmwareStatusNotification from unknown station: {ChargePointId}", chargePointId);
            return;
        }

        station.UpdateFirmwareStatus(status);
        await _stationRepository.UpdateAsync(station);

        _logger.LogInformation("Firmware status updated for {ChargePointId}: {Status}", chargePointId, status);
    }

    public async Task HandleDiagnosticsStatusAsync(string chargePointId, string status)
    {
        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
        if (station == null)
        {
            _logger.LogWarning("DiagnosticsStatusNotification from unknown station: {ChargePointId}", chargePointId);
            return;
        }

        station.UpdateDiagnosticsStatus(status);
        await _stationRepository.UpdateAsync(station);

        _logger.LogInformation("Diagnostics status updated for {ChargePointId}: {Status}", chargePointId, status);
    }

    public async Task HandleStationDisconnectAsync(string chargePointId)
    {
        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
        if (station == null)
        {
            return;
        }

        // Find all active sessions for this station
        var activeSessions = await AsyncExecuter.ToListAsync(
            (await _sessionRepository.GetQueryableAsync())
                .Where(s => s.StationId == station.Id &&
                            (s.Status == SessionStatus.Pending ||
                             s.Status == SessionStatus.InProgress ||
                             s.Status == SessionStatus.Starting ||
                             s.Status == SessionStatus.Suspended)));

        foreach (var session in activeSessions)
        {
            session.MarkFailed("Station disconnected");
            await _sessionRepository.UpdateAsync(session);

            _logger.LogWarning(
                "Orphaned session {SessionId} marked Failed due to station {ChargePointId} disconnect",
                session.Id, chargePointId);
        }

        if (activeSessions.Count > 0)
        {
            _logger.LogInformation(
                "Marked {Count} orphaned sessions as Failed for station {ChargePointId}",
                activeSessions.Count, chargePointId);
        }
    }
}
