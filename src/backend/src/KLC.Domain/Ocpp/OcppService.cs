using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Faults;
using KLC.Fleets;
using KLC.Payments;
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
    private readonly IRepository<AppUser, Guid> _userRepository;
    private readonly WalletDomainService _walletDomainService;
    private readonly IRepository<WalletTransaction, Guid> _walletTransactionRepository;
    private readonly IRepository<PaymentTransaction, Guid> _paymentTransactionRepository;
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
        IRepository<AppUser, Guid> userRepository,
        WalletDomainService walletDomainService,
        IRepository<WalletTransaction, Guid> walletTransactionRepository,
        IRepository<PaymentTransaction, Guid> paymentTransactionRepository,
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
        _userRepository = userRepository;
        _walletDomainService = walletDomainService;
        _walletTransactionRepository = walletTransactionRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
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

        if (!station.IsEnabled || station.Status == StationStatus.Disabled || station.Status == StationStatus.Decommissioned)
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

        // Resolve userId from idTag — ALWAYS normalize to IdentityUserId
        var userId = Guid.Empty;
        if (Guid.TryParse(idTag, out var parsedUserId) && parsedUserId != Guid.Empty)
        {
            // idTag is a GUID — could be AppUser.Id or IdentityUserId
            // Normalize: try AppUser.Id first, then IdentityUserId
            var appUser = await _userRepository.FirstOrDefaultAsync(u => u.Id == parsedUserId);
            if (appUser != null)
            {
                userId = appUser.IdentityUserId;
            }
            else
            {
                // Maybe it's already an IdentityUserId
                var byIdentity = await _userRepository.FirstOrDefaultAsync(u => u.IdentityUserId == parsedUserId);
                if (byIdentity != null)
                {
                    userId = parsedUserId; // Already IdentityUserId
                }
            }
        }
        else
        {
            // Look up RFID/physical tag in UserIdTag registry
            var userIdTag = await _userIdTagRepository.FirstOrDefaultAsync(
                t => t.IdTag == idTag && t.IsActive);
            if (userIdTag != null && userIdTag.IsValid())
            {
                // UserIdTag.UserId stores AppUser.Id — normalize to IdentityUserId
                var tagUser = await _userRepository.FirstOrDefaultAsync(u => u.Id == userIdTag.UserId);
                userId = tagUser?.IdentityUserId ?? Guid.Empty;
                if (userId != Guid.Empty)
                    _logger.LogInformation("Resolved idTag {IdTag} to IdentityUserId {UserId}", idTag, userId);
            }
        }

        // For test/demo idTags, assign the first active AppUser when AllowTestIdTags is enabled
        if (userId == Guid.Empty && (idTag.StartsWith("TEST") || idTag.StartsWith("DEMO")))
        {
            var allowTestIdTags = _configuration.GetValue<bool>("Ocpp:AllowTestIdTags", false);
            if (allowTestIdTags)
            {
                var firstUser = await _userRepository.FirstOrDefaultAsync(u => u.IsActive);
                if (firstUser != null)
                {
                    userId = firstUser.IdentityUserId; // Use IdentityUserId, not Id
                    _logger.LogInformation(
                        "Test idTag {IdTag} assigned to IdentityUserId {UserId} (Ocpp:AllowTestIdTags enabled)",
                        idTag, userId);
                }
            }
        }

        // For unregistered RFID cards: accept the transaction when AllowUnregisteredIdTags is enabled
        // This allows real chargers with physical RFID to start charging sessions
        // The session is created without a user (walk-in) for billing reconciliation later
        if (userId == Guid.Empty)
        {
            var allowUnregistered = _configuration.GetValue<bool>("Ocpp:AllowUnregisteredIdTags", true);
            if (allowUnregistered)
            {
                _logger.LogWarning(
                    "Unregistered idTag {IdTag} accepted (Ocpp:AllowUnregisteredIdTags enabled). " +
                    "Session will be created without user assignment. Register this RFID card in the admin portal.",
                    idTag);
                // userId stays Guid.Empty — session created as walk-in
            }
            else
            {
                _logger.LogWarning("StartTransaction rejected: idTag {IdTag} could not be resolved to a valid user", idTag);
                return null;
            }
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

        // Link to existing BFF-created Pending session (from mobile app QR flow)
        // The BFF creates a session with Status=Pending before sending RemoteStartTransaction
        var pendingSession = await _sessionRepository.FirstOrDefaultAsync(
            s => s.StationId == station.Id &&
                 s.ConnectorNumber == connectorId &&
                 s.OcppTransactionId == null &&
                 (s.Status == SessionStatus.Pending || s.Status == SessionStatus.Starting) &&
                 (userId == Guid.Empty || s.UserId == userId));

        if (pendingSession != null)
        {
            pendingSession.RecordStart(ocppTransactionId, meterStart);
            await _sessionRepository.UpdateAsync(pendingSession);

            // Update connector status to Charging
            await UpdateConnectorStatusAsync(station.Id, connectorId, ConnectorStatus.Charging);

            _logger.LogInformation(
                "Linked OCPP transaction {TransactionId} to existing BFF session {SessionId}",
                ocppTransactionId, pendingSession.Id);

            return pendingSession.Id;
        }

        // No pending session found — create a new one (direct charger start / RFID)
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

        // Update connector status to Charging
        await UpdateConnectorStatusAsync(station.Id, connectorId, ConnectorStatus.Charging);

        _logger.LogInformation("Session {SessionId} started for transaction {TransactionId}",
            sessionId, ocppTransactionId);

        return sessionId;
    }

    private async Task UpdateConnectorStatusAsync(Guid stationId, int connectorNumber, ConnectorStatus status)
    {
        var connector = await _connectorRepository.FirstOrDefaultAsync(
            c => c.StationId == stationId && c.ConnectorNumber == connectorNumber);
        if (connector != null)
        {
            connector.UpdateStatus(status);
            await _connectorRepository.UpdateAsync(connector);
        }
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

        // Auto-deduct session cost from user's wallet and create PaymentTransaction
        if (session.TotalCost > 0 && session.UserId != Guid.Empty)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(
                    u => u.IdentityUserId == session.UserId);

                if (user != null && user.WalletBalance > 0)
                {
                    var deductAmount = Math.Min(session.TotalCost, user.WalletBalance);
                    var (newBalance, walletTx) = _walletDomainService.DeductForSession(
                        user, deductAmount, session.Id);

                    await _userRepository.UpdateAsync(user);
                    await _walletTransactionRepository.InsertAsync(walletTx);

                    // Create PaymentTransaction so session payment is visible in admin portal
                    var paymentTx = new PaymentTransaction(
                        Guid.NewGuid(),
                        session.Id,
                        session.UserId,
                        PaymentGateway.Wallet,
                        deductAmount);
                    paymentTx.MarkCompleted($"WALLET-{walletTx.Id.ToString("N")[..8].ToUpper()}");
                    await _paymentTransactionRepository.InsertAsync(paymentTx);

                    _logger.LogInformation(
                        "Wallet deducted for session {SessionId}: Amount={Amount}, NewBalance={NewBalance}, PaymentId={PaymentId}",
                        session.Id, deductAmount, newBalance, paymentTx.Id);

                    // If wallet didn't cover full cost, create pending payment for remainder
                    if (deductAmount < session.TotalCost)
                    {
                        var remainingAmount = session.TotalCost - deductAmount;
                        var pendingPayment = new PaymentTransaction(
                            Guid.NewGuid(),
                            session.Id,
                            session.UserId,
                            PaymentGateway.VnPay,
                            remainingAmount);
                        await _paymentTransactionRepository.InsertAsync(pendingPayment);

                        _logger.LogWarning(
                            "Partial payment for session {SessionId}: Wallet={WalletAmount}, Remaining={Remaining} (pending VnPay)",
                            session.Id, deductAmount, remainingAmount);
                    }
                }
                else if (user != null)
                {
                    // No wallet balance — create pending payment for full amount
                    var pendingPayment = new PaymentTransaction(
                        Guid.NewGuid(),
                        session.Id,
                        session.UserId,
                        PaymentGateway.VnPay,
                        session.TotalCost);
                    await _paymentTransactionRepository.InsertAsync(pendingPayment);

                    _logger.LogWarning(
                        "No wallet balance for session {SessionId}: Pending VnPay payment for {Amount} VND",
                        session.Id, session.TotalCost);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment for session {SessionId}. Manual billing required.", session.Id);
                // Do NOT rethrow - session completion must not fail due to payment issues
            }
        }

        // Update connector status to Available after session completes
        await UpdateConnectorStatusAsync(session.StationId, session.ConnectorNumber, ConnectorStatus.Available);

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
}
