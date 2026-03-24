using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Handles DataTransfer messages from charge points.
/// Accepts all vendor-specific data transfers in Phase 1.
/// </summary>
public class DataTransferHandler : IOcppMessageHandler
{
    private readonly ILogger<DataTransferHandler> _logger;

    public DataTransferHandler(ILogger<DataTransferHandler> logger)
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
            var vendorId = payload.Value<string>("vendorId");
            var messageId = payload.Value<string?>("messageId");
            var data = payload.Value<string?>("data");

            if (string.IsNullOrWhiteSpace(vendorId))
            {
                _logger.LogWarning("DataTransfer missing vendorId for charge point {ChargePointId}",
                    chargePointId);
                return Task.FromResult(BuildResponse("Rejected"));
            }

            _logger.LogInformation(
                "DataTransfer received from {ChargePointId}: VendorId={VendorId}, MessageId={MessageId}",
                chargePointId, vendorId, messageId ?? "N/A");

            if (!string.IsNullOrEmpty(data))
            {
                _logger.LogDebug("DataTransfer payload: {Data}", data);
            }

            // Accept all vendor data in Phase 1
            return Task.FromResult(BuildResponse("Accepted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DataTransfer for charge point {ChargePointId}",
                chargePointId);
            return Task.FromResult(BuildResponse("UnknownVendorId"));
        }
    }

    private static JObject BuildResponse(string status)
    {
        return new JObject
        {
            { "status", status }
        };
    }
}
