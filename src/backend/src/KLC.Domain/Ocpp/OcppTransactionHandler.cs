using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
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

namespace KLC.Ocpp;

/// <summary>
/// Handles OCPP transaction messages: StartTransaction and StopTransaction.
/// </summary>
public class OcppTransactionHandler : DomainService
{
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<AppUser, Guid> _userRepository;
    private readonly IRepository<UserIdTag, Guid> _userIdTagRepository;
    private readonly IRepository<Vehicle, Guid> _vehicleRepository;
    private readonly IRepository<TariffPlan, Guid> _tariffPlanRepository;
    private readonly IRepository<WalletTransaction, Guid> _walletTransactionRepository;
    private readonly IRepository<PaymentTransaction, Guid> _paymentTransactionRepository;
    private readonly IRepository<FleetVehicle, Guid> _fleetVehicleRepository;
    private readonly IRepository<Fleet, Guid> _fleetRepository;
    private readonly WalletDomainService _walletDomainService;
    private readonly IFleetChargingPolicyService _fleetChargingPolicyService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcppTransactionHandler> _logger;

    public OcppTransactionHandler(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<AppUser, Guid> userRepository,
        IRepository<UserIdTag, Guid> userIdTagRepository,
        IRepository<Vehicle, Guid> vehicleRepository,
        IRepository<TariffPlan, Guid> tariffPlanRepository,
        IRepository<WalletTransaction, Guid> walletTransactionRepository,
        IRepository<PaymentTransaction, Guid> paymentTransactionRepository,
        IRepository<FleetVehicle, Guid> fleetVehicleRepository,
        IRepository<Fleet, Guid> fleetRepository,
        WalletDomainService walletDomainService,
        IFleetChargingPolicyService fleetChargingPolicyService,
        IConfiguration configuration,
        ILogger<OcppTransactionHandler> logger)
    {
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
        _userIdTagRepository = userIdTagRepository;
        _vehicleRepository = vehicleRepository;
        _tariffPlanRepository = tariffPlanRepository;
        _walletTransactionRepository = walletTransactionRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
        _fleetVehicleRepository = fleetVehicleRepository;
        _fleetRepository = fleetRepository;
        _walletDomainService = walletDomainService;
        _fleetChargingPolicyService = fleetChargingPolicyService;
        _configuration = configuration;
        _logger = logger;
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
            // idTag is a full GUID — could be AppUser.Id or IdentityUserId
            // Normalize: try AppUser.Id first, then IdentityUserId
            var appUser = await _userRepository.FirstOrDefaultAsync(u => u.Id == parsedUserId);
            if (appUser != null)
            {
                userId = appUser.IdentityUserId;
            }
            else
            {
                var byIdentity = await _userRepository.FirstOrDefaultAsync(u => u.IdentityUserId == parsedUserId);
                if (byIdentity != null)
                {
                    userId = parsedUserId;
                }
            }
        }
        else if (idTag.Length == 20 && idTag.All(c => "0123456789abcdefABCDEF".Contains(c)))
        {
            // Truncated GUID format: first 20 hex chars of Guid.ToString("N")
            // Used by BFF to comply with OCPP 1.6 idTag max 20 chars.
            // BFF sends session.Id as idTag — the Pending session linking (below) handles
            // user resolution via station+connector match. Log for traceability.
            _logger.LogInformation(
                "Truncated hex idTag {IdTag} from {ChargePointId} — will resolve via Pending session link",
                idTag, chargePointId);
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
}
