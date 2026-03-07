using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KLC.Driver;

/// <summary>
/// SignalR Hub for real-time updates to the driver mobile app.
/// Provides live charging session updates, notifications, and station status.
/// </summary>
[Authorize]
public class DriverHub : Hub<IDriverHubClient>
{
    private readonly ILogger<DriverHub> _logger;

    public DriverHub(ILogger<DriverHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            // Add user to their personal group for notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User:{userId}");
            _logger.LogInformation("User {UserId} connected to DriverHub", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User:{userId}");
            _logger.LogInformation("User {UserId} disconnected from DriverHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to updates for a specific charging session.
    /// </summary>
    public async Task SubscribeToSession(Guid sessionId)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} subscribing to session {SessionId}", userId, sessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Session:{sessionId}");
    }

    /// <summary>
    /// Unsubscribe from session updates.
    /// </summary>
    public async Task UnsubscribeFromSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session:{sessionId}");
    }

    /// <summary>
    /// Subscribe to updates for a specific station (connector availability).
    /// </summary>
    public async Task SubscribeToStation(Guid stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Station:{stationId}");
    }

    /// <summary>
    /// Unsubscribe from station updates.
    /// </summary>
    public async Task UnsubscribeFromStation(Guid stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Station:{stationId}");
    }

    private Guid GetUserId()
    {
        var sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

/// <summary>
/// Interface for sending updates to connected driver app clients.
/// </summary>
public interface IDriverHubClient
{
    /// <summary>
    /// Real-time session update (energy, cost, duration).
    /// Called every 10-30 seconds during active charging.
    /// </summary>
    Task OnSessionUpdate(SessionUpdateMessage update);

    /// <summary>
    /// Session status changed (started, stopped, completed).
    /// </summary>
    Task OnSessionStatusChanged(SessionStatusMessage status);

    /// <summary>
    /// New notification received.
    /// </summary>
    Task OnNotification(NotificationMessage notification);

    /// <summary>
    /// Station connector status changed.
    /// </summary>
    Task OnConnectorStatusChanged(ConnectorStatusMessage status);

    /// <summary>
    /// Payment status update.
    /// </summary>
    Task OnPaymentStatusChanged(PaymentStatusMessage status);

    /// <summary>
    /// Session completed with summary.
    /// </summary>
    Task OnSessionCompleted(SessionCompletedMessage completed);

    /// <summary>
    /// Charging error occurred.
    /// </summary>
    Task OnChargingError(ChargingErrorMessage error);

    /// <summary>
    /// Station status changed (available/unavailable/offline).
    /// </summary>
    Task OnStationStatusChanged(StationStatusChangedMessage status);

    /// <summary>
    /// Wallet balance changed (top-up, deduction, refund).
    /// </summary>
    Task OnWalletBalanceChanged(WalletBalanceChangedMessage balance);
}

// SignalR message types
public record SessionUpdateMessage
{
    public Guid SessionId { get; init; }
    public decimal EnergyKwh { get; init; }
    public decimal CurrentCost { get; init; }
    public int DurationMinutes { get; init; }
    public decimal? PowerKw { get; init; }
    public decimal? SocPercent { get; init; }
    public DateTime Timestamp { get; init; }
}

public record SessionStatusMessage
{
    public Guid SessionId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Message { get; init; }
    public DateTime Timestamp { get; init; }
}

public record NotificationMessage
{
    public Guid NotificationId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public DateTime Timestamp { get; init; }
}

public record ConnectorStatusMessage
{
    public Guid StationId { get; init; }
    public int ConnectorNumber { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record PaymentStatusMessage
{
    public Guid PaymentId { get; init; }
    public Guid SessionId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Error { get; init; }
    public DateTime Timestamp { get; init; }
}

public record SessionCompletedMessage
{
    public Guid SessionId { get; init; }
    public decimal TotalEnergyKwh { get; init; }
    public decimal TotalCost { get; init; }
    public int DurationMinutes { get; init; }
    public DateTime CompletedAt { get; init; }
}

public record ChargingErrorMessage
{
    public Guid SessionId { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record StationStatusChangedMessage
{
    public Guid StationId { get; init; }
    public string StationName { get; init; } = string.Empty;
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record WalletBalanceChangedMessage
{
    public Guid UserId { get; init; }
    public decimal NewBalance { get; init; }
    public decimal ChangeAmount { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
