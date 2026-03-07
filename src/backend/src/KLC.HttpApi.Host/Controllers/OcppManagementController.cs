using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Ocpp;
using KLC.Permissions;
using KLC.Stations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Repositories;

namespace KLC.Controllers;

/// <summary>
/// Admin endpoints for OCPP charger management.
/// </summary>
[ApiController]
[Route("api/v1/ocpp")]
[Authorize(KLCPermissions.Monitoring.Default)]
public class OcppManagementController : AbpControllerBase
{
    private readonly OcppConnectionManager _connectionManager;
    private readonly IOcppRemoteCommandService _remoteCommandService;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<OcppRawEvent, Guid> _rawEventRepository;

    public OcppManagementController(
        OcppConnectionManager connectionManager,
        IOcppRemoteCommandService remoteCommandService,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<OcppRawEvent, Guid> rawEventRepository)
    {
        _connectionManager = connectionManager;
        _remoteCommandService = remoteCommandService;
        _stationRepository = stationRepository;
        _rawEventRepository = rawEventRepository;
    }

    /// <summary>
    /// List all currently connected OCPP chargers.
    /// </summary>
    [HttpGet("connections")]
    public ActionResult<List<OcppConnectionDto>> GetConnections()
    {
        var connections = _connectionManager.GetAllConnections()
            .Select(c => new OcppConnectionDto
            {
                ChargePointId = c.ChargePointId,
                ConnectedAt = c.ConnectedAt,
                LastHeartbeat = c.LastHeartbeat,
                IsRegistered = c.IsRegistered,
                StationId = c.StationId,
                VendorProfile = c.VendorProfileType
            })
            .OrderBy(c => c.ChargePointId)
            .ToList();

        return Ok(connections);
    }

    /// <summary>
    /// Get OCPP connection status for a specific charger.
    /// </summary>
    [HttpGet("connections/{chargePointId}")]
    public async Task<ActionResult<OcppConnectionDetailDto>> GetConnection(string chargePointId)
    {
        var connection = _connectionManager.GetConnection(chargePointId);
        var station = await _stationRepository.FirstOrDefaultAsync(s => s.StationCode == chargePointId);

        if (connection == null && station == null)
            return NotFound();

        return Ok(new OcppConnectionDetailDto
        {
            ChargePointId = chargePointId,
            IsOnline = connection != null,
            ConnectedAt = connection?.ConnectedAt,
            LastHeartbeat = connection?.LastHeartbeat ?? station?.LastHeartbeat,
            IsRegistered = connection?.IsRegistered ?? false,
            StationId = connection?.StationId ?? station?.Id,
            VendorProfile = connection?.VendorProfileType ?? station?.VendorProfile ?? VendorProfileType.Generic,
            Vendor = station?.Vendor,
            Model = station?.Model,
            FirmwareVersion = station?.FirmwareVersion,
            SerialNumber = station?.SerialNumber
        });
    }

    /// <summary>
    /// Send RemoteStartTransaction to a charger.
    /// </summary>
    [HttpPost("connections/{chargePointId}/remote-start")]
    public async Task<ActionResult<RemoteCommandResultDto>> RemoteStart(
        string chargePointId,
        [FromBody] RemoteStartRequest request)
    {
        var success = await _remoteCommandService.SendRemoteStartTransactionAsync(
            chargePointId, request.ConnectorId, request.IdTag);

        return Ok(new RemoteCommandResultDto
        {
            Success = success,
            Message = success ? "RemoteStartTransaction accepted" : "RemoteStartTransaction failed or timed out"
        });
    }

    /// <summary>
    /// Send RemoteStopTransaction to a charger.
    /// </summary>
    [HttpPost("connections/{chargePointId}/remote-stop")]
    public async Task<ActionResult<RemoteCommandResultDto>> RemoteStop(
        string chargePointId,
        [FromBody] RemoteStopRequest request)
    {
        var success = await _remoteCommandService.SendRemoteStopTransactionAsync(
            chargePointId, request.TransactionId);

        return Ok(new RemoteCommandResultDto
        {
            Success = success,
            Message = success ? "RemoteStopTransaction accepted" : "RemoteStopTransaction failed or timed out"
        });
    }

    /// <summary>
    /// Send Reset (Soft/Hard) to a charger.
    /// </summary>
    [HttpPost("connections/{chargePointId}/reset")]
    public async Task<ActionResult<RemoteCommandResultDto>> Reset(
        string chargePointId,
        [FromBody] ResetRequest request)
    {
        var result = await _remoteCommandService.SendResetAsync(chargePointId, request.Type);
        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "Reset accepted" : result.ErrorMessage ?? "Reset rejected"
        });
    }

    /// <summary>
    /// Send UnlockConnector to a charger.
    /// </summary>
    [HttpPost("connections/{chargePointId}/unlock")]
    public async Task<ActionResult<RemoteCommandResultDto>> UnlockConnector(
        string chargePointId,
        [FromBody] UnlockConnectorRequest request)
    {
        var result = await _remoteCommandService.SendUnlockConnectorAsync(chargePointId, request.ConnectorId);
        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "Connector unlocked" : result.ErrorMessage ?? "Unlock rejected"
        });
    }

    /// <summary>
    /// Send ChangeAvailability to a charger.
    /// </summary>
    [HttpPost("connections/{chargePointId}/availability")]
    public async Task<ActionResult<RemoteCommandResultDto>> ChangeAvailability(
        string chargePointId,
        [FromBody] ChangeAvailabilityRequest request)
    {
        var result = await _remoteCommandService.SendChangeAvailabilityAsync(
            chargePointId, request.ConnectorId, request.Type);
        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "Availability changed" : result.ErrorMessage ?? "Change rejected"
        });
    }

    /// <summary>
    /// Get configuration keys from a charger.
    /// </summary>
    [HttpGet("connections/{chargePointId}/configuration")]
    public async Task<ActionResult<ConfigurationResultDto>> GetConfiguration(
        string chargePointId,
        [FromQuery] string? keys)
    {
        var keyList = string.IsNullOrWhiteSpace(keys)
            ? null
            : keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var result = await _remoteCommandService.SendGetConfigurationAsync(chargePointId, keyList);
        return Ok(new ConfigurationResultDto
        {
            Success = result.Accepted,
            ConfigurationKey = result.ConfigurationKey?.Select(k => new ConfigurationEntryDto
            {
                Key = k.Key,
                Value = k.Value,
                Readonly = k.Readonly
            }).ToList(),
            UnknownKey = result.UnknownKey
        });
    }

    /// <summary>
    /// Change a configuration key on a charger.
    /// </summary>
    [HttpPost("connections/{chargePointId}/configuration")]
    public async Task<ActionResult<RemoteCommandResultDto>> ChangeConfiguration(
        string chargePointId,
        [FromBody] ChangeConfigurationRequest request)
    {
        var result = await _remoteCommandService.SendChangeConfigurationAsync(
            chargePointId, request.Key, request.Value);
        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "Configuration changed" : result.ErrorMessage ?? "Change rejected"
        });
    }

    /// <summary>
    /// Send TriggerMessage to request a specific message from the charger.
    /// </summary>
    [HttpPost("connections/{chargePointId}/trigger")]
    public async Task<ActionResult<RemoteCommandResultDto>> TriggerMessage(
        string chargePointId,
        [FromBody] TriggerMessageRequest request)
    {
        var result = await _remoteCommandService.SendTriggerMessageAsync(
            chargePointId, request.RequestedMessage, request.ConnectorId);
        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "TriggerMessage accepted" : result.ErrorMessage ?? "Trigger rejected"
        });
    }

    /// <summary>
    /// Get OCPP raw event log for a charger.
    /// </summary>
    [HttpGet("events")]
    public async Task<ActionResult<List<OcppRawEventDto>>> GetEvents(
        [FromQuery] string? chargePointId,
        [FromQuery] string? action,
        [FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 200);

        var query = await _rawEventRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(chargePointId))
            query = query.Where(e => e.ChargePointId == chargePointId);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(e => e.Action == action);

        var events = await query.OrderByDescending(e => e.ReceivedAt)
            .Take(limit).ToListAsync();

        var dtos = events.Select(e => new OcppRawEventDto
        {
            Id = e.Id,
            ChargePointId = e.ChargePointId,
            Action = e.Action,
            UniqueId = e.UniqueId,
            MessageType = e.MessageType,
            Payload = e.Payload,
            LatencyMs = e.LatencyMs,
            VendorProfile = e.VendorProfile,
            ReceivedAt = e.ReceivedAt
        }).ToList();

        return Ok(dtos);
    }
}

#region DTOs

public class OcppConnectionDto
{
    public string ChargePointId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public bool IsRegistered { get; set; }
    public Guid? StationId { get; set; }
    public VendorProfileType VendorProfile { get; set; }
}

public class OcppConnectionDetailDto
{
    public string ChargePointId { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public bool IsRegistered { get; set; }
    public Guid? StationId { get; set; }
    public VendorProfileType VendorProfile { get; set; }
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? SerialNumber { get; set; }
}

public class RemoteStartRequest
{
    public int ConnectorId { get; set; }
    public string IdTag { get; set; } = string.Empty;
}

public class RemoteStopRequest
{
    public int TransactionId { get; set; }
}

public class ResetRequest
{
    public string Type { get; set; } = "Soft"; // Soft or Hard
}

public class UnlockConnectorRequest
{
    public int ConnectorId { get; set; }
}

public class ChangeAvailabilityRequest
{
    public int ConnectorId { get; set; }
    public string Type { get; set; } = "Operative"; // Operative or Inoperative
}

public class ChangeConfigurationRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class TriggerMessageRequest
{
    public string RequestedMessage { get; set; } = string.Empty;
    public int? ConnectorId { get; set; }
}

public class RemoteCommandResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ConfigurationResultDto
{
    public bool Success { get; set; }
    public List<ConfigurationEntryDto>? ConfigurationKey { get; set; }
    public List<string>? UnknownKey { get; set; }
}

public class ConfigurationEntryDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool Readonly { get; set; }
}

public class OcppRawEventDto
{
    public Guid Id { get; set; }
    public string ChargePointId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public int MessageType { get; set; }
    public string Payload { get; set; } = string.Empty;
    public long? LatencyMs { get; set; }
    public VendorProfileType VendorProfile { get; set; }
    public DateTime ReceivedAt { get; set; }
}

#endregion
