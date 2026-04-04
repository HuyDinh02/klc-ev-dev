using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp;

/// <summary>
/// Host-layer implementation of IOcppRemoteCommandService.
/// First tries the local OcppConnectionManager. If station not found locally,
/// forwards to the OCPP Gateway via internal API (for separated architecture).
/// </summary>
public class OcppRemoteCommandService : IOcppRemoteCommandService
{
    private readonly OcppConnectionManager _connectionManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OcppRemoteCommandService> _logger;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public OcppRemoteCommandService(
        OcppConnectionManager connectionManager,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OcppRemoteCommandService> logger)
    {
        _connectionManager = connectionManager;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RemoteCommandResult> SendRemoteStartTransactionAsync(string stationCode, int connectorId, string idTag)
    {
        return await SendCommandAsync(stationCode, "RemoteStartTransaction", new
        {
            connectorId,
            idTag
        });
    }

    public async Task<RemoteCommandResult> SendRemoteStopTransactionAsync(string stationCode, int transactionId)
    {
        return await SendCommandAsync(stationCode, "RemoteStopTransaction", new
        {
            transactionId
        });
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

    public async Task<RemoteCommandResult> SendSetChargingProfileAsync(string stationCode, int connectorId, ChargingProfilePayload profile)
    {
        var payload = new
        {
            connectorId,
            csChargingProfiles = new
            {
                chargingProfileId = profile.ChargingProfileId,
                transactionId = profile.TransactionId,
                stackLevel = profile.StackLevel,
                chargingProfilePurpose = profile.ChargingProfilePurpose,
                chargingProfileKind = profile.ChargingProfileKind,
                chargingSchedule = new
                {
                    chargingRateUnit = profile.ChargingSchedule.ChargingRateUnit,
                    chargingSchedulePeriod = profile.ChargingSchedule.ChargingSchedulePeriod
                        .Select(p => new
                        {
                            startPeriod = p.StartPeriod,
                            limit = p.Limit,
                            numberPhases = p.NumberPhases
                        }).ToArray()
                }
            }
        };

        return await SendCommandAsync(stationCode, "SetChargingProfile", payload);
    }

    public async Task<RemoteCommandResult> SendUpdateFirmwareAsync(string stationCode, string location, DateTime retrieveDate, int? retries = null, int? retryInterval = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["location"] = location,
            ["retrieveDate"] = retrieveDate.ToString("o")
        };

        if (retries.HasValue)
            payload["retries"] = retries.Value;
        if (retryInterval.HasValue)
            payload["retryInterval"] = retryInterval.Value;

        return await SendCommandAsync(stationCode, "UpdateFirmware", payload);
    }

    public async Task<RemoteCommandResult> SendGetDiagnosticsAsync(string stationCode, string location, DateTime? startTime = null, DateTime? stopTime = null, int? retries = null, int? retryInterval = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["location"] = location
        };

        if (startTime.HasValue)
            payload["startTime"] = startTime.Value.ToString("o");
        if (stopTime.HasValue)
            payload["stopTime"] = stopTime.Value.ToString("o");
        if (retries.HasValue)
            payload["retries"] = retries.Value;
        if (retryInterval.HasValue)
            payload["retryInterval"] = retryInterval.Value;

        return await SendCommandAsync(stationCode, "GetDiagnostics", payload);
    }

    public async Task<RemoteCommandResult> SendClearChargingProfileAsync(string stationCode, int? id = null, int? connectorId = null, string? chargingProfilePurpose = null, int? stackLevel = null)
    {
        var payload = new Dictionary<string, object>();

        if (id.HasValue)
            payload["id"] = id.Value;
        if (connectorId.HasValue)
            payload["connectorId"] = connectorId.Value;
        if (!string.IsNullOrWhiteSpace(chargingProfilePurpose))
            payload["chargingProfilePurpose"] = chargingProfilePurpose;
        if (stackLevel.HasValue)
            payload["stackLevel"] = stackLevel.Value;

        return await SendCommandAsync(stationCode, "ClearChargingProfile", payload);
    }

    public async Task<LocalListVersionResult> SendGetLocalListVersionAsync(string stationCode)
    {
        var connection = _connectionManager.GetConnection(stationCode);
        if (connection == null)
        {
            _logger.LogWarning("Station {StationCode} not connected for GetLocalListVersion", stationCode);
            return new LocalListVersionResult(false);
        }

        var response = await connection.SendCallAsync("GetLocalListVersion", new { }, Timeout);
        if (response == null || response.StartsWith("ERROR:"))
        {
            _logger.LogWarning("GetLocalListVersion timeout/error for {StationCode}", stationCode);
            return new LocalListVersionResult(false);
        }

        try
        {
            using var doc = JsonDocument.Parse(response);
            var listVersion = doc.RootElement.TryGetProperty("listVersion", out var v) ? v.GetInt32() : -1;
            return new LocalListVersionResult(true, listVersion);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse GetLocalListVersion response from {StationCode}", stationCode);
            return new LocalListVersionResult(false);
        }
    }

    public async Task<SendLocalListResult> SendSendLocalListAsync(string stationCode, int listVersion, string updateType, List<LocalAuthEntry>? localAuthorizationList = null)
    {
        var connection = _connectionManager.GetConnection(stationCode);
        if (connection == null)
        {
            _logger.LogWarning("Station {StationCode} not connected for SendLocalList", stationCode);
            return new SendLocalListResult(false, ErrorMessage: "Station not connected");
        }

        var payload = new Dictionary<string, object>
        {
            ["listVersion"] = listVersion,
            ["updateType"] = updateType
        };

        if (localAuthorizationList != null && localAuthorizationList.Count > 0)
        {
            payload["localAuthorizationList"] = localAuthorizationList.Select(entry =>
            {
                var item = new Dictionary<string, object> { ["idTag"] = entry.IdTag };
                if (entry.IdTagInfo != null)
                {
                    var info = new Dictionary<string, object> { ["status"] = entry.IdTagInfo.Status };
                    if (entry.IdTagInfo.ExpiryDate != null)
                        info["expiryDate"] = entry.IdTagInfo.ExpiryDate;
                    if (entry.IdTagInfo.ParentIdTag != null)
                        info["parentIdTag"] = entry.IdTagInfo.ParentIdTag;
                    item["idTagInfo"] = info;
                }
                return item;
            }).ToArray();
        }

        var response = await connection.SendCallAsync("SendLocalList", payload, Timeout);

        if (response == null)
        {
            _logger.LogWarning("SendLocalList timeout for station {StationCode}", stationCode);
            return new SendLocalListResult(false, ErrorMessage: "Command timed out");
        }

        if (response.StartsWith("ERROR:"))
        {
            _logger.LogWarning("SendLocalList error from {StationCode}: {Response}", stationCode, response);
            return new SendLocalListResult(false, ErrorMessage: response);
        }

        try
        {
            using var doc = JsonDocument.Parse(response);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
            var accepted = string.Equals(status, "Accepted", StringComparison.OrdinalIgnoreCase);

            if (!accepted)
            {
                _logger.LogWarning("SendLocalList rejected by {StationCode}: status={Status}", stationCode, status);
            }

            return new SendLocalListResult(accepted, status);
        }
        catch (JsonException)
        {
            return new SendLocalListResult(true, "Unknown");
        }
    }

    public async Task<RemoteCommandResult> SendReserveNowAsync(string stationCode, int connectorId, DateTime expiryDate, string idTag, int reservationId)
    {
        return await SendCommandAsync(stationCode, "ReserveNow", new
        {
            connectorId,
            expiryDate = expiryDate.ToString("o"),
            idTag,
            reservationId
        });
    }

    public async Task<RemoteCommandResult> SendCancelReservationAsync(string stationCode, int reservationId)
    {
        return await SendCommandAsync(stationCode, "CancelReservation", new { reservationId });
    }

    private async Task<RemoteCommandResult> SendCommandAsync(string stationCode, string action, object payload)
    {
        var connection = _connectionManager.GetConnection(stationCode);
        if (connection == null)
        {
            // Fallback: forward to OCPP Gateway if configured (separated architecture)
            var gatewayUrl = _configuration["Ocpp:GatewayUrl"];
            if (!string.IsNullOrEmpty(gatewayUrl))
            {
                _logger.LogInformation("Station {StationCode} not local — forwarding {Action} to Gateway", stationCode, action);
                return await ForwardToGatewayAsync(gatewayUrl, stationCode, action, payload);
            }

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

    /// <summary>
    /// Forward a remote command to the OCPP Gateway via its internal API.
    /// Used when the charger is connected to a separate Gateway service.
    /// </summary>
    private async Task<RemoteCommandResult> ForwardToGatewayAsync(
        string gatewayUrl, string stationCode, string action, object payload)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var internalKey = _configuration["Internal:ApiKey"];
            if (!string.IsNullOrEmpty(internalKey))
            {
                client.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);
            }

            // Map OCPP action to internal API endpoint
            var endpoint = action switch
            {
                "RemoteStartTransaction" => "remote-start",
                "RemoteStopTransaction" => "remote-stop",
                _ => null
            };

            if (endpoint == null)
            {
                // For non-mapped commands, try the Gateway's public OCPP management API
                // These require the same auth — not supported via proxy yet
                _logger.LogWarning("Command {Action} not supported via Gateway forwarding", action);
                return new RemoteCommandResult(false, $"Command {action} not supported via Gateway proxy");
            }

            var response = await client.PostAsJsonAsync(
                $"{gatewayUrl}/api/internal/ocpp/{endpoint}",
                payload);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var success = doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean();
                    var message = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;
                    return new RemoteCommandResult(success, message);
                }
                catch
                {
                    return new RemoteCommandResult(true);
                }
            }

            _logger.LogWarning("Gateway forwarding failed for {Action}: {Status}", action, response.StatusCode);
            return new RemoteCommandResult(false, $"Gateway returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to forward {Action} to Gateway for {StationCode}", action, stationCode);
            return new RemoteCommandResult(false, "Gateway communication error");
        }
    }
}
