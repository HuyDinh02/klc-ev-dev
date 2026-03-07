using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        var result = await SendCommandAsync(stationCode, "RemoteStartTransaction", new
        {
            connectorId,
            idTag
        });
        return result.Accepted;
    }

    public async Task<bool> SendRemoteStopTransactionAsync(string stationCode, int transactionId)
    {
        var result = await SendCommandAsync(stationCode, "RemoteStopTransaction", new
        {
            transactionId
        });
        return result.Accepted;
    }

    public async Task<RemoteCommandResult> SendResetAsync(string stationCode, string resetType)
    {
        return await SendCommandAsync(stationCode, "Reset", new { type = resetType });
    }

    public async Task<RemoteCommandResult> SendUnlockConnectorAsync(string stationCode, int connectorId)
    {
        return await SendCommandAsync(stationCode, "UnlockConnector", new { connectorId });
    }

    public async Task<RemoteCommandResult> SendChangeAvailabilityAsync(string stationCode, int connectorId, string availabilityType)
    {
        return await SendCommandAsync(stationCode, "ChangeAvailability", new
        {
            connectorId,
            type = availabilityType
        });
    }

    public async Task<ConfigurationResult> SendGetConfigurationAsync(string stationCode, List<string>? keys = null)
    {
        var connection = _connectionManager.GetConnection(stationCode);
        if (connection == null)
        {
            _logger.LogWarning("Station {StationCode} not connected for GetConfiguration", stationCode);
            return new ConfigurationResult(false);
        }

        var payload = keys != null && keys.Count > 0
            ? (object)new { key = keys }
            : new { };

        var response = await connection.SendCallAsync("GetConfiguration", payload, Timeout);
        if (response == null || response.StartsWith("ERROR:"))
        {
            _logger.LogWarning("GetConfiguration timeout/error for {StationCode}", stationCode);
            return new ConfigurationResult(false);
        }

        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var configKeys = new List<ConfigurationEntry>();
            if (root.TryGetProperty("configurationKey", out var keysArray))
            {
                foreach (var entry in keysArray.EnumerateArray())
                {
                    var key = entry.GetProperty("key").GetString() ?? "";
                    var value = entry.TryGetProperty("value", out var v) ? v.GetString() : null;
                    var readOnly = entry.TryGetProperty("readonly", out var ro) && ro.GetBoolean();
                    configKeys.Add(new ConfigurationEntry(key, value, readOnly));
                }
            }

            var unknownKeys = new List<string>();
            if (root.TryGetProperty("unknownKey", out var unknownArray))
            {
                unknownKeys = unknownArray.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .ToList();
            }

            return new ConfigurationResult(true, configKeys, unknownKeys);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse GetConfiguration response from {StationCode}", stationCode);
            return new ConfigurationResult(false);
        }
    }

    public async Task<RemoteCommandResult> SendChangeConfigurationAsync(string stationCode, string key, string value)
    {
        return await SendCommandAsync(stationCode, "ChangeConfiguration", new { key, value });
    }

    public async Task<RemoteCommandResult> SendTriggerMessageAsync(string stationCode, string requestedMessage, int? connectorId = null)
    {
        object payload = connectorId.HasValue
            ? new { requestedMessage, connectorId = connectorId.Value }
            : new { requestedMessage };

        return await SendCommandAsync(stationCode, "TriggerMessage", payload);
    }

    private async Task<RemoteCommandResult> SendCommandAsync(string stationCode, string action, object payload)
    {
        var connection = _connectionManager.GetConnection(stationCode);
        if (connection == null)
        {
            _logger.LogWarning("Station {StationCode} not connected for {Action}", stationCode, action);
            return new RemoteCommandResult(false, "Station not connected");
        }

        var response = await connection.SendCallAsync(action, payload, Timeout);

        if (response == null)
        {
            _logger.LogWarning("{Action} timeout for station {StationCode}", action, stationCode);
            return new RemoteCommandResult(false, "Command timed out");
        }

        if (response.StartsWith("ERROR:"))
        {
            _logger.LogWarning("{Action} error from {StationCode}: {Response}", action, stationCode, response);
            return new RemoteCommandResult(false, response);
        }

        // Parse status from response JSON
        try
        {
            using var doc = JsonDocument.Parse(response);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
            var accepted = string.Equals(status, "Accepted", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(status, "Unlocked", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(status, "Scheduled", StringComparison.OrdinalIgnoreCase);

            if (!accepted)
            {
                _logger.LogWarning("{Action} rejected by {StationCode}: status={Status}", action, stationCode, status);
                return new RemoteCommandResult(false, $"Charger responded: {status}");
            }

            return new RemoteCommandResult(true);
        }
        catch (JsonException)
        {
            // If we can't parse, consider it accepted (we got a response)
            return new RemoteCommandResult(true);
        }
    }
}
