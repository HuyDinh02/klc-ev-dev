using System.Threading.Tasks;

namespace KLC.Ocpp;

/// <summary>
/// Service for sending remote OCPP commands to Charge Points.
/// Implemented in the Host layer where WebSocket connections are managed.
/// </summary>
public interface IOcppRemoteCommandService
{
    /// <summary>
    /// Send RemoteStartTransaction to a Charge Point.
    /// Returns true if the command was accepted.
    /// </summary>
    Task<bool> SendRemoteStartTransactionAsync(string stationCode, int connectorId, string idTag);

    /// <summary>
    /// Send RemoteStopTransaction to a Charge Point.
    /// Returns true if the command was accepted.
    /// </summary>
    Task<bool> SendRemoteStopTransactionAsync(string stationCode, int transactionId);
}
