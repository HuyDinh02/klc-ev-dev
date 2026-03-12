using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Auditing;
using KLC.Enums;
using KLC.Hubs;
using KLC.Ocpp.Messages;
using KLC.Ocpp.Vendors;
using KLC.Operators;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

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
        PowerBalancingService? powerBalancingService = null,
        IOperatorWebhookService? webhookService = null)
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
        _powerBalancingService = powerBalancingService;
        _webhookService = webhookService;
    }

    /// <summary>
    /// Process an incoming OCPP message.
    /// </summary>
    public async Task<string?> HandleMessageAsync(OcppConnection connection, string message)
    {
        try
        {
            _logger.LogDebug("Received from {ChargePointId} ({OcppVersion}): {Message}",
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

            await _rawEventRepository.InsertAsync(rawEvent, autoSave: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist raw OCPP event for {ChargePointId}/{Action}",
                connection.ChargePointId, action);
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

        // Persist to database (including vendor profile)
        var stationId = await _ocppService.HandleBootNotificationAsync(
            connection.ChargePointId,
            request.ChargePointVendor ?? string.Empty,
            request.ChargePointModel ?? string.Empty,
            request.ChargePointSerialNumber,
            request.FirmwareVersion);

        // Persist vendor profile on station entity
        if (stationId.HasValue)
        {
            var station = await _ocppService.GetStationByChargePointIdAsync(connection.ChargePointId);
            if (station != null && station.VendorProfile != vendorProfile.ProfileType)
            {
                station.SetVendorProfile(vendorProfile.ProfileType);
            }
        }

        connection.RecordHeartbeat();

        // Register the station ID on the connection for SignalR notifications
        if (stationId.HasValue)
        {
            connection.SetRegistered(stationId.Value);
        }

        // Reject unknown stations (BR-006-02)
        var status = stationId.HasValue ? RegistrationStatus.Accepted : RegistrationStatus.Rejected;

        _auditLogger.LogOcppEvent("BootNotification", connection.ChargePointId,
            $"Vendor={request.ChargePointVendor}, Model={request.ChargePointModel}, Status={status}");

        var response = new BootNotificationResponse
        {
            Status = status,
            CurrentTime = DateTime.UtcNow.ToString("o"),
            Interval = vendorProfile.HeartbeatIntervalSeconds
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

        return parser.SerializeCallResult(uniqueId, new StatusNotificationResponse());
    }

    private async Task<string> HandleStartTransactionAsync(OcppConnection connection, string uniqueId, JsonElement payload, IOcppMessageParser parser)
    {
        var request = JsonSerializer.Deserialize<StartTransactionRequest>(payload.GetRawText());
        if (request == null)
            return parser.SerializeCallError(uniqueId, OcppErrorCode.FormationViolation, "Invalid StartTransaction payload");

        _logger.LogInformation("StartTransaction from {ChargePointId}: Connector={ConnectorId}, IdTag={IdTag}, MeterStart={MeterStart}",
            connection.ChargePointId, request.ConnectorId, request.IdTag, request.MeterStart);

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

        return parser.SerializeCallResult(uniqueId, response);
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

        _logger.LogDebug("MeterValues from {ChargePointId}: Connector={ConnectorId}, TransactionId={TransactionId}, " +
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
            _logger.LogDebug("Received CallResult for {UniqueId} from {ChargePointId}",
                parsed.UniqueId, connection.ChargePointId);
        }
        else
        {
            _logger.LogWarning("Received unexpected CallResult for {UniqueId} from {ChargePointId}",
                parsed.UniqueId, connection.ChargePointId);
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
