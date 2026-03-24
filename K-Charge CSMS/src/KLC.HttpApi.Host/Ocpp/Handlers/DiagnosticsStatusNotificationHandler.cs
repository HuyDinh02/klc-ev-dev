using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Handles DiagnosticsStatusNotification messages from charge points.
/// Logs diagnostic status updates (Idle, Uploaded, UploadFailed, Uploading).
/// </summary>
public class DiagnosticsStatusNotificationHandler : IOcppMessageHandler
{
    private readonly ILogger<DiagnosticsStatusNotificationHandler> _logger;

    public DiagnosticsStatusNotificationHandler(ILogger<DiagnosticsStatusNotificationHandler> logger)
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
                _logger.LogWarning("DiagnosticsStatusNotification missing status for charge point {ChargePointId}",
                    chargePointId);
                return Task.FromResult(new JObject());
            }

            // Valid status values: Idle, Uploaded, UploadFailed, Uploading
            var isValidStatus = statusStr switch
            {
                "Idle" or "Uploaded" or "UploadFailed" or "Uploading" => true,
                _ => false
            };

            if (!isValidStatus)
            {
                _logger.LogWarning("DiagnosticsStatusNotification invalid status {Status} for {ChargePointId}",
                    statusStr, chargePointId);
            }
            else
            {
                _logger.LogInformation(
                    "DiagnosticsStatusNotification: {ChargePointId} status = {Status}",
                    chargePointId, statusStr);
            }

            return Task.FromResult(new JObject());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DiagnosticsStatusNotification for charge point {ChargePointId}",
                chargePointId);
            return Task.FromResult(new JObject());
        }
    }
}
