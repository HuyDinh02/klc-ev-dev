using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Ocpp;
using KLC.Permissions;
using KLC.Stations;
using KLC.Users;
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
    private readonly IRepository<UserIdTag, Guid> _userIdTagRepository;

    public OcppManagementController(
        OcppConnectionManager connectionManager,
        IOcppRemoteCommandService remoteCommandService,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<OcppRawEvent, Guid> rawEventRepository,
        IRepository<UserIdTag, Guid> userIdTagRepository)
    {
        _connectionManager = connectionManager;
        _remoteCommandService = remoteCommandService;
        _stationRepository = stationRepository;
        _rawEventRepository = rawEventRepository;
        _userIdTagRepository = userIdTagRepository;
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
            SerialNumber = station?.SerialNumber,
            FirmwareUpdateStatus = station?.FirmwareUpdateStatus,
            DiagnosticsStatus = station?.DiagnosticsStatus
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
        var result = await _remoteCommandService.SendRemoteStartTransactionAsync(
            chargePointId, request.ConnectorId, request.IdTag);

        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted
                ? "RemoteStartTransaction accepted"
                : result.ErrorMessage ?? "RemoteStartTransaction failed or timed out"
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
        var result = await _remoteCommandService.SendRemoteStopTransactionAsync(
            chargePointId, request.TransactionId);

        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted
                ? "RemoteStopTransaction accepted"
                : result.ErrorMessage ?? "RemoteStopTransaction failed or timed out"
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
    /// Send SetChargingProfile to apply a charging profile to a connector.
    /// </summary>
    [HttpPost("connections/{chargePointId}/set-charging-profile")]
    public async Task<ActionResult<RemoteCommandResultDto>> SetChargingProfile(
        string chargePointId,
        [FromBody] SetChargingProfileRequest request)
    {
        var profile = new ChargingProfilePayload(
            request.ChargingProfileId,
            request.TransactionId,
            request.StackLevel,
            request.ChargingProfilePurpose,
            request.ChargingProfileKind,
            new ChargingSchedulePayload(
                request.ChargingSchedule.ChargingRateUnit,
                request.ChargingSchedule.ChargingSchedulePeriod
                    .Select(p => new ChargingSchedulePeriodPayload(p.StartPeriod, p.Limit, p.NumberPhases))
                    .ToList()));

        var result = await _remoteCommandService.SendSetChargingProfileAsync(
            chargePointId, request.ConnectorId, profile);

        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "SetChargingProfile accepted" : result.ErrorMessage ?? "SetChargingProfile rejected"
        });
    }

    /// <summary>
    /// Send ClearChargingProfile to remove charging profile(s) from a charger.
    /// </summary>
    [HttpPost("connections/{chargePointId}/clear-charging-profile")]
    public async Task<ActionResult<RemoteCommandResultDto>> ClearChargingProfile(
        string chargePointId,
        [FromBody] ClearChargingProfileRequest request)
    {
        var result = await _remoteCommandService.SendClearChargingProfileAsync(
            chargePointId, request.Id, request.ConnectorId, request.ChargingProfilePurpose, request.StackLevel);

        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "ClearChargingProfile accepted" : result.ErrorMessage ?? "ClearChargingProfile rejected"
        });
    }

    /// <summary>
    /// Convenience endpoint to set a power limit on a connector.
    /// Wraps SetChargingProfile with a TxDefaultProfile at the specified kW limit.
    /// </summary>
    [HttpPost("connections/{chargePointId}/set-power-limit")]
    public async Task<ActionResult<RemoteCommandResultDto>> SetPowerLimit(
        string chargePointId,
        [FromBody] SetPowerLimitRequest request)
    {
        var limitWatts = request.MaxPowerKw * 1000m;

        var profile = new ChargingProfilePayload(
            ChargingProfileId: 1,
            TransactionId: null,
            StackLevel: 0,
            ChargingProfilePurpose: "TxDefaultProfile",
            ChargingProfileKind: "Absolute",
            ChargingSchedule: new ChargingSchedulePayload(
                ChargingRateUnit: "W",
                ChargingSchedulePeriod: new List<ChargingSchedulePeriodPayload>
                {
                    new(StartPeriod: 0, Limit: limitWatts)
                }));

        var result = await _remoteCommandService.SendSetChargingProfileAsync(
            chargePointId, request.ConnectorId, profile);

        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted
                ? $"Power limit set to {request.MaxPowerKw} kW ({limitWatts} W)"
                : result.ErrorMessage ?? "SetChargingProfile rejected"
        });
    }

    /// <summary>
    /// Send UpdateFirmware to instruct a charger to download and install new firmware.
    /// </summary>
    [HttpPost("connections/{chargePointId}/update-firmware")]
    public async Task<ActionResult<RemoteCommandResultDto>> UpdateFirmware(
        string chargePointId,
        [FromBody] UpdateFirmwareRequest request)
    {
        var result = await _remoteCommandService.SendUpdateFirmwareAsync(
            chargePointId, request.Location, request.RetrieveDate, request.Retries, request.RetryInterval);
        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "UpdateFirmware accepted" : result.ErrorMessage ?? "UpdateFirmware rejected"
        });
    }

    /// <summary>
    /// Send GetDiagnostics to instruct a charger to upload diagnostics.
    /// </summary>
    [HttpPost("connections/{chargePointId}/get-diagnostics")]
    public async Task<ActionResult<RemoteCommandResultDto>> GetDiagnostics(
        string chargePointId,
        [FromBody] GetDiagnosticsRequest request)
    {
        var result = await _remoteCommandService.SendGetDiagnosticsAsync(
            chargePointId, request.Location, request.StartTime, request.StopTime, request.Retries, request.RetryInterval);
        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "GetDiagnostics accepted" : result.ErrorMessage ?? "GetDiagnostics rejected"
        });
    }

    /// <summary>
    /// Get the current local authorization list version from a charger.
    /// </summary>
    [HttpGet("connections/{chargePointId}/local-list-version")]
    public async Task<ActionResult<LocalListVersionDto>> GetLocalListVersion(string chargePointId)
    {
        var result = await _remoteCommandService.SendGetLocalListVersionAsync(chargePointId);
        if (!result.Accepted)
            return Ok(new LocalListVersionDto { Success = false, ListVersion = -1 });

        return Ok(new LocalListVersionDto { Success = true, ListVersion = result.ListVersion });
    }

    /// <summary>
    /// Send a local authorization list update to a charger.
    /// </summary>
    [HttpPost("connections/{chargePointId}/send-local-list")]
    public async Task<ActionResult<RemoteCommandResultDto>> SendLocalList(
        string chargePointId,
        [FromBody] SendLocalListRequest request)
    {
        var entries = request.LocalAuthorizationList?.Select(e => new LocalAuthEntry(
            e.IdTag,
            e.IdTagInfo != null
                ? new IdTagInfoPayload(e.IdTagInfo.Status, e.IdTagInfo.ExpiryDate, e.IdTagInfo.ParentIdTag)
                : null
        )).ToList();

        var result = await _remoteCommandService.SendSendLocalListAsync(
            chargePointId, request.ListVersion, request.UpdateType, entries);

        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted
                ? "SendLocalList accepted"
                : result.ErrorMessage ?? $"SendLocalList rejected: {result.Status}"
        });
    }

    /// <summary>
    /// Convenience endpoint: sync all active UserIdTags to a charger as a full local authorization list.
    /// Queries the current list version, increments it, and pushes all active tags.
    /// </summary>
    [HttpPost("connections/{chargePointId}/sync-local-list")]
    public async Task<ActionResult<SyncLocalListResultDto>> SyncLocalList(string chargePointId)
    {
        // 1. Get current list version from charger
        var versionResult = await _remoteCommandService.SendGetLocalListVersionAsync(chargePointId);
        if (!versionResult.Accepted)
        {
            return Ok(new SyncLocalListResultDto
            {
                Success = false,
                Message = "Failed to retrieve current local list version from charger"
            });
        }

        var newVersion = versionResult.ListVersion + 1;

        // 2. Read all active UserIdTags from DB
        var queryable = await _userIdTagRepository.GetQueryableAsync();
        var activeTags = await queryable
            .Where(t => t.IsActive && !t.IsDeleted)
            .ToListAsync();

        // 3. Build local auth entries
        var entries = activeTags.Select(tag => new LocalAuthEntry(
            tag.IdTag,
            new IdTagInfoPayload(
                Status: "Accepted",
                ExpiryDate: tag.ExpiryDate?.ToString("o"),
                ParentIdTag: null
            )
        )).ToList();

        // 4. Send full update
        var result = await _remoteCommandService.SendSendLocalListAsync(
            chargePointId, newVersion, "Full", entries);

        return Ok(new SyncLocalListResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted
                ? $"Synced {entries.Count} tags to charger (version {newVersion})"
                : result.ErrorMessage ?? $"SendLocalList rejected: {result.Status}",
            ListVersion = newVersion,
            TagCount = entries.Count
        });
    }

    /// <summary>
    /// Send DataTransfer to a charger with vendor-specific data.
    /// </summary>
    [HttpPost("connections/{chargePointId}/data-transfer")]
    public async Task<ActionResult<RemoteCommandResultDto>> DataTransfer(
        string chargePointId,
        [FromBody] DataTransferRequest request)
    {
        var result = await _remoteCommandService.SendDataTransferAsync(
            chargePointId, request.VendorId, request.MessageId, request.Data);
        return Ok(new RemoteCommandResultDto
        {
            Success = result.Accepted,
            Message = result.Accepted ? "DataTransfer accepted" : result.ErrorMessage ?? "DataTransfer rejected"
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
    public string? FirmwareUpdateStatus { get; set; }
    public string? DiagnosticsStatus { get; set; }
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

public class SetChargingProfileRequest
{
    public int ConnectorId { get; set; }
    public int ChargingProfileId { get; set; }
    public int? TransactionId { get; set; }
    public int StackLevel { get; set; }
    public string ChargingProfilePurpose { get; set; } = "TxDefaultProfile";
    public string ChargingProfileKind { get; set; } = "Absolute";
    public ChargingScheduleDto ChargingSchedule { get; set; } = new();
}

public class ChargingScheduleDto
{
    public string ChargingRateUnit { get; set; } = "W";
    public List<ChargingSchedulePeriodDto> ChargingSchedulePeriod { get; set; } = new();
}

public class ChargingSchedulePeriodDto
{
    public int StartPeriod { get; set; }
    public decimal Limit { get; set; }
    public int? NumberPhases { get; set; }
}

public class ClearChargingProfileRequest
{
    public int? Id { get; set; }
    public int? ConnectorId { get; set; }
    public string? ChargingProfilePurpose { get; set; }
    public int? StackLevel { get; set; }
}

public class SetPowerLimitRequest
{
    public int ConnectorId { get; set; }
    public decimal MaxPowerKw { get; set; }
}

public class UpdateFirmwareRequest
{
    public string Location { get; set; } = string.Empty;
    public DateTime RetrieveDate { get; set; }
    public int? Retries { get; set; }
    public int? RetryInterval { get; set; }
}

public class GetDiagnosticsRequest
{
    public string Location { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? StopTime { get; set; }
    public int? Retries { get; set; }
    public int? RetryInterval { get; set; }
}

public class LocalListVersionDto
{
    public bool Success { get; set; }
    public int ListVersion { get; set; }
}

public class SendLocalListRequest
{
    public int ListVersion { get; set; }
    public string UpdateType { get; set; } = "Full"; // Full or Differential
    public List<LocalAuthEntryDto>? LocalAuthorizationList { get; set; }
}

public class LocalAuthEntryDto
{
    public string IdTag { get; set; } = string.Empty;
    public IdTagInfoDto? IdTagInfo { get; set; }
}

public class IdTagInfoDto
{
    public string Status { get; set; } = "Accepted";
    public string? ExpiryDate { get; set; }
    public string? ParentIdTag { get; set; }
}

public class SyncLocalListResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ListVersion { get; set; }
    public int TagCount { get; set; }
}

public class DataTransferRequest
{
    public string VendorId { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? Data { get; set; }
}

#endregion
