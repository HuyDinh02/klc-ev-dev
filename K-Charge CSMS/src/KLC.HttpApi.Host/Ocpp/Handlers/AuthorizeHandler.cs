using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using KLC.Authorization;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Handles Authorize messages from charge points.
/// Validates RFID tags against the authorization database.
/// </summary>
public class AuthorizeHandler : IOcppMessageHandler
{
    private readonly IIdTagRepository _idTagRepository;
    private readonly ILogger<AuthorizeHandler> _logger;

    public AuthorizeHandler(
        IIdTagRepository idTagRepository,
        ILogger<AuthorizeHandler> logger)
    {
        _idTagRepository = idTagRepository;
        _logger = logger;
    }

    public async Task<JObject> HandleAsync(
        string chargePointId,
        JObject payload,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract payload fields
            var idTag = payload.Value<string>("idTag");

            if (string.IsNullOrWhiteSpace(idTag))
            {
                _logger.LogWarning("Authorize message missing idTag for charge point {ChargePointId}",
                    chargePointId);
                return BuildResponse("Invalid");
            }

            // Find IdTag in database
            var tag = await _idTagRepository.FindByTagIdAsync(idTag, cancellationToken: cancellationToken);

            if (tag == null)
            {
                _logger.LogWarning("Authorize: unknown tag {IdTag} on charge point {ChargePointId}",
                    idTag, chargePointId);
                return BuildResponse("Invalid");
            }

            if (tag.IsBlocked)
            {
                _logger.LogWarning("Authorize: blocked tag {IdTag} on charge point {ChargePointId}",
                    idTag, chargePointId);
                return BuildResponse("Blocked");
            }

            if (tag.IsExpired)
            {
                _logger.LogWarning("Authorize: expired tag {IdTag} on charge point {ChargePointId}",
                    idTag, chargePointId);
                return BuildResponse("Expired");
            }

            _logger.LogInformation("Authorize: accepted tag {IdTag} on charge point {ChargePointId}",
                idTag, chargePointId);
            return BuildResponse("Accepted", tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Authorize for charge point {ChargePointId}",
                chargePointId);
            return BuildResponse("Invalid");
        }
    }

    private static JObject BuildResponse(string status, IdTag? tag = null)
    {
        var idTagInfo = new JObject
        {
            { "status", status }
        };

        if (tag?.ExpiryDate.HasValue == true)
        {
            idTagInfo["expiryDate"] = tag.ExpiryDate.Value.ToString("O");
        }

        if (!string.IsNullOrEmpty(tag?.ParentTagId))
        {
            idTagInfo["parentIdTag"] = tag.ParentTagId;
        }

        return new JObject
        {
            { "idTagInfo", idTagInfo }
        };
    }
}
