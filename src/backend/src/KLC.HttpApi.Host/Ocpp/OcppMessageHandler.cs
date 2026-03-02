using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Ocpp.Messages;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp;

/// <summary>
/// Handles OCPP 1.6J messages from Charge Points.
/// </summary>
public class OcppMessageHandler
{
    private readonly ILogger<OcppMessageHandler> _logger;
    private readonly OcppConnectionManager _connectionManager;
    private readonly IOcppService _ocppService;

    public OcppMessageHandler(
        ILogger<OcppMessageHandler> logger,
        OcppConnectionManager connectionManager,
        IOcppService ocppService)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _ocppService = ocppService;
    }

    /// <summary>
    /// Process an incoming OCPP message.
    /// </summary>
    public async Task<string?> HandleMessageAsync(OcppConnection connection, string message)
    {
        try
        {
            _logger.LogDebug("Received from {ChargePointId}: {Message}", connection.ChargePointId, message);

            // Parse OCPP JSON array format: [MessageType, UniqueId, ...]
            var jsonArray = JsonSerializer.Deserialize<JsonElement[]>(message);
            if (jsonArray == null || jsonArray.Length < 3)
            {
                _logger.LogWarning("Invalid OCPP message format from {ChargePointId}", connection.ChargePointId);
                return null;
            }

            var messageType = jsonArray[0].GetInt32();
            var uniqueId = jsonArray[1].GetString() ?? string.Empty;

            return messageType switch
            {
                OcppMessageType.Call => await HandleCallAsync(connection, uniqueId, jsonArray),
                OcppMessageType.CallResult => HandleCallResult(connection, uniqueId, jsonArray),
                OcppMessageType.CallError => HandleCallError(connection, uniqueId, jsonArray),
                _ => CreateErrorResponse(uniqueId, OcppErrorCode.ProtocolError, "Unknown message type")
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from {ChargePointId}", connection.ChargePointId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {ChargePointId}", connection.ChargePointId);
            return CreateErrorResponse("", OcppErrorCode.InternalError, ex.Message);
        }
    }

    private async Task<string> HandleCallAsync(OcppConnection connection, string uniqueId, JsonElement[] jsonArray)
    {
        var action = jsonArray[2].GetString() ?? string.Empty;
        var payload = jsonArray.Length > 3 ? jsonArray[3] : default;

        _logger.LogInformation("Handling {Action} from {ChargePointId}", action, connection.ChargePointId);

        return action switch
        {
            "BootNotification" => await HandleBootNotificationAsync(connection, uniqueId, payload),
            "Heartbeat" => await HandleHeartbeatAsync(connection, uniqueId),
            "StatusNotification" => await HandleStatusNotificationAsync(connection, uniqueId, payload),
            "StartTransaction" => await HandleStartTransactionAsync(connection, uniqueId, payload),
            "StopTransaction" => await HandleStopTransactionAsync(connection, uniqueId, payload),
            "MeterValues" => await HandleMeterValuesAsync(connection, uniqueId, payload),
            "Authorize" => HandleAuthorize(uniqueId, payload),
            _ => CreateErrorResponse(uniqueId, OcppErrorCode.NotImplemented, $"Action {action} not implemented")
        };
    }

    private async Task<string> HandleBootNotificationAsync(OcppConnection connection, string uniqueId, JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<BootNotificationRequest>(payload.GetRawText());
        if (request == null)
            return CreateErrorResponse(uniqueId, OcppErrorCode.FormationViolation, "Invalid BootNotification payload");

        _logger.LogInformation("BootNotification from {ChargePointId}: Vendor={Vendor}, Model={Model}, FW={FirmwareVersion}",
            connection.ChargePointId, request.ChargePointVendor, request.ChargePointModel, request.FirmwareVersion);

        // Persist to database
        var stationId = await _ocppService.HandleBootNotificationAsync(
            connection.ChargePointId,
            request.ChargePointVendor ?? string.Empty,
            request.ChargePointModel ?? string.Empty,
            request.ChargePointSerialNumber,
            request.FirmwareVersion);

        connection.RecordHeartbeat();

        // Reject unknown stations (BR-006-02)
        var status = stationId.HasValue ? RegistrationStatus.Accepted : RegistrationStatus.Rejected;

        var response = new BootNotificationResponse
        {
            Status = status,
            CurrentTime = DateTime.UtcNow.ToString("o"),
            Interval = 300 // 5 minutes heartbeat interval
        };

        return CreateCallResult(uniqueId, response);
    }

    private async Task<string> HandleHeartbeatAsync(OcppConnection connection, string uniqueId)
    {
        connection.RecordHeartbeat();
        _logger.LogDebug("Heartbeat from {ChargePointId}", connection.ChargePointId);

        // Persist to database
        await _ocppService.HandleHeartbeatAsync(connection.ChargePointId);

        var response = new HeartbeatResponse
        {
            CurrentTime = DateTime.UtcNow.ToString("o")
        };

        return CreateCallResult(uniqueId, response);
    }

    private async Task<string> HandleStatusNotificationAsync(OcppConnection connection, string uniqueId, JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<StatusNotificationRequest>(payload.GetRawText());
        if (request == null)
            return CreateErrorResponse(uniqueId, OcppErrorCode.FormationViolation, "Invalid StatusNotification payload");

        _logger.LogInformation("StatusNotification from {ChargePointId}: Connector={ConnectorId}, Status={Status}, Error={ErrorCode}",
            connection.ChargePointId, request.ConnectorId, request.Status, request.ErrorCode);

        // Map OCPP status to domain status
        var connectorStatus = MapOcppStatusToConnectorStatus(request.Status);

        // Persist to database
        await _ocppService.HandleStatusNotificationAsync(
            connection.ChargePointId,
            request.ConnectorId,
            connectorStatus,
            request.ErrorCode);

        return CreateCallResult(uniqueId, new StatusNotificationResponse());
    }

    private async Task<string> HandleStartTransactionAsync(OcppConnection connection, string uniqueId, JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<StartTransactionRequest>(payload.GetRawText());
        if (request == null)
            return CreateErrorResponse(uniqueId, OcppErrorCode.FormationViolation, "Invalid StartTransaction payload");

        _logger.LogInformation("StartTransaction from {ChargePointId}: Connector={ConnectorId}, IdTag={IdTag}, MeterStart={MeterStart}",
            connection.ChargePointId, request.ConnectorId, request.IdTag, request.MeterStart);

        // Generate transaction ID
        var transactionId = Math.Abs(Guid.NewGuid().GetHashCode());

        // Persist to database (BR-006-04)
        var sessionId = await _ocppService.HandleStartTransactionAsync(
            connection.ChargePointId,
            request.ConnectorId,
            request.IdTag ?? string.Empty,
            request.MeterStart,
            transactionId);

        var response = new StartTransactionResponse
        {
            TransactionId = transactionId,
            IdTagInfo = new IdTagInfo
            {
                Status = sessionId.HasValue ? AuthorizationStatus.Accepted : AuthorizationStatus.Invalid
            }
        };

        return CreateCallResult(uniqueId, response);
    }

    private async Task<string> HandleStopTransactionAsync(OcppConnection connection, string uniqueId, JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<StopTransactionRequest>(payload.GetRawText());
        if (request == null)
            return CreateErrorResponse(uniqueId, OcppErrorCode.FormationViolation, "Invalid StopTransaction payload");

        _logger.LogInformation("StopTransaction from {ChargePointId}: TransactionId={TransactionId}, MeterStop={MeterStop}, Reason={Reason}",
            connection.ChargePointId, request.TransactionId, request.MeterStop, request.Reason);

        // Persist to database (BR-006-04)
        await _ocppService.HandleStopTransactionAsync(
            request.TransactionId,
            request.MeterStop,
            request.Reason);

        var response = new StopTransactionResponse
        {
            IdTagInfo = new IdTagInfo
            {
                Status = AuthorizationStatus.Accepted
            }
        };

        return CreateCallResult(uniqueId, response);
    }

    private async Task<string> HandleMeterValuesAsync(OcppConnection connection, string uniqueId, JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<MeterValuesRequest>(payload.GetRawText());
        if (request == null)
            return CreateErrorResponse(uniqueId, OcppErrorCode.FormationViolation, "Invalid MeterValues payload");

        _logger.LogDebug("MeterValues from {ChargePointId}: Connector={ConnectorId}, TransactionId={TransactionId}, Values={Count}",
            connection.ChargePointId, request.ConnectorId, request.TransactionId, request.MeterValue?.Length ?? 0);

        // Process meter values (BR-006-05)
        if (request.MeterValue != null)
        {
            foreach (var mv in request.MeterValue)
            {
                decimal energyWh = 0;
                decimal? currentAmps = null;
                decimal? voltage = null;
                decimal? power = null;
                decimal? soc = null;

                if (mv.SampledValue != null)
                {
                    foreach (var sv in mv.SampledValue)
                    {
                        if (decimal.TryParse(sv.Value, out var value))
                        {
                            switch (sv.Measurand)
                            {
                                case "Energy.Active.Import.Register":
                                    energyWh = value;
                                    break;
                                case "Current.Import":
                                    currentAmps = value;
                                    break;
                                case "Voltage":
                                    voltage = value;
                                    break;
                                case "Power.Active.Import":
                                    power = value;
                                    break;
                                case "SoC":
                                    soc = value;
                                    break;
                            }
                        }
                    }
                }

                await _ocppService.HandleMeterValuesAsync(
                    connection.ChargePointId,
                    request.ConnectorId,
                    request.TransactionId,
                    energyWh,
                    currentAmps,
                    voltage,
                    power,
                    soc);
            }
        }

        return CreateCallResult(uniqueId, new MeterValuesResponse());
    }

    private string HandleAuthorize(string uniqueId, JsonElement payload)
    {
        // TODO: Implement authorization logic
        var response = new
        {
            idTagInfo = new IdTagInfo
            {
                Status = AuthorizationStatus.Accepted
            }
        };

        return CreateCallResult(uniqueId, response);
    }

    private string? HandleCallResult(OcppConnection connection, string uniqueId, JsonElement[] jsonArray)
    {
        var payload = jsonArray.Length > 2 ? jsonArray[2].GetRawText() : "{}";

        if (connection.TryCompletePendingRequest(uniqueId, payload))
        {
            _logger.LogDebug("Received CallResult for {UniqueId} from {ChargePointId}",
                uniqueId, connection.ChargePointId);
        }
        else
        {
            _logger.LogWarning("Received unexpected CallResult for {UniqueId} from {ChargePointId}",
                uniqueId, connection.ChargePointId);
        }

        return null; // No response needed for CallResult
    }

    private string? HandleCallError(OcppConnection connection, string uniqueId, JsonElement[] jsonArray)
    {
        var errorCode = jsonArray.Length > 2 ? jsonArray[2].GetString() : "Unknown";
        var errorDescription = jsonArray.Length > 3 ? jsonArray[3].GetString() : "";

        _logger.LogWarning("Received CallError from {ChargePointId}: {ErrorCode} - {ErrorDescription}",
            connection.ChargePointId, errorCode, errorDescription);

        connection.TryCompletePendingRequest(uniqueId, $"ERROR:{errorCode}:{errorDescription}");

        return null; // No response needed for CallError
    }

    private static ConnectorStatus MapOcppStatusToConnectorStatus(string? ocppStatus)
    {
        return ocppStatus?.ToLower() switch
        {
            "available" => ConnectorStatus.Available,
            "preparing" => ConnectorStatus.Preparing,
            "charging" => ConnectorStatus.Charging,
            "suspendedevse" => ConnectorStatus.SuspendedEVSE,
            "suspendedev" => ConnectorStatus.SuspendedEV,
            "finishing" => ConnectorStatus.Finishing,
            "reserved" => ConnectorStatus.Reserved,
            "unavailable" => ConnectorStatus.Unavailable,
            "faulted" => ConnectorStatus.Faulted,
            _ => ConnectorStatus.Available
        };
    }

    private static string CreateCallResult(string uniqueId, object payload)
    {
        var response = new object[] { OcppMessageType.CallResult, uniqueId, payload };
        return JsonSerializer.Serialize(response);
    }

    private static string CreateErrorResponse(string uniqueId, string errorCode, string errorDescription)
    {
        var response = new object[] { OcppMessageType.CallError, uniqueId, errorCode, errorDescription, new { } };
        return JsonSerializer.Serialize(response);
    }
}
