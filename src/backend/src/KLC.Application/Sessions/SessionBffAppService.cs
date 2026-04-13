using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Fleets;
using KLC.Stations;
using KLC.Tariffs;
using KLC.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace KLC.Sessions;

/// <summary>
/// Application service encapsulating session start/stop business logic.
/// Shared between Admin API and Driver BFF.
/// </summary>
public class SessionBffAppService : ISessionBffAppService
{
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly IRepository<TariffPlan, Guid> _tariffRepository;
    private readonly IRepository<AppUser, Guid> _appUserRepository;
    private readonly IFleetChargingPolicyService _fleetChargingPolicyService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SessionBffAppService> _logger;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public SessionBffAppService(
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<TariffPlan, Guid> tariffRepository,
        IRepository<AppUser, Guid> appUserRepository,
        IFleetChargingPolicyService fleetChargingPolicyService,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SessionBffAppService> logger,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _sessionRepository = sessionRepository;
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _tariffRepository = tariffRepository;
        _appUserRepository = appUserRepository;
        _fleetChargingPolicyService = fleetChargingPolicyService;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<StartSessionResultDto> StartSessionAsync(StartSessionInput input)
    {
        // Resolve connector from flexible input: connectorId, stationId+connectorNumber, or stationCode+connectorNumber
        Connector? connector = null;
        ChargingStation? station = null;

        if (input.ConnectorId.HasValue)
        {
            connector = await _connectorRepository.FirstOrDefaultAsync(c => c.Id == input.ConnectorId.Value);
            if (connector != null)
            {
                station = await _stationRepository.FirstOrDefaultAsync(s => s.Id == connector.StationId);
            }
        }
        else
        {
            var stationId = input.StationId;
            if (!stationId.HasValue && !string.IsNullOrEmpty(input.StationCode))
            {
                station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == input.StationCode);
                stationId = station?.Id;
            }
            else if (stationId.HasValue)
            {
                station = await _stationRepository.FirstOrDefaultAsync(s => s.Id == stationId.Value);
            }

            if (stationId.HasValue && input.ConnectorNumber.HasValue)
            {
                connector = await _connectorRepository.FirstOrDefaultAsync(
                    c => c.StationId == stationId.Value &&
                         c.ConnectorNumber == input.ConnectorNumber.Value);
            }
        }

        if (connector == null)
        {
            _logger.LogWarning(
                "StartSession: connector not found. UserId={UserId}, ConnectorId={ConnectorId}, " +
                "StationId={StationId}, StationCode={StationCode}, ConnectorNumber={ConnectorNumber}",
                input.UserId, input.ConnectorId, input.StationId, input.StationCode, input.ConnectorNumber);
            return new StartSessionResultDto { Success = false, Error = "Connector not found" };
        }

        _logger.LogInformation(
            "StartSession: connector resolved. UserId={UserId}, ConnectorId={ConnectorId}, " +
            "StationId={StationId}, ConnectorNumber={ConnectorNumber}, IsEnabled={IsEnabled}, " +
            "Status={Status}, StationCode={StationCode}",
            input.UserId, connector.Id, connector.StationId, connector.ConnectorNumber,
            connector.IsEnabled, connector.Status, station?.StationCode);

        // Allow both Available (idle) and Preparing (cable plugged in, waiting for authorization)
        var canStartSession = connector.IsEnabled &&
            (connector.Status == ConnectorStatus.Available || connector.Status == ConnectorStatus.Preparing);

        if (!canStartSession)
        {
            _logger.LogWarning(
                "StartSession rejected: connector not available. UserId={UserId}, ConnectorId={ConnectorId}, " +
                "StationId={StationId}, ConnectorNumber={ConnectorNumber}, IsEnabled={IsEnabled}, " +
                "Status={Status}, StationCode={StationCode}",
                input.UserId, connector.Id, connector.StationId, connector.ConnectorNumber,
                connector.IsEnabled, connector.Status, station?.StationCode);
            return new StartSessionResultDto { Success = false, Error = "Connector is not available" };
        }

        // Check for existing active session
        var existingSession = await _sessionRepository.FirstOrDefaultAsync(
            s => s.UserId == input.UserId &&
                 (s.Status == SessionStatus.Pending ||
                  s.Status == SessionStatus.Starting ||
                  s.Status == SessionStatus.InProgress));

        if (existingSession != null)
        {
            return new StartSessionResultDto { Success = false, Error = "You already have an active session" };
        }

        // Validate wallet balance before starting session
        var minBalance = _configuration.GetValue<decimal>("Wallet:MinBalanceToStart", 50_000m);
        var user = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == input.UserId);

        if (user == null || user.WalletBalance < minBalance)
        {
            _logger.LogWarning(
                "Session start denied: insufficient wallet balance. UserId={UserId}, Balance={Balance}, MinRequired={MinBalance}",
                input.UserId, user?.WalletBalance ?? 0, minBalance);
            return new StartSessionResultDto
            {
                Success = false,
                Error = KLCDomainErrorCodes.Wallet.InsufficientBalanceToCharge
            };
        }

        // Validate fleet charging policy if vehicle is specified
        if (input.VehicleId.HasValue)
        {
            var policyResult = await _fleetChargingPolicyService.ValidateChargingAsync(
                input.VehicleId.Value, connector.StationId);
            if (!policyResult.Allowed)
            {
                _logger.LogWarning(
                    "Session start denied by fleet policy: userId={UserId}, vehicleId={VehicleId}, reason={Reason}",
                    input.UserId, input.VehicleId, policyResult.DenialReason);
                return new StartSessionResultDto
                {
                    Success = false,
                    Error = policyResult.DenialReason ?? "Fleet charging policy denied"
                };
            }
        }

        try
        {
            // Get tariff
            var tariff = station?.TariffPlanId.HasValue == true
                ? await _tariffRepository.FirstOrDefaultAsync(t => t.Id == station.TariffPlanId)
                : await _tariffRepository.FirstOrDefaultAsync(t => t.IsDefault && t.IsActive);

            // Create session
            var session = new ChargingSession(
                Guid.NewGuid(),
                input.UserId,
                connector.StationId,
                connector.ConnectorNumber,
                input.VehicleId,
                tariff?.Id,
                tariff?.BaseRatePerKwh ?? 0);

            await _sessionRepository.InsertAsync(session);

            // Update connector status
            connector.UpdateStatus(ConnectorStatus.Preparing);
            await _connectorRepository.UpdateAsync(connector);
            await _unitOfWorkManager.Current!.SaveChangesAsync();

            // Send RemoteStartTransaction to the charger via OCPP Gateway
            var remoteStartAccepted = false;
            try
            {
                var adminApiUrl = _configuration["Ocpp:GatewayUrl"] ?? _configuration["Auth:Authority"] ?? "https://localhost:44305";
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(35);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var internalApiKey = _configuration["Internal:ApiKey"];
                if (!string.IsNullOrEmpty(internalApiKey))
                {
                    httpClient.DefaultRequestHeaders.Add("X-Internal-Key", internalApiKey);
                }

                var idTag = session.Id.ToString("N")[..20];
                _logger.LogInformation(
                    "REMOTE_START: Sending to {GatewayUrl} — Station={StationCode}, Connector={ConnectorNumber}, IdTag={IdTag}, SessionId={SessionId}",
                    adminApiUrl, station?.StationCode, connector.ConnectorNumber, idTag, session.Id);

                var remoteStartResponse = await httpClient.PostAsJsonAsync(
                    $"{adminApiUrl}/api/internal/ocpp/remote-start",
                    new { stationCode = station?.StationCode, connectorId = connector.ConnectorNumber, idTag });

                var responseBody = await remoteStartResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(
                    "REMOTE_START_RESPONSE: Station={StationCode}, HttpStatus={HttpStatus}, Body={Body}",
                    station?.StationCode, (int)remoteStartResponse.StatusCode, responseBody);

                if (remoteStartResponse.IsSuccessStatusCode)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
                        remoteStartAccepted = doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
                    }
                    catch { /* parse failure = not accepted */ }
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning(
                    "REMOTE_START_TIMEOUT: Request to OCPP Gateway timed out — Station={StationCode}, SessionId={SessionId}",
                    station?.StationCode, session.Id);
            }
            catch (Exception remoteEx)
            {
                _logger.LogWarning(remoteEx,
                    "REMOTE_START_FAIL: Failed to send RemoteStartTransaction — Station={StationCode}, SessionId={SessionId}",
                    station?.StationCode, session.Id);
            }

            // If charger did not accept, mark session as Failed
            if (!remoteStartAccepted)
            {
                _logger.LogWarning(
                    "REMOTE_START_REJECTED: Charger did not accept. Marking session Failed — SessionId={SessionId}, Station={StationCode}",
                    session.Id, station?.StationCode);
                session.MarkFailed("Charger did not accept RemoteStartTransaction");
                connector.UpdateStatus(ConnectorStatus.Available);
                await _sessionRepository.UpdateAsync(session);
                await _connectorRepository.UpdateAsync(connector);

                return new StartSessionResultDto
                {
                    Success = false,
                    SessionId = session.Id,
                    StationId = connector.StationId,
                    Error = "Trạm sạc không phản hồi. Vui lòng thử lại."
                };
            }

            return new StartSessionResultDto
            {
                Success = true,
                SessionId = session.Id,
                Status = session.Status,
                StationId = connector.StationId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session for user {UserId} at station {StationId}", input.UserId, connector.StationId);
            return new StartSessionResultDto { Success = false, Error = "Failed to start charging session" };
        }
    }

    public async Task<StopSessionResultDto> StopSessionAsync(Guid userId, Guid sessionId)
    {
        var session = await _sessionRepository.FirstOrDefaultAsync(
            s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
        {
            return new StopSessionResultDto { Success = false, Error = "Session not found" };
        }

        if (session.Status != SessionStatus.InProgress && session.Status != SessionStatus.Suspended)
        {
            return new StopSessionResultDto { Success = false, Error = "Session is not in progress" };
        }

        try
        {
            session.MarkStopping();
            await _sessionRepository.UpdateAsync(session);
            await _unitOfWorkManager.Current!.SaveChangesAsync();

            // Send RemoteStopTransaction to charger via OCPP Gateway
            if (session.OcppTransactionId.HasValue)
            {
                try
                {
                    var station = await _stationRepository.FirstOrDefaultAsync(s => s.Id == session.StationId);

                    if (station != null)
                    {
                        var adminApiUrl = _configuration["Ocpp:GatewayUrl"] ?? _configuration["Auth:Authority"] ?? "https://localhost:44305";
                        using var httpClient = _httpClientFactory.CreateClient();
                        httpClient.DefaultRequestHeaders.Accept.Add(
                            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                        var internalApiKey = _configuration["Internal:ApiKey"];
                        if (!string.IsNullOrEmpty(internalApiKey))
                        {
                            httpClient.DefaultRequestHeaders.Add("X-Internal-Key", internalApiKey);
                        }

                        var remoteStopResponse = await httpClient.PostAsJsonAsync(
                            $"{adminApiUrl}/api/internal/ocpp/remote-stop",
                            new { stationCode = station.StationCode, transactionId = session.OcppTransactionId.Value });

                        if (remoteStopResponse.IsSuccessStatusCode)
                        {
                            _logger.LogInformation(
                                "RemoteStopTransaction sent: Station={StationCode}, TxnId={TransactionId}",
                                station.StationCode, session.OcppTransactionId.Value);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "RemoteStopTransaction failed: Station={StationCode}, Status={StatusCode}",
                                station.StationCode, remoteStopResponse.StatusCode);
                        }
                    }
                }
                catch (Exception remoteEx)
                {
                    _logger.LogWarning(remoteEx,
                        "Failed to send RemoteStopTransaction for session {SessionId}",
                        sessionId);
                }
            }

            return new StopSessionResultDto
            {
                Success = true,
                SessionId = session.Id,
                Status = session.Status,
                StationId = session.StationId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop session {SessionId} for user {UserId}", sessionId, userId);
            return new StopSessionResultDto { Success = false, Error = "Failed to stop charging session" };
        }
    }
}
