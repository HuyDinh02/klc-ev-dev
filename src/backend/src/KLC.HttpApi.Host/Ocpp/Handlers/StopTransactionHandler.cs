using System;
using System.Collections.Generic;
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
using Volo.Abp.Uow;

namespace KLC.Ocpp.Handlers;

public class StopTransactionHandler : IOcppActionHandler
{
    private readonly ILogger<StopTransactionHandler> _logger;
    private readonly IOcppService _ocppService;
    private readonly IMonitoringNotifier _notifier;
    private readonly VendorProfileFactory _vendorProfileFactory;
    private readonly IAuditEventLogger _auditLogger;
    private readonly PowerBalancingService? _powerBalancingService;
    private readonly IOperatorWebhookService? _webhookService;
    private readonly IServiceScopeFactory _scopeFactory;

    public string Action => "StopTransaction";

    public StopTransactionHandler(
        ILogger<StopTransactionHandler> logger,
        IOcppService ocppService,
        IMonitoringNotifier notifier,
        VendorProfileFactory vendorProfileFactory,
        IAuditEventLogger auditLogger,
        IServiceScopeFactory scopeFactory,
        PowerBalancingService? powerBalancingService = null,
        IOperatorWebhookService? webhookService = null)
    {
        _logger = logger;
        _ocppService = ocppService;
        _notifier = notifier;
        _vendorProfileFactory = vendorProfileFactory;
        _auditLogger = auditLogger;
        _scopeFactory = scopeFactory;
        _powerBalancingService = powerBalancingService;
        _webhookService = webhookService;
    }

    public async Task<string> HandleAsync(OcppHandlerContext context)
    {
        var request = JsonSerializer.Deserialize<StopTransactionRequest>(context.Payload.GetRawText());
        if (request == null)
            return context.Parser.SerializeCallError(context.UniqueId, OcppErrorCode.FormationViolation, "Invalid StopTransaction payload");

        _logger.LogInformation("StopTransaction from {ChargePointId}: TransactionId={TransactionId}, MeterStop={MeterStop}, Reason={Reason}, TransactionData={DataCount}",
            context.Connection.ChargePointId, request.TransactionId, request.MeterStop, request.Reason,
            request.TransactionData?.Length ?? 0);

        // Process TransactionData meter values before stopping (BR-006-04b)
        if (request.TransactionData is { Length: > 0 })
        {
            var vendorProfile = _vendorProfileFactory.Resolve(context.Connection.VendorProfileType);
            foreach (var mv in request.TransactionData)
            {
                var (energyWh, currentAmps, voltage, power, soc) = ExtractSampledValues(mv, vendorProfile);
                var parsedTimestamp = vendorProfile.ParseTimestamp(mv.Timestamp);
                var timestampStr = parsedTimestamp?.ToString("o") ?? mv.Timestamp;

                await _ocppService.HandleMeterValuesAsync(
                    context.Connection.ChargePointId,
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

        _auditLogger.LogOcppEvent("StopTransaction", context.Connection.ChargePointId,
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

            // Notify connector reset to Available
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
                        new Dictionary<string, string>
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

        return context.Parser.SerializeCallResult(context.UniqueId, response);
    }

    private static (decimal energyWh, decimal? currentAmps, decimal? voltage, decimal? power, decimal? soc)
        ExtractSampledValues(MeterValue mv, IVendorProfile vendorProfile)
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
}
