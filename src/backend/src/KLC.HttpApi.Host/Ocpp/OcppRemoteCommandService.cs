using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp;

/// <summary>
/// Host-layer implementation of IOcppRemoteCommandService.
/// Bridges the Application layer to the OcppConnectionManager singleton.
/// </summary>
public class OcppRemoteCommandService : IOcppRemoteCommandService
{
    private readonly OcppConnectionManager _connectionManager;
    private readonly ILogger<OcppRemoteCommandService> _logger;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public OcppRemoteCommandService(
        OcppConnectionManager connectionManager,
        ILogger<OcppRemoteCommandService> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task<bool> SendRemoteStartTransactionAsync(string stationCode, int connectorId, string idTag)
    {
        var connection = _connectionManager.GetConnection(stationCode);
        if (connection == null)
        {
            _logger.LogWarning("Station {StationCode} not connected via OCPP", stationCode);
            return false;
        }

        var response = await connection.SendCallAsync("RemoteStartTransaction", new
        {
            connectorId,
            idTag
        }, Timeout);

        if (response == null)
        {
            _logger.LogWarning("RemoteStartTransaction timeout for station {StationCode}", stationCode);
            return false;
        }

        return true;
    }

    public async Task<bool> SendRemoteStopTransactionAsync(string stationCode, int transactionId)
    {
        var connection = _connectionManager.GetConnection(stationCode);
        if (connection == null)
        {
            _logger.LogWarning("Station {StationCode} not connected via OCPP for stop", stationCode);
            return false;
        }

        var response = await connection.SendCallAsync("RemoteStopTransaction", new
        {
            transactionId
        }, Timeout);

        if (response == null)
        {
            _logger.LogWarning("RemoteStopTransaction timeout for station {StationCode}", stationCode);
            return false;
        }

        return true;
    }
}
