using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Auditing;
using KLC.Enums;
using KLC.Hubs;
using KLC.Notifications;
using KLC.Ocpp.Messages;
using KLC.Ocpp.Vendors;
using KLC.Operators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Settings;
using Volo.Abp.Uow;

namespace KLC.Ocpp;

/// <summary>
/// Handles OCPP 1.6J messages from Charge Points.
/// </summary>
public class OcppMessageHandler
{
    private readonly ILogger<OcppMessageHandler> _logger;
    private readonly OcppConnectionManager _connectionManager;
    private readonly IOcppService _ocppService;
    private readonly IMonitoringNotifier _notifier;
    private readonly VendorProfileFactory _vendorProfileFactory;
    private readonly IRepository<OcppRawEvent, Guid> _rawEventRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly OcppMessageParserFactory _parserFactory;
    private readonly IAuditEventLogger _auditLogger;
    private readonly PowerBalancingService? _powerBalancingService;
    private readonly IOperatorWebhookService? _webhookService;
    private readonly ISettingProvider _settingProvider;
    private readonly IPushNotificationService? _pushNotificationService;
    private readonly IOcppRemoteCommandService _remoteCommandService;
    private readonly IServiceScopeFactory _scopeFactory;

    public OcppMessageHandler(
        ILogger<OcppMessageHandler> logger,
        OcppConnectionManager connectionManager,
        IOcppService ocppService,
        IMonitoringNotifier notifier,
        VendorProfileFactory vendorProfileFactory,
        IRepository<OcppRawEvent, Guid> rawEventRepository,
        IGuidGenerator guidGenerator,
        OcppMessageParserFactory parserFactory,
        IAuditEventLogger auditLogger,
        ISettingProvider settingProvider,
        IOcppRemoteCommandService remoteCommandService,
        IServiceScopeFactory scopeFactory,
        PowerBalancingService? powerBalancingService = null,
        IOperatorWebhookService? webhookService = null,
        IPushNotificationService? pushNotificationService = null)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _ocppService = ocppService;
        _notifier = notifier;
        _vendorProfileFactory = vendorProfileFactory;
        _rawEventRepository = rawEventRepository;
        _guidGenerator = guidGenerator;
        _parserFactory = parserFactory;
        _auditLogger = auditLogger;
        _settingProvider = settingProvider;
        _remoteCommandService = remoteCommandService;
        _scopeFactory = scopeFactory;
        _powerBalancingService = powerBalancingService;
        _webhookService = webhookService;
        _pushNotificationService = pushNotificationService;
    }

    /// <summary>
    /// Process an incoming OCPP message.
    /// </summary>
    public async Task<string?> HandleMessageAsync(OcppConnection connection, string message)
    {
        try
        {
            _logger.LogInformation("OCPP_RAW_IN from {ChargePointId} ({OcppVersion}): {Message}",
                connection.ChargePointId, connection.OcppVersion, message);

            var parser = _parserFactory.GetParser(connection.OcppVersion);
            var parsed = parser.Parse(message);

            if (parsed == null)
            {
                _logger.LogWarning("Invalid OCPP message format from {ChargePointId}", connection.ChargePointId);
                return null;
            }

            return parsed.MessageType switch
            {
                OcppMessageType.Call => await HandleCallAsync(connection, parsed, parser),
                OcppMessageType.CallResult => HandleCallResult(connection, parsed),
                OcppMessageType.CallError => HandleCallError(connection, parsed),
                _ => parser.SerializeCallError(parsed.UniqueId, OcppErrorCode.ProtocolError, "Unknown message type")
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from {ChargePointId}", connection.ChargePointId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {ChargePointId}", connection.ChargePointId);
            var parser = _parserFactory.GetParser(connection.OcppVersion);
            return parser.SerializeCallError("", OcppErrorCode.InternalError, ex.Message);
        }
    }

    private async Task<string> HandleCallAsync(OcppConnection connection, ParsedOcppMessage parsed, IOcppMessageParser parser)
    {
        var action = parsed.Action ?? string.Empty;
        var payload = parsed.Payload;
        var uniqueId = parsed.UniqueId;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("Handling {Action} from {ChargePointId} [uid={UniqueId}, proto={OcppVersion}]",
            action, connection.ChargePointId, uniqueId, connection.OcppVersion);

        // OCPP 1.6J §4.1.1 idempotency: if charger retries the same uniqueId (no response received
        // in time), return the cached response instead of re-processing — prevents duplicate sessions.
        var cachedResponse = connection.GetCachedResponse(uniqueId);
        if (cachedResponse != null)
        {
            _logger.LogWarning(
                "Idempotent retry: returning cached response for {Action} uid={UniqueId} from {ChargePointId}",
                action, uniqueId, connection.ChargePointId);
            return cachedResponse;
        }

        var result = action switch
        {
            "BootNotification" => await HandleBootNotificationAsync(connection, uniqueId, payload, parser),
            "Heartbeat" => await HandleHeartbeatAsync(connection, uniqueId, parser),
            "StatusNotification" => await HandleStatusNotificationAsync(connection, uniqueId, payload, parser),
            "StartTransaction" => await HandleStartTransactionAsync(connection, uniqueId, payload, parser),
            "StopTransaction" => await HandleStopTransactionAsync(connection, uniqueId, payload, parser),
            "MeterValues" => await HandleMeterValuesAsync(connection, uniqueId, payload, parser),
            "Authorize" => await HandleAuthorizeAsync(uniqueId, payload, parser),
            "DataTransfer" => HandleDataTransfer(uniqueId, payload, parser),
            "DiagnosticsStatusNotification" => await HandleDiagnosticsStatusNotificationAsync(connection, uniqueId, payload, parser),
            "FirmwareStatusNotification" => await HandleFirmwareStatusNotificationAsync(connection, uniqueId, payload, parser),
            _ => parser.SerializeCallError(uniqueId, OcppErrorCode.NotImplemented, $"Action {action} not implemented")
        };

        sw.Stop();

        _logger.LogInformation(
            "Completed {Action} from {ChargePointId} [uid={UniqueId}] in {LatencyMs}ms",
            action, connection.ChargePointId, uniqueId, sw.ElapsedMilliseconds);

        // Cache response for idempotency — charger may retry if it doesn't receive our response
        // in time. 5-minute TTL covers any reasonable retry window.
        connection.CacheResponse(uniqueId, result, TimeSpan.FromMinutes(5));

        // Persist raw event for auditable actions (fire-and-forget style, inside same UoW)
        await PersistRawEventAsync(connection, action, uniqueId, payload, sw.ElapsedMilliseconds);

        return result;
    }

    private async Task PersistRawEventAsync(
        OcppConnection connection, string action, string uniqueId,
        JsonElement payload, long latencyMs)
    {
        try
        {
            var vendorProfile = connection.VendorProfileType;
            var rawPayload = payload.ValueKind != JsonValueKind.Undefined
                ? payload.GetRawText()
                : "{}";

            var rawEvent = new OcppRawEvent(
                _guidGenerator.Create(),
                connection.ChargePointId,
                action,
                uniqueId,
                OcppMessageType.Call,
                rawPayload,
                vendorProfile,
                latencyMs);

            // autoSave: true ensures the raw event is flushed to DB immediately.
            // Previously autoSave: false relied on UoW commit, but handler repo calls
            // (UpdateAsync with default autoSave: true) had already flushed the DbContext,
            // and any subsequent UoW commit failure silently lost the raw event.
            await _rawEventRepository.InsertAsync(rawEvent, autoSave: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCPP_RAW_PERSIST_FAIL: Failed to persist raw OCPP event for {ChargePointId}/{Action}/{UniqueId}",
                connection.ChargePointId, action, uniqueId);
        }
    }

    private async Task<string> HandleBootNotificationAsync(OcppConnection connection, string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        var request = JsonSerializer.Deserialize<BootNotificationRequest>(payload.GetRawText());
        if (request == null)
            return parser.SerializeCallError(uniqueId, OcppErrorCode.FormationViolation, "Invalid BootNotification payload");

        _logger.LogInformation("BootNotification from {ChargePointId}: Vendor={Vendor}, Model={Model}, FW={FirmwareVersion}",
            connection.ChargePointId, request.ChargePointVendor, request.ChargePointModel, request.FirmwareVersion);

        // Detect vendor profile from BootNotification vendor/model strings
        var vendorProfile = _vendorProfileFactory.Detect(request.ChargePointVendor, request.ChargePointModel);
        connection.SetVendorProfile(vendorProfile.ProfileType);

        _logger.LogInformation("Vendor profile for {ChargePointId}: {VendorProfile}",
            connection.ChargePointId, vendorProfile.ProfileType);

        // Capture previous status before BootNotification changes it
        var stationBefore = await _ocppService.GetStationByChargePointIdAsync(connection.ChargePointId);
        var previousStationStatus = stationBefore?.Status;

        // Persist to database (including vendor profile)
        var stationId = await _ocppService.HandleBootNotificationAsync(
            connection.ChargePointId,
            request.ChargePointVendor ?? string.Empty,
            request.ChargePointModel ?? string.Empty,
            request.ChargePointSerialNumber,
            request.FirmwareVersion);

        // Persist vendor profile on station entity and notify status change
        if (stationId.HasValue)
        {
            var station = await _ocppService.GetStationByChargePointIdAsync(connection.ChargePointId);
            if (station != null)
            {
                if (station.VendorProfile != vendorProfile.ProfileType)
                {
                    station.SetVendorProfile(vendorProfile.ProfileType);
                }

                // Broadcast station status change via SignalR (Offline → Online)
                if (previousStationStatus.HasValue && previousStationStatus.Value != station.Status)
                {
                    await _notifier.NotifyStationStatusChangedAsync(
                        station.Id,
                        station.Name,
                        previousStationStatus.Value,
                        station.Status);
                }
            }
        }

        connection.RecordHeartbeat();

        // Register the station ID on the connection for SignalR notifications
        if (stationId.HasValue)
        {
            connection.SetRegistered(stationId.Value);
            connection.PendingPostBootConfig = true;
        }

        // Reject unknown stations (BR-006-02)
        var status = stationId.HasValue ? RegistrationStatus.Accepted : RegistrationStatus.Rejected;

        _auditLogger.LogOcppEvent("BootNotification", connection.ChargePointId,
            $"Vendor={request.ChargePointVendor}, Model={request.ChargePointModel}, Status={status}");

        var heartbeatSetting = await _settingProvider.GetOrNullAsync(Settings.KLCSettings.Ocpp.HeartbeatInterval);
        var heartbeatInterval = int.TryParse(heartbeatSetting, out var parsed) && parsed > 0
            ? parsed
            : vendorProfile.HeartbeatIntervalSeconds;

        var response = new BootNotificationResponse
        {
            Status = status,
            CurrentTime = DateTime.UtcNow.ToString("o"),
            Interval = heartbeatInterval
        };

        return parser.SerializeCallResult(uniqueId, response);
    }

    private async Task<string> HandleHeartbeatAsync(OcppConnection connection, string uniqueId, IOcppMessageParser parser)
    {
        connection.RecordHeartbeat();
        _logger.LogDebug("Heartbeat from {ChargePointId}", connection.ChargePointId);

        // Persist to database
        await _ocppService.HandleHeartbeatAsync(connection.ChargePointId);

        var response = new HeartbeatResponse
        {
            CurrentTime = DateTime.UtcNow.ToString("o")
        };

        return parser.SerializeCallResult(uniqueId, response);
    }

    private async Task<string> HandleStatusNotificationAsync(OcppConnection connection, string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        var request = JsonSerializer.Deserialize<StatusNotificationRequest>(payload.GetRawText());
        if (request == null)
            return parser.SerializeCallError(uniqueId, OcppErrorCode.FormationViolation, "Invalid StatusNotification payload");

        _logger.LogInformation("StatusNotification from {ChargePointId}: Connector={ConnectorId}, Status={Status}, Error={ErrorCode}",
            connection.ChargePointId, request.ConnectorId, request.Status, request.ErrorCode);

        // Map OCPP status to domain status
        var connectorStatus = MapOcppStatusToConnectorStatus(request.Status);

        // Persist to database + escalate errors to Fault entities
        var statusResult = await _ocppService.HandleStatusNotificationAsync(
            connection.ChargePointId,
            request.ConnectorId,
            connectorStatus,
            request.ErrorCode,
            request.Info,
            request.VendorErrorCode);

        // Push real-time update via SignalR
        if (statusResult != null)
        {
            await _notifier.NotifyConnectorStatusChangedAsync(
                statusResult.StationId,
                request.ConnectorId,
                statusResult.PreviousStatus,
                statusResult.NewStatus);

            if (_webhookService != null)
            {
                // Deliver webhook: FaultDetected when connector transitions to Faulted
                if (statusResult.NewStatus == ConnectorStatus.Faulted)
                {
                    _ = _webhookService.EnqueueWebhookAsync(
                        WebhookEventType.FaultDetected,
                        statusResult.StationId,
                        new
                        {
                            stationId = statusResult.StationId,
                            connectorId = request.ConnectorId,
                            previousStatus = statusResult.PreviousStatus.ToString(),
                            newStatus = statusResult.NewStatus.ToString(),
                            errorCode = request.ErrorCode,
                            errorInfo = request.Info,
                            vendorErrorCode = request.VendorErrorCode
                        });
                }

                // Deliver webhook: ConnectorStatusChanged for all status transitions
                _ = _webhookService.EnqueueWebhookAsync(
                    WebhookEventType.ConnectorStatusChanged,
                    statusResult.StationId,
                    new
                    {
                        stationId = statusResult.StationId,
                        connectorId = request.ConnectorId,
                        previousStatus = statusResult.PreviousStatus.ToString(),
                        newStatus = statusResult.NewStatus.ToString(),
                        timestamp = DateTime.UtcNow
                    });
            }
        }

        // Auto-complete session when connector transitions to Available/Finishing
        // while an InProgress session exists (EV unplugged without StopTransaction)
        if (statusResult != null &&
            request.ConnectorId > 0 &&
            (statusResult.NewStatus == ConnectorStatus.Available ||
             statusResult.NewStatus == ConnectorStatus.Finishing) &&
            (statusResult.PreviousStatus == ConnectorStatus.Charging ||
             statusResult.PreviousStatus == ConnectorStatus.SuspendedEV ||
             statusResult.PreviousStatus == ConnectorStatus.SuspendedEVSE))
        {
            try
            {
                var session = await _ocppService.GetActiveSessionForConnectorAsync(
                    connection.ChargePointId, request.ConnectorId);

                if (session != null && session.OcppTransactionId.HasValue)
                {
                    _logger.LogInformation(
                        "Connector {ConnectorId} on {ChargePointId} went {Status} — auto-completing session {SessionId} (txn={TxnId})",
                        request.ConnectorId, connection.ChargePointId, statusResult.NewStatus,
                        session.Id, session.OcppTransactionId.Value);

                    // Complete session using last known meter data
                    var meterStop = session.MeterStart.GetValueOrDefault() +
                        (int)(session.TotalEnergyKwh * 1000);
                    await _ocppService.HandleStopTransactionAsync(
                        session.OcppTransactionId.Value, meterStop, "EVDisconnected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-complete session on connector status change");
            }
        }

        return parser.SerializeCallResult(uniqueId, new StatusNotificationResponse());
    }

    private async Task<string> HandleStartTransactionAsync(OcppConnection connection, string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        var request = JsonSerializer.Deserialize<StartTransactionRequest>(payload.GetRawText());
        if (request == null)
            return parser.SerializeCallError(uniqueId, OcppErrorCode.FormationViolation, "Invalid StartTransaction payload");

        _logger.LogInformation("StartTransaction from {ChargePointId}: Connector={ConnectorId}, IdTag={IdTag}, MeterStart={MeterStart}",
            connection.ChargePointId, request.ConnectorId, request.IdTag, request.MeterStart);

        // Deduplicate: if this connector already has an active session, return the existing
        // transactionId instead of creating a new one. Some chargers (e.g., ChargeCore) retry
        // StartTransaction with new uniqueIds every ~10s, creating duplicate sessions.
        var existingSession = await _ocppService.GetActiveSessionForConnectorAsync(
            connection.ChargePointId, request.ConnectorId);
        if (existingSession != null && existingSession.OcppTransactionId.HasValue)
        {
            _logger.LogInformation(
                "StartTransaction DEDUP: Connector {ConnectorId} on {ChargePointId} already has active session " +
                "{SessionId} with txnId={TransactionId}. Returning existing transactionId.",
                request.ConnectorId, connection.ChargePointId, existingSession.Id, existingSession.OcppTransactionId.Value);

            var dedupResponse = new StartTransactionResponse
            {
                TransactionId = existingSession.OcppTransactionId.Value,
                IdTagInfo = new IdTagInfo { Status = AuthorizationStatus.Accepted }
            };
            var dedupResult = parser.SerializeCallResult(uniqueId, dedupResponse);
            _logger.LogInformation("OCPP_RESPONSE StartTransaction DEDUP to {ChargePointId}: {Response}",
                connection.ChargePointId, dedupResult);
            return dedupResult;
        }

        // Generate transaction ID
        var transactionId = Math.Abs(Guid.NewGuid().GetHashCode());

        // Persist to database (BR-006-04)
        var sessionId = await _ocppService.HandleStartTransactionAsync(
            connection.ChargePointId,
            request.ConnectorId,
            request.IdTag ?? string.Empty,
            request.MeterStart,
            transactionId);

        _auditLogger.LogOcppEvent("StartTransaction", connection.ChargePointId,
            $"Connector={request.ConnectorId}, IdTag={request.IdTag}, TransactionId={transactionId}, SessionId={sessionId}");

        // Push real-time update via SignalR
        if (sessionId.HasValue && connection.StationId.HasValue)
        {
            await _notifier.NotifySessionUpdatedAsync(
                sessionId.Value,
                connection.StationId.Value,
                request.ConnectorId,
                SessionStatus.InProgress,
                0, 0);

            // Deliver webhook: SessionStarted
            if (_webhookService != null)
            {
                _ = _webhookService.EnqueueWebhookAsync(
                    WebhookEventType.SessionStarted,
                    connection.StationId.Value,
                    new
                    {
                        sessionId = sessionId.Value,
                        stationId = connection.StationId.Value,
                        connectorId = request.ConnectorId,
                        transactionId,
                        idTag = request.IdTag,
                        meterStart = request.MeterStart
                    });
            }
        }

        // Push notification: charging started
        // Use a fresh DI scope — push service queries device tokens from DB and must NOT
        // share the DbContext that the current handler UoW is still committing.
        if (sessionId.HasValue)
        {
            var capturedSessionId = sessionId.Value;
            var capturedConnectorId = request.ConnectorId;
            var capturedChargePointId = connection.ChargePointId;
            var scopeFactory = _scopeFactory;
            var logger = _logger;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                    var ocppService = scope.ServiceProvider.GetRequiredService<IOcppService>();
                    var pushService = scope.ServiceProvider.GetService<IPushNotificationService>();
                    if (pushService == null) return;

                    using var uow = uowManager.Begin(requiresNew: true);
                    var session = await ocppService.GetActiveSessionForConnectorAsync(
                        capturedChargePointId, capturedConnectorId);
                    await uow.CompleteAsync();

                    if (session != null && session.UserId != Guid.Empty)
                    {
                        using var pushScope = scopeFactory.CreateScope();
                        var pushUowManager = pushScope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                        var pushSvc = pushScope.ServiceProvider.GetRequiredService<IPushNotificationService>();
                        using var pushUow = pushUowManager.Begin(requiresNew: true);
                        await pushSvc.SendToUserAsync(
                            session.UserId,
                            "Đang sạc ⚡",
                            $"Phiên sạc đã bắt đầu tại cổng {capturedConnectorId}",
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "type", "session_started" },
                                { "sessionId", capturedSessionId.ToString() }
                            });
                        await pushUow.CompleteAsync();
                    }
                }
                catch (Exception ex) { logger.LogWarning(ex, "Push notification failed for session start"); }
            });
        }

        // Trigger immediate power rebalancing (new session changes load)
        _powerBalancingService?.TriggerRebalance();

        var response = new StartTransactionResponse
        {
            TransactionId = transactionId,
            IdTagInfo = new IdTagInfo
            {
                Status = sessionId.HasValue ? AuthorizationStatus.Accepted : AuthorizationStatus.Invalid
            }
        };

        var result = parser.SerializeCallResult(uniqueId, response);
        _logger.LogInformation("OCPP_RESPONSE StartTransaction to {ChargePointId}: {Response}",
            connection.ChargePointId, result);
        return result;
    }

    private async Task<string> HandleStopTransactionAsync(OcppConnection connection, string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        var request = JsonSerializer.Deserialize<StopTransactionRequest>(payload.GetRawText());
        if (request == null)
            return parser.SerializeCallError(uniqueId, OcppErrorCode.FormationViolation, "Invalid StopTransaction payload");

        _logger.LogInformation("StopTransaction from {ChargePointId}: TransactionId={TransactionId}, MeterStop={MeterStop}, Reason={Reason}, TransactionData={DataCount}",
            connection.ChargePointId, request.TransactionId, request.MeterStop, request.Reason,
            request.TransactionData?.Length ?? 0);

        // Process TransactionData meter values before stopping (BR-006-04b)
        if (request.TransactionData is { Length: > 0 })
        {
            var vendorProfile = _vendorProfileFactory.Resolve(connection.VendorProfileType);
            foreach (var mv in request.TransactionData)
            {
                var (energyWh, currentAmps, voltage, power, soc) = ExtractSampledValues(mv, vendorProfile);
                var parsedTimestamp = vendorProfile.ParseTimestamp(mv.Timestamp);
                var timestampStr = parsedTimestamp?.ToString("o") ?? mv.Timestamp;

                await _ocppService.HandleMeterValuesAsync(
                    connection.ChargePointId,
                    0, // connectorId not available in StopTransaction
                    request.TransactionId,
                    energyWh,
                    timestampStr,
                    currentAmps,
                    voltage,
                    power,
                    soc);
            }
        }

        // Persist to database (BR-006-04)
        var stopResult = await _ocppService.HandleStopTransactionAsync(
            request.TransactionId,
            request.MeterStop,
            request.Reason);

        _auditLogger.LogOcppEvent("StopTransaction", connection.ChargePointId,
            $"TransactionId={request.TransactionId}, MeterStop={request.MeterStop}, Reason={request.Reason}");

        // Push real-time update via SignalR
        if (stopResult != null)
        {
            await _notifier.NotifySessionUpdatedAsync(
                stopResult.SessionId,
                stopResult.StationId,
                stopResult.ConnectorNumber,
                SessionStatus.Completed,
                stopResult.TotalEnergyKwh,
                stopResult.TotalCost);

            // Notify connector reset to Available so the monitoring dashboard updates
            // immediately. Without this, the UI stays "Charging" until the charger
            // sends StatusNotification(Available), which may be delayed or never arrive.
            await _notifier.NotifyConnectorStatusChangedAsync(
                stopResult.StationId,
                stopResult.ConnectorNumber,
                ConnectorStatus.Charging,   // previous
                ConnectorStatus.Available); // new

            // Deliver webhook: SessionCompleted
            if (_webhookService != null)
            {
                _ = _webhookService.EnqueueWebhookAsync(
                    WebhookEventType.SessionCompleted,
                    stopResult.StationId,
                    new
                    {
                        sessionId = stopResult.SessionId,
                        stationId = stopResult.StationId,
                        connectorNumber = stopResult.ConnectorNumber,
                        totalEnergyKwh = stopResult.TotalEnergyKwh,
                        totalCost = stopResult.TotalCost,
                        stopReason = request.Reason
                    });
            }
        }

        // Push notification: charging completed
        // Use a fresh DI scope — push service queries device tokens from DB and must NOT
        // share the DbContext that the current handler UoW is still committing.
        if (stopResult != null && stopResult.UserId != Guid.Empty)
        {
            var userId = stopResult.UserId;
            var energy = stopResult.TotalEnergyKwh;
            var cost = stopResult.TotalCost;
            var capturedSessionId = stopResult.SessionId;
            var scopeFactory = _scopeFactory;
            var logger = _logger;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                    var pushSvc = scope.ServiceProvider.GetService<IPushNotificationService>();
                    if (pushSvc == null) return;

                    using var uow = uowManager.Begin(requiresNew: true);
                    await pushSvc.SendToUserAsync(
                        userId,
                        "Sạc hoàn tất ✅",
                        $"Đã sạc {energy:F2} kWh — Chi phí: {cost:N0}đ",
                        new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "type", "session_completed" },
                            { "sessionId", capturedSessionId.ToString() },
                            { "energyKwh", energy.ToString("F2") },
                            { "cost", cost.ToString("F0") }
                        });
                    await uow.CompleteAsync();
                }
                catch (Exception ex) { logger.LogWarning(ex, "Push notification failed for session complete"); }
            });
        }

        // Trigger immediate power rebalancing (freed capacity)
        _powerBalancingService?.TriggerRebalance();

        var response = new StopTransactionResponse
        {
            IdTagInfo = new IdTagInfo
            {
                Status = AuthorizationStatus.Accepted
            }
        };

        return parser.SerializeCallResult(uniqueId, response);
    }

    private async Task<string> HandleMeterValuesAsync(OcppConnection connection, string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        var request = JsonSerializer.Deserialize<MeterValuesRequest>(payload.GetRawText());
        if (request == null)
            return parser.SerializeCallError(uniqueId, OcppErrorCode.FormationViolation, "Invalid MeterValues payload");

        // Resolve vendor profile for this connection
        var vendorProfile = _vendorProfileFactory.Resolve(connection.VendorProfileType);

        _logger.LogInformation("MeterValues from {ChargePointId}: Connector={ConnectorId}, TransactionId={TransactionId}, " +
            "Values={Count}, VendorProfile={VendorProfile}",
            connection.ChargePointId, request.ConnectorId, request.TransactionId,
            request.MeterValue?.Length ?? 0, vendorProfile.ProfileType);

        // Process meter values (BR-006-05)
        if (request.MeterValue != null)
        {
            foreach (var mv in request.MeterValue)
            {
                var (energyWh, currentAmps, voltage, power, soc) = ExtractSampledValues(mv, vendorProfile);
                var parsedTimestamp = vendorProfile.ParseTimestamp(mv.Timestamp);
                var timestampStr = parsedTimestamp?.ToString("o") ?? mv.Timestamp;

                var meterResult = await _ocppService.HandleMeterValuesAsync(
                    connection.ChargePointId,
                    request.ConnectorId,
                    request.TransactionId,
                    energyWh,
                    timestampStr,
                    currentAmps,
                    voltage,
                    power,
                    soc);

                // Push real-time meter update via SignalR
                if (meterResult != null)
                {
                    await _notifier.NotifySessionUpdatedAsync(
                        meterResult.SessionId,
                        meterResult.StationId,
                        meterResult.ConnectorNumber,
                        SessionStatus.InProgress,
                        meterResult.TotalEnergyKwh,
                        meterResult.TotalCost);

                    await _notifier.NotifyMeterValueReceivedAsync(
                        meterResult.SessionId,
                        meterResult.TotalEnergyKwh,
                        meterResult.PowerKw,
                        meterResult.SocPercent);

                    // Auto-stop when battery is full (SoC ≥ 100%)
                    if (meterResult.SocPercent >= 100m && request.TransactionId.HasValue)
                    {
                        _logger.LogInformation(
                            "Battery full (SoC={SoC}%) for session {SessionId} on {ChargePointId}. Sending RemoteStopTransaction.",
                            meterResult.SocPercent, meterResult.SessionId, connection.ChargePointId);

                        // Fire-and-forget: must NOT await inside the message handler because
                        // SendCallAsync blocks waiting for the charger's Accepted response,
                        // but the WebSocket receive loop is blocked here → deadlock.
                        // We send the response first, then let the receive loop process the Accepted.
                        var chargePointId = connection.ChargePointId;
                        var ocppTxId = request.TransactionId.Value;
                        var sessionId = meterResult.SessionId;
                        var remoteCommandService = _remoteCommandService;
                        var logger = _logger;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // Brief delay so MeterValues response is sent and receive loop is free
                                await Task.Delay(200);
                                var stopResult = await remoteCommandService.SendRemoteStopTransactionAsync(
                                    chargePointId, ocppTxId);
                                if (stopResult.Accepted)
                                    logger.LogInformation("RemoteStopTransaction accepted for full-battery session {SessionId}", sessionId);
                                else
                                    logger.LogWarning("RemoteStopTransaction rejected for full-battery session {SessionId}: {Error}", sessionId, stopResult.ErrorMessage);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to send RemoteStopTransaction for full-battery session {SessionId}", sessionId);
                            }
                        });
                    }
                }
            }
        }

        return parser.SerializeCallResult(uniqueId, new MeterValuesResponse());
    }

    private async Task<string> HandleAuthorizeAsync(string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        var idTag = string.Empty;
        if (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("idTag", out var idTagElement))
        {
            idTag = idTagElement.GetString() ?? string.Empty;
        }

        var isValid = await _ocppService.ValidateIdTagAsync(idTag);

        _logger.LogInformation("Authorize request for idTag {IdTag}: {Status}", idTag, isValid ? "Accepted" : "Invalid");

        var response = new
        {
            idTagInfo = new IdTagInfo
            {
                Status = isValid ? AuthorizationStatus.Accepted : AuthorizationStatus.Invalid
            }
        };

        return parser.SerializeCallResult(uniqueId, response);
    }

    private string HandleDataTransfer(string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        _logger.LogDebug("DataTransfer received: {Payload}", payload.GetRawText());

        var response = new
        {
            status = "Accepted",
            data = (string?)null
        };

        return parser.SerializeCallResult(uniqueId, response);
    }

    private async Task<string> HandleDiagnosticsStatusNotificationAsync(OcppConnection connection, string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        var status = payload.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "Unknown";
        _logger.LogInformation("DiagnosticsStatusNotification from {ChargePointId}: Status={Status}",
            connection.ChargePointId, status);

        await _ocppService.HandleDiagnosticsStatusAsync(connection.ChargePointId, status ?? "Unknown");

        return parser.SerializeCallResult(uniqueId, new { });
    }

    private async Task<string> HandleFirmwareStatusNotificationAsync(OcppConnection connection, string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        var status = payload.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "Unknown";
        _logger.LogInformation("FirmwareStatusNotification from {ChargePointId}: Status={Status}",
            connection.ChargePointId, status);

        await _ocppService.HandleFirmwareStatusAsync(connection.ChargePointId, status ?? "Unknown");

        return parser.SerializeCallResult(uniqueId, new { });
    }

    private string? HandleCallResult(OcppConnection connection, ParsedOcppMessage parsed)
    {
        var payload = parsed.Payload.ValueKind != JsonValueKind.Undefined
            ? parsed.Payload.GetRawText()
            : "{}";

        if (connection.TryCompletePendingRequest(parsed.UniqueId, payload))
        {
            _logger.LogInformation("OCPP_CALL_RESULT from {ChargePointId} [uid={UniqueId}]: {Payload}",
                connection.ChargePointId, parsed.UniqueId, payload);
        }
        else
        {
            _logger.LogWarning("OCPP_CALL_RESULT unexpected from {ChargePointId} [uid={UniqueId}]: {Payload}",
                connection.ChargePointId, parsed.UniqueId, payload);
        }

        return null; // No response needed for CallResult
    }

    private string? HandleCallError(OcppConnection connection, ParsedOcppMessage parsed)
    {
        _logger.LogWarning("Received CallError from {ChargePointId}: {ErrorCode} - {ErrorDescription}",
            connection.ChargePointId, parsed.ErrorCode, parsed.ErrorDescription);

        connection.TryCompletePendingRequest(parsed.UniqueId, $"ERROR:{parsed.ErrorCode}:{parsed.ErrorDescription}");

        return null; // No response needed for CallError
    }

    private static (decimal energyWh, decimal? currentAmps, decimal? voltage, decimal? power, decimal? soc)
        ExtractSampledValues(Messages.MeterValue mv, Vendors.IVendorProfile vendorProfile)
    {
        decimal energyWh = 0;
        decimal? currentAmps = null;
        decimal? voltage = null;
        decimal? power = null;
        decimal? soc = null;

        if (mv.SampledValue != null)
        {
            foreach (var sv in mv.SampledValue)
            {
                switch (sv.Measurand)
                {
                    case "Energy.Active.Import.Register":
                        energyWh = vendorProfile.NormalizeEnergyToWh(sv.Value ?? "0", sv.Unit, sv.Measurand);
                        break;
                    case "Current.Import":
                        if (decimal.TryParse(sv.Value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var amps))
                            currentAmps = amps;
                        break;
                    case "Voltage":
                        if (decimal.TryParse(sv.Value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var volts))
                            voltage = volts;
                        break;
                    case "Power.Active.Import":
                        power = vendorProfile.NormalizePowerToW(sv.Value ?? "0", sv.Unit);
                        break;
                    case "SoC":
                        if (decimal.TryParse(sv.Value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var socVal))
                            soc = socVal;
                        break;
                }
            }
        }

        return (energyWh, currentAmps, voltage, power, soc);
    }

    private static ConnectorStatus MapOcppStatusToConnectorStatus(string? ocppStatus)
    {
        return ocppStatus?.ToLower() switch
        {
            "available" => ConnectorStatus.Available,
            "preparing" => ConnectorStatus.Preparing,
            "charging" => ConnectorStatus.Charging,
            "suspendedevse" => ConnectorStatus.SuspendedEVSE,
            "suspendedev" => ConnectorStatus.SuspendedEV,
            "finishing" => ConnectorStatus.Finishing,
            "reserved" => ConnectorStatus.Reserved,
            "unavailable" => ConnectorStatus.Unavailable,
            "faulted" => ConnectorStatus.Faulted,
            _ => ConnectorStatus.Available
        };
    }

}
