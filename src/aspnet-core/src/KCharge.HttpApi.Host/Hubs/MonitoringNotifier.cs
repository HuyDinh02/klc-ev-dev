using System;
using System.Threading.Tasks;
using KCharge.Enums;
using Microsoft.AspNetCore.SignalR;

namespace KCharge.Hubs;

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
}

public class MonitoringNotifier : IMonitoringNotifier
{
    private readonly IHubContext<MonitoringHub, IMonitoringHubClient> _hubContext;

    public MonitoringNotifier(IHubContext<MonitoringHub, IMonitoringHubClient> hubContext)
    {
        _hubContext = hubContext;
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
}
