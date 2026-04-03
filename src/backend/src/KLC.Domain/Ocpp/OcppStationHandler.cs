using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Faults;
using KLC.Fleets;
using KLC.Sessions;
using KLC.Stations;
using KLC.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace KLC.Ocpp;

/// <summary>
/// Handles station lifecycle OCPP messages: BootNotification, StatusNotification,
/// Heartbeat, FirmwareStatus, DiagnosticsStatus, and station disconnect.
/// </summary>
public class OcppStationHandler : DomainService
{
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<Fault, Guid> _faultRepository;
    private readonly IRepository<AppUser, Guid> _userRepository;
    private readonly IRepository<UserIdTag, Guid> _userIdTagRepository;
    private readonly IFleetChargingPolicyService _fleetChargingPolicyService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcppStationHandler> _logger;

    public OcppStationHandler(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<Fault, Guid> faultRepository,
        IRepository<AppUser, Guid> userRepository,
        IRepository<UserIdTag, Guid> userIdTagRepository,
        IFleetChargingPolicyService fleetChargingPolicyService,
        IConfiguration configuration,
        ILogger<OcppStationHandler> logger)
    {
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _sessionRepository = sessionRepository;
        _faultRepository = faultRepository;
        _userRepository = userRepository;
        _userIdTagRepository = userIdTagRepository;
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

        if (!station.IsEnabled || station.Status == StationStatus.Disabled)
        {
            _logger.LogWarning(
                "BootNotification rejected for unavailable station {ChargePointId}: enabled={IsEnabled}, status={Status}",
                chargePointId,
                station.IsEnabled,
                station.Status);
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
            // ConnectorId 0 = overall charger status in OCPP.
            // Station status is managed by connect/disconnect lifecycle, not by StatusNotification.
            // Just log it — don't change station status.
            _logger.LogDebug("Overall charger status for {ChargePointId}: {Status}", chargePointId, status);
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
        // Auto-resolve faults when error clears (NoError or connector goes Available)
        else if (string.Equals(errorCode, "NoError", StringComparison.OrdinalIgnoreCase) ||
                 status == ConnectorStatus.Available)
        {
            // Resolve faults for this connector AND station-level (ConnectorNumber=null)
            var openFaults = await AsyncExecuter.ToListAsync(
                (await _faultRepository.GetQueryableAsync())
                    .Where(f => f.StationId == station.Id
                        && (f.ConnectorNumber == null ||
                            f.ConnectorNumber == (connectorId > 0 ? connectorId : (int?)null))
                        && (f.Status == FaultStatus.Open || f.Status == FaultStatus.Investigating)));

            foreach (var fault in openFaults)
            {
                fault.Close("Auto-resolved: charger reported NoError/Available");
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

        // Mark station as offline immediately on WebSocket disconnect
        station.MarkOffline();

        // Reset all connectors to Unavailable when station goes offline
        var connectors = await AsyncExecuter.ToListAsync(
            (await _connectorRepository.GetQueryableAsync())
                .Where(c => c.StationId == station.Id && c.Status != ConnectorStatus.Unavailable));

        foreach (var connector in connectors)
        {
            connector.UpdateStatus(ConnectorStatus.Unavailable);
            await _connectorRepository.UpdateAsync(connector);
        }

        await _stationRepository.UpdateAsync(station);
        _logger.LogInformation(
            "Station {ChargePointId} marked Offline, {ConnectorCount} connectors set to Unavailable",
            chargePointId, connectors.Count);

        // Fail Pending/Starting sessions immediately — they never got an OCPP transaction,
        // so there's nothing to recover.
        var pendingSessions = await AsyncExecuter.ToListAsync(
            (await _sessionRepository.GetQueryableAsync())
                .Where(s => s.StationId == station.Id &&
                            s.OcppTransactionId == null &&
                            (s.Status == SessionStatus.Pending ||
                             s.Status == SessionStatus.Starting)));

        foreach (var session in pendingSessions)
        {
            session.MarkFailed("Station disconnected before transaction started");
            await _sessionRepository.UpdateAsync(session);

            _logger.LogWarning(
                "Pending session {SessionId} marked Failed due to station {ChargePointId} disconnect",
                session.Id, chargePointId);
        }

        // InProgress/Suspended sessions are NOT failed immediately.
        // Real chargers may reconnect after transient network issues and resume
        // or send StopTransaction. A background job (OrphanedSessionCleanupService)
        // will fail these after a grace period if the station doesn't reconnect.
        var inProgressCount = await AsyncExecuter.CountAsync(
            (await _sessionRepository.GetQueryableAsync())
                .Where(s => s.StationId == station.Id &&
                            s.OcppTransactionId != null &&
                            (s.Status == SessionStatus.InProgress ||
                             s.Status == SessionStatus.Suspended)));

        if (inProgressCount > 0)
        {
            _logger.LogWarning(
                "{Count} InProgress sessions for station {ChargePointId} kept alive pending reconnection (grace period)",
                inProgressCount, chargePointId);
        }

        if (pendingSessions.Count > 0)
        {
            _logger.LogInformation(
                "Marked {Count} pending sessions as Failed for station {ChargePointId}",
                pendingSessions.Count, chargePointId);
        }
    }

    public async Task UpdateConnectorStatusAsync(Guid stationId, int connectorNumber, ConnectorStatus status)
    {
        var connector = await _connectorRepository.FirstOrDefaultAsync(
            c => c.StationId == stationId && c.ConnectorNumber == connectorNumber);
        if (connector != null)
        {
            connector.UpdateStatus(status);
            await _connectorRepository.UpdateAsync(connector);
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

        // Accept known test/demo idTags early (no DB query needed)
        if (idTag.StartsWith("TEST") || idTag.StartsWith("DEMO"))
        {
            var allowTestIdTags = _configuration.GetValue<bool>("Ocpp:AllowTestIdTags", false);
            if (allowTestIdTags)
            {
                _logger.LogInformation("Accepted test/demo idTag: {IdTag} (Ocpp:AllowTestIdTags is enabled)", idTag);
                return true;
            }
        }

        // Look up RFID/physical tag in UserIdTag registry
        try
        {
            var userIdTag = await _userIdTagRepository.FirstOrDefaultAsync(
                t => t.IdTag == idTag && t.IsActive);
            if (userIdTag != null && userIdTag.IsValid())
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query UserIdTag for {IdTag}, rejecting", idTag);
        }

        // Accept unregistered RFID tags when configured (allows real chargers to start)
        var allowUnregistered = _configuration.GetValue<bool>("Ocpp:AllowUnregisteredIdTags", true);
        if (allowUnregistered)
        {
            _logger.LogWarning("Accepting unregistered idTag {IdTag} (Ocpp:AllowUnregisteredIdTags enabled)", idTag);
            return true;
        }

        _logger.LogWarning("IdTag validation failed for: {IdTag}", idTag);
        return false;
    }

    public async Task<ChargingSession?> GetActiveSessionForConnectorAsync(string chargePointId, int connectorNumber)
    {
        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);
        if (station == null) return null;

        return await _sessionRepository.FirstOrDefaultAsync(
            s => s.StationId == station.Id &&
                 s.ConnectorNumber == connectorNumber &&
                 (s.Status == SessionStatus.InProgress || s.Status == SessionStatus.Suspended));
    }
}
