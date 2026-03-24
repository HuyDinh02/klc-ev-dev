using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Handles FirmwareStatusNotification messages from charge points.
/// Logs firmware update status (Downloaded, DownloadFailed, Downloading, Idle, InstallationFailed, Installing, Installed).
/// </summary>
public class FirmwareStatusNotificationHandler : IOcppMessageHandler
{
    private readonly ILogger<FirmwareStatusNotificationHandler> _logger;

    public FirmwareStatusNotificationHandler(ILogger<FirmwareStatusNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task<JObject> HandleAsync(
        string chargePointId,
        JObject payload,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract payload fields
            var statusStr = payload.Value<string>("status");

            if (string.IsNullOrWhiteSpace(statusStr))
            {
                _logger.LogWarning("FirmwareStatusNotification missing status for charge point {ChargePointId}",
                    chargePointId);
                return Task.FromResult(new JObject());
            }

            // Valid status values per OCPP spec
            var isValidStatus = statusStr switch
            {
                "Downloaded" or "DownloadFailed" or "Downloading" or
                "Idle" or "InstallationFailed" or "Installing" or "Installed" => true,
                _ => false
            };

            if (!isValidStatus)
            {
                _logger.LogWarning("FirmwareStatusNotification invalid status {Status} for {ChargePointId}",
                    statusStr, chargePointId);
            }
            else
            {
                _logger.LogInformation(
                    "FirmwareStatusNotification: {ChargePointId} status = {Status}",
                    chargePointId, statusStr);
            }

            return Task.FromResult(new JObject());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FirmwareStatusNotification for charge point {ChargePointId}",
                chargePointId);
            return Task.FromResult(new JObject());
        }
    }
}
