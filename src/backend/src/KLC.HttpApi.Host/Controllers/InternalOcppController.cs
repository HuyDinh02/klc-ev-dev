using System.Threading.Tasks;
using KLC.Ocpp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KLC.Controllers;

/// <summary>
/// Internal OCPP endpoints for service-to-service calls (BFF → Admin API).
/// Authenticated via shared API key in X-Internal-Key header.
/// </summary>
[ApiController]
[Route("api/internal/ocpp")]
[AllowAnonymous]
public class InternalOcppController : ControllerBase
{
    private readonly IOcppRemoteCommandService _remoteCommandService;
    private readonly IConfiguration _configuration;

    public InternalOcppController(
        IOcppRemoteCommandService remoteCommandService,
        IConfiguration configuration)
    {
        _remoteCommandService = remoteCommandService;
        _configuration = configuration;
    }

    [HttpPost("remote-start")]
    public async Task<ActionResult<RemoteCommandResultDto>> RemoteStart(
        [FromBody] InternalRemoteStartRequest request)
    {
        if (!ValidateApiKey())
            return Unauthorized();

        var result = await _remoteCommandService.SendRemoteStartTransactionAsync(
            request.StationCode, request.ConnectorId, request.IdTag);

        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted
                ? "RemoteStartTransaction accepted"
                : result.ErrorMessage ?? "RemoteStartTransaction failed"
        });
    }

    [HttpPost("remote-stop")]
    public async Task<ActionResult<RemoteCommandResultDto>> RemoteStop(
        [FromBody] InternalRemoteStopRequest request)
    {
        if (!ValidateApiKey())
            return Unauthorized();

        var result = await _remoteCommandService.SendRemoteStopTransactionAsync(
            request.StationCode, request.TransactionId);

        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted
                ? "RemoteStopTransaction accepted"
                : result.ErrorMessage ?? "RemoteStopTransaction failed"
        });
    }

    private bool ValidateApiKey()
    {
        var expectedKey = _configuration["Internal:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey))
            return true; // No key configured = allow (dev mode)

        var providedKey = Request.Headers["X-Internal-Key"].ToString();
        return providedKey == expectedKey;
    }
}

public record InternalRemoteStartRequest
{
    public string StationCode { get; init; } = "";
    public int ConnectorId { get; init; }
    public string IdTag { get; init; } = "";
}

public record InternalRemoteStopRequest
{
    public string StationCode { get; init; } = "";
    public int TransactionId { get; init; }
}
