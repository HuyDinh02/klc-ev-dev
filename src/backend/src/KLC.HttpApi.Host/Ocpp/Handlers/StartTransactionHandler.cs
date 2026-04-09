using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Auditing;
using KLC.Enums;
using KLC.Hubs;
using KLC.Notifications;
using KLC.Ocpp.Messages;
using KLC.Operators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Uow;

namespace KLC.Ocpp.Handlers;

public class StartTransactionHandler : IOcppActionHandler
{
    private readonly ILogger<StartTransactionHandler> _logger;
    private readonly IOcppService _ocppService;
    private readonly IMonitoringNotifier _notifier;
    private readonly IAuditEventLogger _auditLogger;
    private readonly PowerBalancingService? _powerBalancingService;
    private readonly IOperatorWebhookService? _webhookService;
    private readonly IServiceScopeFactory _scopeFactory;

    public string Action => "StartTransaction";

    public StartTransactionHandler(
        ILogger<StartTransactionHandler> logger,
        IOcppService ocppService,
        IMonitoringNotifier notifier,
        IAuditEventLogger auditLogger,
        IServiceScopeFactory scopeFactory,
        PowerBalancingService? powerBalancingService = null,
        IOperatorWebhookService? webhookService = null)
    {
        _logger = logger;
        _ocppService = ocppService;
        _notifier = notifier;
        _auditLogger = auditLogger;
        _scopeFactory = scopeFactory;
        _powerBalancingService = powerBalancingService;
        _webhookService = webhookService;
    }

    public async Task<string> HandleAsync(OcppHandlerContext context)
    {
        var request = JsonSerializer.Deserialize<StartTransactionRequest>(context.Payload.GetRawText());
        if (request == null)
            return context.Parser.SerializeCallError(context.UniqueId, OcppErrorCode.FormationViolation, "Invalid StartTransaction payload");

        _logger.LogInformation("StartTransaction from {ChargePointId}: Connector={ConnectorId}, IdTag={IdTag}, MeterStart={MeterStart}",
            context.Connection.ChargePointId, request.ConnectorId, request.IdTag, request.MeterStart);

        // Deduplicate: if this connector already has an active session, return the existing
        // transactionId instead of creating a new one.
        var existingSession = await _ocppService.GetActiveSessionForConnectorAsync(
            context.Connection.ChargePointId, request.ConnectorId);
        if (existingSession != null && existingSession.OcppTransactionId.HasValue)
        {
            _logger.LogInformation(
                "StartTransaction DEDUP: Connector {ConnectorId} on {ChargePointId} already has active session " +
                "{SessionId} with txnId={TransactionId}. Returning existing transactionId.",
                request.ConnectorId, context.Connection.ChargePointId, existingSession.Id, existingSession.OcppTransactionId.Value);

            var dedupResponse = new StartTransactionResponse
            {
                TransactionId = existingSession.OcppTransactionId.Value,
                IdTagInfo = new IdTagInfo { Status = AuthorizationStatus.Accepted }
            };
            var dedupResult = context.Parser.SerializeCallResult(context.UniqueId, dedupResponse);
            _logger.LogInformation("OCPP_RESPONSE StartTransaction DEDUP to {ChargePointId}: {Response}",
                context.Connection.ChargePointId, dedupResult);
            return dedupResult;
        }

        // Generate transaction ID
        var transactionId = Math.Abs(Guid.NewGuid().GetHashCode());

        // Persist to database (BR-006-04)
        var sessionId = await _ocppService.HandleStartTransactionAsync(
            context.Connection.ChargePointId,
            request.ConnectorId,
            request.IdTag ?? string.Empty,
            request.MeterStart,
            transactionId);

        _auditLogger.LogOcppEvent("StartTransaction", context.Connection.ChargePointId,
            $"Connector={request.ConnectorId}, IdTag={request.IdTag}, TransactionId={transactionId}, SessionId={sessionId}");

        // Push real-time update via SignalR
        if (sessionId.HasValue && context.Connection.StationId.HasValue)
        {
            await _notifier.NotifySessionUpdatedAsync(
                sessionId.Value,
                context.Connection.StationId.Value,
                request.ConnectorId,
                SessionStatus.InProgress,
                0, 0);

            // Deliver webhook: SessionStarted
            if (_webhookService != null)
            {
                _ = _webhookService.EnqueueWebhookAsync(
                    WebhookEventType.SessionStarted,
                    context.Connection.StationId.Value,
                    new
                    {
                        sessionId = sessionId.Value,
                        stationId = context.Connection.StationId.Value,
                        connectorId = request.ConnectorId,
                        transactionId,
                        idTag = request.IdTag,
                        meterStart = request.MeterStart
                    });
            }
        }

        // Push notification: charging started
        if (sessionId.HasValue)
        {
            var capturedSessionId = sessionId.Value;
            var capturedConnectorId = request.ConnectorId;
            var capturedChargePointId = context.Connection.ChargePointId;
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
                            new Dictionary<string, string>
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

        var result = context.Parser.SerializeCallResult(context.UniqueId, response);
        _logger.LogInformation("OCPP_RESPONSE StartTransaction to {ChargePointId}: {Response}",
            context.Connection.ChargePointId, result);
        return result;
    }
}
