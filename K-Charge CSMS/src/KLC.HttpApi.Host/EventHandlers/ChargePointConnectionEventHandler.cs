using KLC.Ocpp.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace KLC.EventHandlers;

public class ChargePointConnectionEventHandler : IDistributedEventHandler<ChargePointConnectedEto>, ITransientDependency
{
    private readonly IHubContext<Hubs.ChargingHub> _hubContext;
    private readonly ILogger<ChargePointConnectionEventHandler> _logger;

    public ChargePointConnectionEventHandler(
        IHubContext<Hubs.ChargingHub> hubContext,
        ILogger<ChargePointConnectionEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleEventAsync(ChargePointConnectedEto eventData)
    {
        try
        {
            _logger.LogInformation(
                "Broadcasting charge point connection: ChargePointId={ChargePointId}, Model={Model}, Vendor={Vendor}",
                eventData.ChargePointId,
                eventData.Model,
                eventData.Vendor);

            var payload = new
            {
                chargePointId = eventData.ChargePointId,
                vendor = eventData.Vendor,
                model = eventData.Model,
                firmwareVersion = eventData.FirmwareVersion,
                serialNumber = eventData.SerialNumber,
                connectedAt = eventData.ConnectedAt
            };

            // Send to dashboard
            await _hubContext.Clients
                .Group("dashboard")
                .SendAsync("ChargePointConnected", payload);

            // Also send to station-specific group
            await _hubContext.Clients
                .Group($"station:{eventData.ChargePointId}")
                .SendAsync("ChargePointConnected", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error broadcasting charge point connection for ChargePointId={ChargePointId}",
                eventData.ChargePointId);
        }
    }
}
