using System.Linq;
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
    private readonly OcppConnectionManager _connectionManager;
    private readonly IConfiguration _configuration;

    public InternalOcppController(
        IOcppRemoteCommandService remoteCommandService,
        OcppConnectionManager connectionManager,
        IConfiguration configuration)
    {
        _remoteCommandService = remoteCommandService;
        _connectionManager = connectionManager;
        _configuration = configuration;
    }

    [HttpGet("connections")]
    public ActionResult GetConnections()
    {
        if (!ValidateApiKey()) return Unauthorized();

        var connections = _connectionManager.GetAllConnections()
            .Select(c => new
            {
                c.ChargePointId,
                c.ConnectedAt,
                c.LastHeartbeat,
                c.IsRegistered,
                c.StationId,
                c.VendorProfileType
            }).ToList();

        return Ok(connections);
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

    /// <summary>
    /// Generic OCPP command forwarding — sends any action to the charger via WebSocket.
    /// Used by Admin API to forward commands when charger is on a separate Gateway.
    /// </summary>
    [HttpPost("command")]
    public async Task<ActionResult<RemoteCommandResultDto>> SendCommand(
        [FromBody] GenericCommandRequest request)
    {
        if (!ValidateApiKey())
            return Unauthorized();

        var connection = _connectionManager.GetConnection(request.StationCode);
        if (connection == null)
            return Ok(new RemoteCommandResultDto { Success = false, Message = "Station not connected to this gateway" });

        try
        {
            var response = await connection.SendCallAsync(
                request.Action, request.Payload, System.TimeSpan.FromSeconds(30));

            if (response == null)
                return Ok(new RemoteCommandResultDto { Success = false, Message = "Command timed out" });

            if (response.StartsWith("ERROR:"))
                return Ok(new RemoteCommandResultDto { Success = false, Message = response });

            return Ok(new RemoteCommandResultDto { Success = true, Message = "Command accepted" });
        }
        catch (System.Exception ex)
        {
            return Ok(new RemoteCommandResultDto { Success = false, Message = ex.Message });
        }
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

public record GenericCommandRequest
{
    public string StationCode { get; init; } = "";
    public string Action { get; init; } = "";
    public object? Payload { get; init; }
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
