using KLC.Ocpp.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace KLC.EventHandlers;

public class MeterValueEventHandler : IDistributedEventHandler<MeterValueReceivedEto>, ITransientDependency
{
    private readonly IHubContext<Hubs.ChargingHub> _hubContext;
    private readonly ILogger<MeterValueEventHandler> _logger;

    public MeterValueEventHandler(
        IHubContext<Hubs.ChargingHub> hubContext,
        ILogger<MeterValueEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleEventAsync(MeterValueReceivedEto eventData)
    {
        try
        {
            _logger.LogDebug(
                "Broadcasting meter value: ChargePointId={ChargePointId}, ConnectorId={ConnectorId}, EnergyWh={EnergyWh}, PowerW={PowerW}",
                eventData.ChargePointId,
                eventData.ConnectorId,
                eventData.EnergyWh,
                eventData.PowerW);

            var payload = new
            {
                chargePointId = eventData.ChargePointId,
                connectorId = eventData.ConnectorId,
                transactionId = eventData.TransactionId,
                energyWh = eventData.EnergyWh,
                powerW = eventData.PowerW,
                currentA = eventData.CurrentA,
                voltageV = eventData.VoltageV,
                socPercent = eventData.SocPercent,
                timestamp = eventData.Timestamp
            };

            // Send to station-specific group only (meter values are frequent)
            await _hubContext.Clients
                .Group($"station:{eventData.ChargePointId}")
                .SendAsync("MeterValueReceived", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error broadcasting meter value for ChargePointId={ChargePointId}",
                eventData.ChargePointId);
        }
    }
}
