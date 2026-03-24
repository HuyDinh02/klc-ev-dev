using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using KLC.Ocpp.Handlers;

namespace KLC.Ocpp;

/// <summary>
/// Routes incoming OCPP CALL messages to appropriate handlers.
/// Processes CALLRESULT/CALLERROR responses for pending outbound requests.
/// </summary>
public class OcppMessageRouter
{
    private static readonly Dictionary<string, Type> HandlerMap = new()
    {
        ["Authorize"] = typeof(AuthorizeHandler),
        ["BootNotification"] = typeof(BootNotificationHandler),
        ["DataTransfer"] = typeof(DataTransferHandler),
        ["DiagnosticsStatusNotification"] = typeof(DiagnosticsStatusNotificationHandler),
        ["FirmwareStatusNotification"] = typeof(FirmwareStatusNotificationHandler),
        ["Heartbeat"] = typeof(HeartbeatHandler),
        ["MeterValues"] = typeof(MeterValuesHandler),
        ["StartTransaction"] = typeof(StartTransactionHandler),
        ["StatusNotification"] = typeof(StatusNotificationHandler),
        ["StopTransaction"] = typeof(StopTransactionHandler),
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OcppMessageRouter> _logger;

    public OcppMessageRouter(
        IServiceProvider serviceProvider,
        ILogger<OcppMessageRouter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Route an incoming OCPP message based on its type.
    /// </summary>
    /// <returns>Response JArray to send back, or null if no response needed.</returns>
    public async Task<JArray?> RouteAsync(
        string chargePointId,
        JArray message,
        OcppConnection connection,
        CancellationToken cancellationToken)
    {
        if (message.Count < 1)
        {
            _logger.LogWarning("Received empty message from {ChargePointId}", chargePointId);
            return null;
        }

        var messageType = message[0].Value<int>();

        return messageType switch
        {
            2 => await HandleCallAsync(chargePointId, message, cancellationToken),
            3 => HandleCallResult(chargePointId, message, connection),
            4 => HandleCallError(chargePointId, message, connection),
            _ => LogAndIgnore(chargePointId, messageType)
        };
    }

    private async Task<JArray> HandleCallAsync(
        string chargePointId, JArray message, CancellationToken ct)
    {
        if (message.Count < 3)
        {
            return CreateCallError("0", "FormationViolation", "CALL requires at least 3 elements");
        }

        var uniqueId = message[1].Value<string>() ?? "0";
        var action = message[2].Value<string>() ?? "";
        var payload = message.Count > 3 && message[3] is JObject obj ? obj : new JObject();

        _logger.LogInformation(
            "CALL from {ChargePointId}: {Action} (id={UniqueId})",
            chargePointId, action, uniqueId);

        if (!HandlerMap.TryGetValue(action, out var handlerType))
        {
            _logger.LogWarning("No handler for action {Action} from {ChargePointId}", action, chargePointId);
            return CreateCallError(uniqueId, "NotImplemented", $"Action '{action}' is not supported");
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = (IOcppMessageHandler)scope.ServiceProvider.GetRequiredService(handlerType);

            var result = await handler.HandleAsync(chargePointId, payload, ct);

            _logger.LogDebug(
                "CALLRESULT for {ChargePointId}/{Action}: {Result}",
                chargePointId, action, result?.ToString(Newtonsoft.Json.Formatting.None));

            return new JArray(3, uniqueId, result ?? new JObject());
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Handler for {Action} was cancelled for {ChargePointId}", action, chargePointId);
            return CreateCallError(uniqueId, "InternalError", "Request was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler exception for {Action} from {ChargePointId}", action, chargePointId);
            return CreateCallError(uniqueId, "InternalError", "Internal server error");
        }
    }

    private JArray? HandleCallResult(string chargePointId, JArray message, OcppConnection connection)
    {
        if (message.Count < 3)
        {
            _logger.LogWarning("CALLRESULT with insufficient fields from {ChargePointId}", chargePointId);
            return null;
        }

        var uniqueId = message[1].Value<string>();
        if (string.IsNullOrEmpty(uniqueId))
        {
            _logger.LogWarning("CALLRESULT with empty uniqueId from {ChargePointId}", chargePointId);
            return null;
        }

        if (connection.PendingRequests.TryRemove(uniqueId, out var tcs))
        {
            // Pass the payload wrapped in JArray for the dispatcher to extract
            var payload = message[2] as JObject ?? new JObject();
            tcs.SetResult(new JArray(payload));

            _logger.LogDebug("CALLRESULT matched pending request {UniqueId} from {ChargePointId}",
                uniqueId, chargePointId);
        }
        else
        {
            _logger.LogWarning("CALLRESULT for unknown request {UniqueId} from {ChargePointId}",
                uniqueId, chargePointId);
        }

        return null; // No response needed for CALLRESULT
    }

    private JArray? HandleCallError(string chargePointId, JArray message, OcppConnection connection)
    {
        if (message.Count < 4)
        {
            _logger.LogWarning("CALLERROR with insufficient fields from {ChargePointId}", chargePointId);
            return null;
        }

        var uniqueId = message[1].Value<string>();
        var errorCode = message[2].Value<string>() ?? "InternalError";
        var errorDescription = message[3].Value<string>() ?? "";
        var errorDetails = message.Count > 4 && message[4] is JObject details ? details : new JObject();

        _logger.LogWarning(
            "CALLERROR from {ChargePointId}: id={UniqueId}, code={ErrorCode}, desc={ErrorDesc}",
            chargePointId, uniqueId, errorCode, errorDescription);

        if (!string.IsNullOrEmpty(uniqueId) &&
            connection.PendingRequests.TryRemove(uniqueId, out var tcs))
        {
            tcs.SetException(new OcppCallErrorException(errorCode, errorDescription, errorDetails));
        }

        return null;
    }

    private JArray? LogAndIgnore(string chargePointId, int messageType)
    {
        _logger.LogWarning("Unknown message type {Type} from {ChargePointId}", messageType, chargePointId);
        return null;
    }

    private static JArray CreateCallError(string uniqueId, string errorCode, string description)
    {
        return new JArray(4, uniqueId, errorCode, description, new JObject());
    }
}
