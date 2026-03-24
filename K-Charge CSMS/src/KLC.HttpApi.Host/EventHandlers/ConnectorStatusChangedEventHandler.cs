using KLC.Ocpp.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace KLC.EventHandlers;

public class ConnectorStatusChangedEventHandler : IDistributedEventHandler<ConnectorStatusChangedEto>, ITransientDependency
{
    private readonly IHubContext<Hubs.ChargingHub> _hubContext;
    private readonly ILogger<ConnectorStatusChangedEventHandler> _logger;

    public ConnectorStatusChangedEventHandler(
        IHubContext<Hubs.ChargingHub> hubContext,
        ILogger<ConnectorStatusChangedEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleEventAsync(ConnectorStatusChangedEto eventData)
    {
        try
        {
            _logger.LogInformation(
                "Broadcasting connector status change: ChargePointId={ChargePointId}, ConnectorId={ConnectorId}, Status={Status}",
                eventData.ChargePointId,
                eventData.ConnectorId,
                eventData.Status);

            var payload = new
            {
                chargePointId = eventData.ChargePointId,
                connectorId = eventData.ConnectorId,
                status = eventData.Status,
                errorCode = eventData.ErrorCode,
                timestamp = eventData.Timestamp
            };

            // Send to station-specific group
            await _hubContext.Clients
                .Group($"station:{eventData.ChargePointId}")
                .SendAsync("ConnectorStatusChanged", payload);

            // Send to dashboard
            await _hubContext.Clients
                .Group("dashboard")
                .SendAsync("ConnectorStatusChanged", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error broadcasting connector status change for ChargePointId={ChargePointId}",
                eventData.ChargePointId);
        }
    }
}
