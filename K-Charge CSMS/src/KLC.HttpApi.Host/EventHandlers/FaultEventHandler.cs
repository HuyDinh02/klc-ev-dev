using KLC.Ocpp.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace KLC.EventHandlers;

public class FaultEventHandler : IDistributedEventHandler<FaultDetectedEto>, ITransientDependency
{
    private readonly IHubContext<Hubs.ChargingHub> _hubContext;
    private readonly ILogger<FaultEventHandler> _logger;

    public FaultEventHandler(
        IHubContext<Hubs.ChargingHub> hubContext,
        ILogger<FaultEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleEventAsync(FaultDetectedEto eventData)
    {
        try
        {
            _logger.LogWarning(
                "Broadcasting fault detected: ChargePointId={ChargePointId}, ConnectorId={ConnectorId}, ErrorCode={ErrorCode}",
                eventData.ChargePointId,
                eventData.ConnectorId,
                eventData.ErrorCode);

            var payload = new
            {
                chargePointId = eventData.ChargePointId,
                connectorId = eventData.ConnectorId,
                errorCode = eventData.ErrorCode,
                info = eventData.Info,
                severity = eventData.Severity,
                timestamp = eventData.Timestamp
            };

            // Send to station-specific group
            await _hubContext.Clients
                .Group($"station:{eventData.ChargePointId}")
                .SendAsync("FaultDetected", payload);

            // Send to dashboard
            await _hubContext.Clients
                .Group("dashboard")
                .SendAsync("FaultDetected", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error broadcasting fault detection for ChargePointId={ChargePointId}",
                eventData.ChargePointId);
        }
    }
}
