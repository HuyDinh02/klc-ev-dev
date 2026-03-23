using KLC.Ocpp.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace KLC.EventHandlers;

public class ChargingSessionEventHandler :
    IDistributedEventHandler<ChargingSessionStartedEto>,
    IDistributedEventHandler<ChargingSessionCompletedEto>,
    ITransientDependency
{
    private readonly IHubContext<Hubs.ChargingHub> _hubContext;
    private readonly ILogger<ChargingSessionEventHandler> _logger;

    public ChargingSessionEventHandler(
        IHubContext<Hubs.ChargingHub> hubContext,
        ILogger<ChargingSessionEventHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task HandleEventAsync(ChargingSessionStartedEto eventData)
    {
        try
        {
            _logger.LogInformation(
                "Broadcasting charging session started: ChargePointId={ChargePointId}, TransactionId={TransactionId}, ConnectorId={ConnectorId}",
                eventData.ChargePointId,
                eventData.TransactionId,
                eventData.ConnectorId);

            var payload = new
            {
                chargePointId = eventData.ChargePointId,
                connectorId = eventData.ConnectorId,
                transactionId = eventData.TransactionId,
                idTag = eventData.IdTag,
                startTime = eventData.StartTime,
                meterStart = eventData.MeterStart
            };

            // Send to station-specific group
            await _hubContext.Clients
                .Group($"station:{eventData.ChargePointId}")
                .SendAsync("ChargingSessionStarted", payload);

            // Send to dashboard
            await _hubContext.Clients
                .Group("dashboard")
                .SendAsync("ChargingSessionStarted", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error broadcasting charging session started for ChargePointId={ChargePointId}",
                eventData.ChargePointId);
        }
    }

    public async Task HandleEventAsync(ChargingSessionCompletedEto eventData)
    {
        try
        {
            _logger.LogInformation(
                "Broadcasting charging session completed: ChargePointId={ChargePointId}, TransactionId={TransactionId}",
                eventData.ChargePointId,
                eventData.TransactionId);

            var payload = new
            {
                chargePointId = eventData.ChargePointId,
                connectorId = eventData.ConnectorId,
                transactionId = eventData.TransactionId,
                idTag = eventData.IdTag,
                startTime = eventData.StartTime,
                stopTime = eventData.StopTime,
                meterStart = eventData.MeterStart,
                meterStop = eventData.MeterStop,
                energyWh = eventData.EnergyWh,
                durationMinutes = eventData.DurationMinutes,
                cost = eventData.Cost,
                reason = eventData.Reason
            };

            // Send to station-specific group
            await _hubContext.Clients
                .Group($"station:{eventData.ChargePointId}")
                .SendAsync("ChargingSessionCompleted", payload);

            // Send to dashboard
            await _hubContext.Clients
                .Group("dashboard")
                .SendAsync("ChargingSessionCompleted", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error broadcasting charging session completed for ChargePointId={ChargePointId}",
                eventData.ChargePointId);
        }
    }
}
