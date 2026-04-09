using System;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Hubs;
using KLC.Ocpp.Messages;
using KLC.Operators;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Handlers;

public class StatusNotificationHandler : IOcppActionHandler
{
    private readonly ILogger<StatusNotificationHandler> _logger;
    private readonly IOcppService _ocppService;
    private readonly IMonitoringNotifier _notifier;
    private readonly IOperatorWebhookService? _webhookService;

    public string Action => "StatusNotification";

    public StatusNotificationHandler(
        ILogger<StatusNotificationHandler> logger,
        IOcppService ocppService,
        IMonitoringNotifier notifier,
        IOperatorWebhookService? webhookService = null)
    {
        _logger = logger;
        _ocppService = ocppService;
        _notifier = notifier;
        _webhookService = webhookService;
    }

    public async Task<string> HandleAsync(OcppHandlerContext context)
    {
        var request = JsonSerializer.Deserialize<StatusNotificationRequest>(context.Payload.GetRawText());
        if (request == null)
            return context.Parser.SerializeCallError(context.UniqueId, OcppErrorCode.FormationViolation, "Invalid StatusNotification payload");

        _logger.LogInformation("StatusNotification from {ChargePointId}: Connector={ConnectorId}, Status={Status}, Error={ErrorCode}",
            context.Connection.ChargePointId, request.ConnectorId, request.Status, request.ErrorCode);

        // Map OCPP status to domain status
        var connectorStatus = MapOcppStatusToConnectorStatus(request.Status);

        // Persist to database + escalate errors to Fault entities
        var statusResult = await _ocppService.HandleStatusNotificationAsync(
            context.Connection.ChargePointId,
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
                    context.Connection.ChargePointId, request.ConnectorId);

                if (session != null && session.OcppTransactionId.HasValue)
                {
                    _logger.LogInformation(
                        "Connector {ConnectorId} on {ChargePointId} went {Status} — auto-completing session {SessionId} (txn={TxnId})",
                        request.ConnectorId, context.Connection.ChargePointId, statusResult.NewStatus,
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

        return context.Parser.SerializeCallResult(context.UniqueId, new StatusNotificationResponse());
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
