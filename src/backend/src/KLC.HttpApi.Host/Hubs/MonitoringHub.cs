using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Permissions;
using KLC.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KLC.Hubs;

/// <summary>
/// SignalR Hub for real-time monitoring updates.
/// Clients can subscribe to station status updates, alerts, and session updates.
/// </summary>
[Authorize]
public class MonitoringHub : Hub<IMonitoringHubClient>
{
    private readonly IAuthorizationService _authorizationService;

    public MonitoringHub(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public override async Task OnConnectedAsync()
    {
        // SEC-3: verify caller holds Monitoring.Default permission before joining the group.
        // [Authorize] only validates the JWT; it does not check ABP permissions.
        var authResult = await _authorizationService.AuthorizeAsync(
            Context.User!, KLCPermissions.Monitoring.Default);
        if (!authResult.Succeeded)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "Monitoring");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Monitoring");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to updates for a specific station.
    /// </summary>
    public async Task SubscribeToStation(Guid stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Station:{stationId}");
    }

    /// <summary>
    /// Unsubscribe from updates for a specific station.
    /// </summary>
    public async Task UnsubscribeFromStation(Guid stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Station:{stationId}");
    }

    /// <summary>
    /// Subscribe to updates for a specific power sharing group.
    /// </summary>
    public async Task SubscribeToPowerSharingGroup(Guid groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"PowerSharing:{groupId}");
    }

    /// <summary>
    /// Unsubscribe from power sharing group updates.
    /// </summary>
    public async Task UnsubscribeFromPowerSharingGroup(Guid groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"PowerSharing:{groupId}");
    }

    /// <summary>
    /// Subscribe to updates for a specific session (for driver app).
    /// </summary>
    public async Task SubscribeToSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Session:{sessionId}");
    }

    /// <summary>
    /// Unsubscribe from session updates.
    /// </summary>
    public async Task UnsubscribeFromSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session:{sessionId}");
    }
}

/// <summary>
/// Interface for sending monitoring updates to clients.
/// </summary>
public interface IMonitoringHubClient
{
    Task OnStationStatusChanged(StationStatusUpdate update);
    Task OnConnectorStatusChanged(ConnectorStatusUpdate update);
    Task OnAlertCreated(AlertNotification alert);
    Task OnSessionUpdated(SessionUpdate update);
    Task OnMeterValueReceived(MeterValueUpdate update);
    Task OnPowerAllocationChanged(PowerAllocationUpdate update);
}

// Update DTOs for SignalR messages
public record StationStatusUpdate(
    Guid StationId,
    string StationName,
    StationStatus PreviousStatus,
    StationStatus NewStatus,
    DateTime Timestamp
);

public record ConnectorStatusUpdate(
    Guid StationId,
    int ConnectorNumber,
    ConnectorStatus PreviousStatus,
    ConnectorStatus NewStatus,
    DateTime Timestamp
);

public record AlertNotification(
    Guid AlertId,
    Guid? StationId,
    string? StationName,
    string AlertType,
    string Message,
    DateTime Timestamp
);

public record SessionUpdate(
    Guid SessionId,
    Guid StationId,
    int ConnectorNumber,
    SessionStatus Status,
    decimal CurrentEnergyKwh,
    decimal CurrentCost,
    DateTime Timestamp
);

public record MeterValueUpdate(
    Guid SessionId,
    decimal EnergyKwh,
    decimal? PowerKw,
    decimal? SocPercent,
    DateTime Timestamp
);

public record PowerAllocationUpdate(
    Guid GroupId,
    string GroupName,
    decimal TotalCapacityKw,
    decimal TotalAllocatedKw,
    int ActiveConnectors,
    int ProfilesDispatched,
    IReadOnlyList<ConnectorAllocation> Allocations,
    DateTime Timestamp
);

public record ConnectorAllocation(
    Guid ConnectorId,
    Guid StationId,
    string StationCode,
    int ConnectorNumber,
    decimal AllocatedPowerKw,
    decimal MaxPowerKw
);
