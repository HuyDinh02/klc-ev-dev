using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
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
    private readonly IOcppService _ocppService;
    private readonly IConfiguration _configuration;

    public InternalOcppController(
        IOcppRemoteCommandService remoteCommandService,
        OcppConnectionManager connectionManager,
        IOcppService ocppService,
        IConfiguration configuration)
    {
        _remoteCommandService = remoteCommandService;
        _connectionManager = connectionManager;
        _ocppService = ocppService;
        _configuration = configuration;
    }

    /// <summary>
    /// Get all connected chargers.
    /// Merges in-memory WebSocket connections (this instance) with DB Online stations
    /// to handle multi-instance Cloud Run deployments. In-memory data takes priority
    /// (richer: includes real-time heartbeat, vendor profile). DB-only entries appear
    /// when the charger's WebSocket is held by a different Cloud Run instance.
    /// </summary>
    [HttpGet("connections")]
    public async Task<ActionResult> GetConnections()
    {
        if (!ValidateApiKey()) return Unauthorized();

        // In-memory connections on this instance (richest data)
        var localConnections = _connectionManager.GetAllConnections()
            .ToDictionary(c => c.ChargePointId, c => c);

        // DB Online stations (authoritative across all instances)
        var onlineStations = await _ocppService.GetOnlineStationsAsync();

        // Merge: start with local in-memory, add DB-only entries for other instances
        var result = localConnections.Values
            .Select(c => new
            {
                c.ChargePointId,
                c.ConnectedAt,
                c.LastHeartbeat,
                c.IsRegistered,
                c.StationId,
                VendorProfile = (int)c.VendorProfileType,
            })
            .ToList<object>();

        foreach (var station in onlineStations)
        {
            if (!localConnections.ContainsKey(station.StationCode))
            {
                result.Add(new
                {
                    ChargePointId = station.StationCode,
                    ConnectedAt = (DateTime?)null,
                    LastHeartbeat = station.LastHeartbeat,
                    IsRegistered = true,
                    StationId = (Guid?)station.Id,
                    VendorProfile = (int)station.VendorProfile,
                });
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Get connection detail for a specific charger.
    /// Checks the in-memory connection manager first (this instance), then falls back to the
    /// DB station status to handle multi-instance Cloud Run deployments where the HTTP request
    /// may land on a different instance than the one holding the charger's WebSocket.
    /// </summary>
    [HttpGet("connections/{chargePointId}")]
    public async Task<ActionResult> GetConnection(string chargePointId)
    {
        if (!ValidateApiKey()) return Unauthorized();

        // Fast path: connection is on this instance
        var connection = _connectionManager.GetConnection(chargePointId);
        if (connection != null)
        {
            return Ok(new
            {
                connection.ChargePointId,
                connection.ConnectedAt,
                connection.LastHeartbeat,
                connection.IsRegistered,
                connection.StationId,
                VendorProfile = (int)connection.VendorProfileType,
                Source = "local"
            });
        }

        // Fallback: check DB station status (authoritative across all instances).
        // When BootNotification is processed on any instance, the station is set Online in the DB.
        // When the charger disconnects (or heartbeat monitor expires it), it is set Offline.
        var station = await _ocppService.GetStationByChargePointIdAsync(chargePointId);
        if (station != null && station.Status == StationStatus.Online)
        {
            return Ok(new
            {
                ChargePointId = chargePointId,
                ConnectedAt = (DateTime?)null,
                LastHeartbeat = station.LastHeartbeat,
                IsRegistered = true,
                StationId = (Guid?)station.Id,
                VendorProfile = (int)station.VendorProfile,
                Source = "db"
            });
        }

        return NotFound();
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
