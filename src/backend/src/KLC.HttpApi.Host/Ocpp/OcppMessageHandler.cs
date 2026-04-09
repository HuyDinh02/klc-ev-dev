using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Ocpp.Handlers;
using KLC.Ocpp.Messages;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace KLC.Ocpp;

/// <summary>
/// Thin coordinator for OCPP 1.6J messages from Charge Points.
/// Delegates action-specific handling to IOcppActionHandler implementations (Strategy Pattern).
/// Retains: message parsing/dispatch, HandleCallResult, HandleCallError, PersistRawEventAsync.
/// </summary>
public class OcppMessageHandler
{
    private readonly ILogger<OcppMessageHandler> _logger;
    private readonly OcppMessageParserFactory _parserFactory;
    private readonly IRepository<OcppRawEvent, Guid> _rawEventRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IOcppService _ocppService;
    private readonly Dictionary<string, IOcppActionHandler> _handlers;

    public OcppMessageHandler(
        ILogger<OcppMessageHandler> logger,
        OcppMessageParserFactory parserFactory,
        IRepository<OcppRawEvent, Guid> rawEventRepository,
        IGuidGenerator guidGenerator,
        IOcppService ocppService,
        IEnumerable<IOcppActionHandler> handlers)
    {
        _logger = logger;
        _parserFactory = parserFactory;
        _rawEventRepository = rawEventRepository;
        _guidGenerator = guidGenerator;
        _ocppService = ocppService;
        _handlers = handlers.ToDictionary(h => h.Action, h => h, StringComparer.OrdinalIgnoreCase);
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

        // OCPP 1.6J idempotency: return cached response for retried uniqueIds
        var cachedResponse = connection.GetCachedResponse(uniqueId);
        if (cachedResponse != null)
        {
            _logger.LogWarning(
                "Idempotent retry: returning cached response for {Action} uid={UniqueId} from {ChargePointId}",
                action, uniqueId, connection.ChargePointId);
            return cachedResponse;
        }

        string result;

        // Delegate to registered action handlers
        if (_handlers.TryGetValue(action, out var handler))
        {
            var context = new OcppHandlerContext
            {
                Connection = connection,
                UniqueId = uniqueId,
                Payload = payload,
                Parser = parser
            };
            result = await handler.HandleAsync(context);
        }
        else
        {
            // Handle actions that don't have dedicated handlers
            result = action switch
            {
                "DiagnosticsStatusNotification" => await HandleDiagnosticsStatusNotificationAsync(connection, uniqueId, payload, parser),
                "FirmwareStatusNotification" => await HandleFirmwareStatusNotificationAsync(connection, uniqueId, payload, parser),
                _ => parser.SerializeCallError(uniqueId, OcppErrorCode.NotImplemented, $"Action {action} not implemented")
            };
        }

        sw.Stop();

        _logger.LogInformation(
            "Completed {Action} from {ChargePointId} [uid={UniqueId}] in {LatencyMs}ms",
            action, connection.ChargePointId, uniqueId, sw.ElapsedMilliseconds);

        // Cache response for idempotency
        connection.CacheResponse(uniqueId, result, TimeSpan.FromMinutes(5));

        // Persist raw event for auditable actions
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

            await _rawEventRepository.InsertAsync(rawEvent, autoSave: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCPP_RAW_PERSIST_FAIL: Failed to persist raw OCPP event for {ChargePointId}/{Action}/{UniqueId}",
                connection.ChargePointId, action, uniqueId);
        }
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

        return null;
    }

    private string? HandleCallError(OcppConnection connection, ParsedOcppMessage parsed)
    {
        _logger.LogWarning("Received CallError from {ChargePointId}: {ErrorCode} - {ErrorDescription}",
            connection.ChargePointId, parsed.ErrorCode, parsed.ErrorDescription);

        connection.TryCompletePendingRequest(parsed.UniqueId, $"ERROR:{parsed.ErrorCode}:{parsed.ErrorDescription}");

        return null;
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
}
