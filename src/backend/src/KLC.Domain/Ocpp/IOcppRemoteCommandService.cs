using System.Collections.Generic;
using System.Threading.Tasks;

namespace KLC.Ocpp;

/// <summary>
/// Result from a remote OCPP command.
/// </summary>
public record RemoteCommandResult(bool Accepted, string? ErrorMessage = null);

/// <summary>
/// Result from GetConfiguration command.
/// </summary>
public record ConfigurationResult(
    bool Accepted,
    List<ConfigurationEntry>? ConfigurationKey = null,
    List<string>? UnknownKey = null);

public record ConfigurationEntry(string Key, string? Value, bool Readonly);

/// <summary>
/// Service for sending remote OCPP commands to Charge Points.
/// Implemented in the Host layer where WebSocket connections are managed.
/// </summary>
public interface IOcppRemoteCommandService
{
    /// <summary>
    /// Send RemoteStartTransaction to a Charge Point.
    /// </summary>
    Task<bool> SendRemoteStartTransactionAsync(string stationCode, int connectorId, string idTag);

    /// <summary>
    /// Send RemoteStopTransaction to a Charge Point.
    /// </summary>
    Task<bool> SendRemoteStopTransactionAsync(string stationCode, int transactionId);

    /// <summary>
    /// Send Reset (Soft or Hard) to a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendResetAsync(string stationCode, string resetType);

    /// <summary>
    /// Send UnlockConnector to a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendUnlockConnectorAsync(string stationCode, int connectorId);

    /// <summary>
    /// Send ChangeAvailability (Operative/Inoperative) to a Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendChangeAvailabilityAsync(string stationCode, int connectorId, string availabilityType);

    /// <summary>
    /// Send GetConfiguration to read charger settings.
    /// </summary>
    Task<ConfigurationResult> SendGetConfigurationAsync(string stationCode, List<string>? keys = null);

    /// <summary>
    /// Send ChangeConfiguration to update a charger setting.
    /// </summary>
    Task<RemoteCommandResult> SendChangeConfigurationAsync(string stationCode, string key, string value);

    /// <summary>
    /// Send TriggerMessage to request a specific message from the Charge Point.
    /// </summary>
    Task<RemoteCommandResult> SendTriggerMessageAsync(string stationCode, string requestedMessage, int? connectorId = null);
}
