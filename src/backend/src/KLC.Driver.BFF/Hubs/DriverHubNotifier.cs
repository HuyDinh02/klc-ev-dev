using Microsoft.AspNetCore.SignalR;

namespace KLC.Driver;

/// <summary>
/// Service for sending real-time updates to connected driver app clients.
/// </summary>
public interface IDriverHubNotifier
{
    Task NotifySessionCompletedAsync(Guid userId, SessionCompletedMessage message);
    Task NotifyChargingErrorAsync(Guid userId, Guid sessionId, ChargingErrorMessage message);
    Task NotifyStationStatusChangedAsync(Guid stationId, StationStatusChangedMessage message);
    Task NotifyWalletBalanceChangedAsync(Guid userId, WalletBalanceChangedMessage message);
    Task NotifySessionUpdateAsync(Guid sessionId, SessionUpdateMessage message);
    Task NotifyNotificationAsync(Guid userId, NotificationMessage message);
}

public class DriverHubNotifier : IDriverHubNotifier
{
    private readonly IHubContext<DriverHub, IDriverHubClient> _hubContext;
    private readonly ILogger<DriverHubNotifier> _logger;

    public DriverHubNotifier(
        IHubContext<DriverHub, IDriverHubClient> hubContext,
        ILogger<DriverHubNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifySessionCompletedAsync(Guid userId, SessionCompletedMessage message)
    {
        await _hubContext.Clients.Group($"User:{userId}").OnSessionCompleted(message);
        await _hubContext.Clients.Group($"Session:{message.SessionId}").OnSessionCompleted(message);
    }

    public async Task NotifyChargingErrorAsync(Guid userId, Guid sessionId, ChargingErrorMessage message)
    {
        await _hubContext.Clients.Group($"User:{userId}").OnChargingError(message);
        await _hubContext.Clients.Group($"Session:{sessionId}").OnChargingError(message);
    }

    public async Task NotifyStationStatusChangedAsync(Guid stationId, StationStatusChangedMessage message)
    {
        await _hubContext.Clients.Group($"Station:{stationId}").OnStationStatusChanged(message);
    }

    public async Task NotifyWalletBalanceChangedAsync(Guid userId, WalletBalanceChangedMessage message)
    {
        await _hubContext.Clients.Group($"User:{userId}").OnWalletBalanceChanged(message);
    }

    public async Task NotifySessionUpdateAsync(Guid sessionId, SessionUpdateMessage message)
    {
        await _hubContext.Clients.Group($"Session:{sessionId}").OnSessionUpdate(message);
    }

    public async Task NotifyNotificationAsync(Guid userId, NotificationMessage message)
    {
        await _hubContext.Clients.Group($"User:{userId}").OnNotification(message);
    }
}
