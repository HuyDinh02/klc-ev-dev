using System;
using System.Threading.Tasks;
using KLC.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KLC.Hubs;

/// <summary>
/// Service for sending real-time monitoring updates to connected clients.
/// </summary>
public interface IMonitoringNotifier
{
    Task NotifyStationStatusChangedAsync(
        Guid stationId,
        string stationName,
        StationStatus previousStatus,
        StationStatus newStatus);

    Task NotifyConnectorStatusChangedAsync(
        Guid stationId,
        int connectorNumber,
        ConnectorStatus previousStatus,
        ConnectorStatus newStatus);

    Task NotifyAlertCreatedAsync(
        Guid alertId,
        Guid? stationId,
        string? stationName,
        string alertType,
        string message);

    Task NotifySessionUpdatedAsync(
        Guid sessionId,
        Guid stationId,
        int connectorNumber,
        SessionStatus status,
        decimal currentEnergyKwh,
        decimal currentCost);

    Task NotifyMeterValueReceivedAsync(
        Guid sessionId,
        decimal energyKwh,
        decimal? powerKw,
        decimal? socPercent);

    Task NotifyPowerAllocationChangedAsync(PowerAllocationUpdate update);
}

public class MonitoringNotifier : IMonitoringNotifier
{
    private readonly IHubContext<MonitoringHub, IMonitoringHubClient> _hubContext;
    private readonly ILogger<MonitoringNotifier> _logger;

    public MonitoringNotifier(
        IHubContext<MonitoringHub, IMonitoringHubClient> hubContext,
        ILogger<MonitoringNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyStationStatusChangedAsync(
        Guid stationId,
        string stationName,
        StationStatus previousStatus,
        StationStatus newStatus)
    {
        var update = new StationStatusUpdate(
            stationId,
            stationName,
            previousStatus,
            newStatus,
            DateTime.UtcNow
        );

        // Notify all monitoring clients and station-specific subscribers
        await _hubContext.Clients.Group("Monitoring").OnStationStatusChanged(update);
        await _hubContext.Clients.Group($"Station:{stationId}").OnStationStatusChanged(update);
    }

    public async Task NotifyConnectorStatusChangedAsync(
        Guid stationId,
        int connectorNumber,
        ConnectorStatus previousStatus,
        ConnectorStatus newStatus)
    {
        var update = new ConnectorStatusUpdate(
            stationId,
            connectorNumber,
            previousStatus,
            newStatus,
            DateTime.UtcNow
        );

        // Notify all monitoring clients and station-specific subscribers
        await _hubContext.Clients.Group("Monitoring").OnConnectorStatusChanged(update);
        await _hubContext.Clients.Group($"Station:{stationId}").OnConnectorStatusChanged(update);
    }

    public async Task NotifyAlertCreatedAsync(
        Guid alertId,
        Guid? stationId,
        string? stationName,
        string alertType,
        string message)
    {
        var alert = new AlertNotification(
            alertId,
            stationId,
            stationName,
            alertType,
            message,
            DateTime.UtcNow
        );

        // Notify all monitoring clients
        await _hubContext.Clients.Group("Monitoring").OnAlertCreated(alert);

        // Also notify station-specific subscribers if applicable
        if (stationId.HasValue)
        {
            await _hubContext.Clients.Group($"Station:{stationId}").OnAlertCreated(alert);
        }
    }

    public async Task NotifySessionUpdatedAsync(
        Guid sessionId,
        Guid stationId,
        int connectorNumber,
        SessionStatus status,
        decimal currentEnergyKwh,
        decimal currentCost)
    {
        var update = new SessionUpdate(
            sessionId,
            stationId,
            connectorNumber,
            status,
            currentEnergyKwh,
            currentCost,
            DateTime.UtcNow
        );

        _logger.LogInformation("Sending SessionUpdate via SignalR: Session={SessionId}, Status={Status}, Energy={Energy}kWh",
            sessionId, status, currentEnergyKwh);

        // Notify all monitoring clients (admin dashboard)
        await _hubContext.Clients.Group("Monitoring").OnSessionUpdated(update);

        // Notify session subscribers (driver app)
        await _hubContext.Clients.Group($"Session:{sessionId}").OnSessionUpdated(update);

        // Also notify station subscribers
        await _hubContext.Clients.Group($"Station:{stationId}").OnSessionUpdated(update);
    }

    public async Task NotifyMeterValueReceivedAsync(
        Guid sessionId,
        decimal energyKwh,
        decimal? powerKw,
        decimal? socPercent)
    {
        var update = new MeterValueUpdate(
            sessionId,
            energyKwh,
            powerKw,
            socPercent,
            DateTime.UtcNow
        );

        // Notify session subscribers
        await _hubContext.Clients.Group($"Session:{sessionId}").OnMeterValueReceived(update);
    }

    public async Task NotifyPowerAllocationChangedAsync(PowerAllocationUpdate update)
    {
        // Notify all monitoring clients (admin dashboard)
        await _hubContext.Clients.Group("Monitoring").OnPowerAllocationChanged(update);

        // Notify group-specific subscribers (power sharing detail page)
        await _hubContext.Clients.Group($"PowerSharing:{update.GroupId}").OnPowerAllocationChanged(update);
    }
}
