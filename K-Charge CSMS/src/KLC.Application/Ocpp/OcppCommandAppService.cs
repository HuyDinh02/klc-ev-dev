using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Volo.Abp.Application.Services;
using KLC.Ocpp;

namespace KLC.Application.Ocpp;

public class OcppCommandAppService : ApplicationService, IOcppCommandService
{
    private readonly IOcppMessageDispatcher _messageDispatcher;
    private readonly ILogger<OcppCommandAppService> _logger;

    public OcppCommandAppService(
        IOcppMessageDispatcher messageDispatcher,
        ILogger<OcppCommandAppService> logger)
    {
        _messageDispatcher = messageDispatcher;
        _logger = logger;
    }

    public async Task<OcppCommandResultDto> RemoteStartAsync(RemoteStartRequestDto request)
    {
        try
        {
            var payload = new JObject
            {
                { "idTag", request.IdTag },
                { "connectorId", request.ConnectorId }
            };

            if (!string.IsNullOrEmpty(request.ChargingProfile))
            {
                payload["chargingProfile"] = JObject.Parse(request.ChargingProfile);
            }

            var response = await _messageDispatcher.SendCommandAsync(
                request.ChargePointId,
                "RemoteStartTransaction",
                payload);

            return ParseResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending RemoteStart command to {ChargePointId}", request.ChargePointId);
            return new OcppCommandResultDto
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    public async Task<OcppCommandResultDto> RemoteStopAsync(RemoteStopRequestDto request)
    {
        try
        {
            var payload = new JObject
            {
                { "transactionId", request.TransactionId }
            };

            var response = await _messageDispatcher.SendCommandAsync(
                request.ChargePointId,
                "RemoteStopTransaction",
                payload);

            return ParseResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending RemoteStop command to {ChargePointId}", request.ChargePointId);
            return new OcppCommandResultDto
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    public async Task<OcppCommandResultDto> ResetAsync(ResetRequestDto request)
    {
        try
        {
            var payload = new JObject
            {
                { "type", request.Type }
            };

            var response = await _messageDispatcher.SendCommandAsync(
                request.ChargePointId,
                "Reset",
                payload);

            return ParseResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Reset command to {ChargePointId}", request.ChargePointId);
            return new OcppCommandResultDto
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    public async Task<OcppCommandResultDto> UnlockConnectorAsync(UnlockConnectorRequestDto request)
    {
        try
        {
            var payload = new JObject
            {
                { "connectorId", request.ConnectorId }
            };

            var response = await _messageDispatcher.SendCommandAsync(
                request.ChargePointId,
                "UnlockConnector",
                payload);

            return ParseResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending UnlockConnector command to {ChargePointId}", request.ChargePointId);
            return new OcppCommandResultDto
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    public async Task<OcppCommandResultDto> ChangeAvailabilityAsync(ChangeAvailabilityRequestDto request)
    {
        try
        {
            var payload = new JObject
            {
                { "connectorId", request.ConnectorId },
                { "type", request.Type }
            };

            var response = await _messageDispatcher.SendCommandAsync(
                request.ChargePointId,
                "ChangeAvailability",
                payload);

            return ParseResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ChangeAvailability command to {ChargePointId}", request.ChargePointId);
            return new OcppCommandResultDto
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    public async Task<OcppCommandResultDto> GetConfigurationAsync(GetConfigurationRequestDto request)
    {
        try
        {
            var payload = new JObject();

            if (request.Keys != null && request.Keys.Count > 0)
            {
                payload["key"] = JArray.FromObject(request.Keys);
            }

            var response = await _messageDispatcher.SendCommandAsync(
                request.ChargePointId,
                "GetConfiguration",
                payload);

            return ParseResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending GetConfiguration command to {ChargePointId}", request.ChargePointId);
            return new OcppCommandResultDto
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    public async Task<OcppCommandResultDto> ChangeConfigurationAsync(ChangeConfigurationRequestDto request)
    {
        try
        {
            var payload = new JObject
            {
                { "key", request.Key },
                { "value", request.Value }
            };

            var response = await _messageDispatcher.SendCommandAsync(
                request.ChargePointId,
                "ChangeConfiguration",
                payload);

            return ParseResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ChangeConfiguration command to {ChargePointId}", request.ChargePointId);
            return new OcppCommandResultDto
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    public async Task<OcppCommandResultDto> TriggerMessageAsync(TriggerMessageRequestDto request)
    {
        try
        {
            var payload = new JObject
            {
                { "requestedMessage", request.RequestedMessage }
            };

            if (request.ConnectorId.HasValue)
            {
                payload["connectorId"] = request.ConnectorId.Value;
            }

            var response = await _messageDispatcher.SendCommandAsync(
                request.ChargePointId,
                "TriggerMessage",
                payload);

            return ParseResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending TriggerMessage command to {ChargePointId}", request.ChargePointId);
            return new OcppCommandResultDto
            {
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    private OcppCommandResultDto ParseResponse(JObject response)
    {
        if (response == null)
        {
            return new OcppCommandResultDto
            {
                Status = "Rejected",
                Message = "No response received from charging station"
            };
        }

        var status = response["status"]?.ToString() ?? "Unknown";
        var message = response["message"]?.ToString();

        return new OcppCommandResultDto
        {
            Status = status,
            Message = message,
            Data = response
        };
    }
}
