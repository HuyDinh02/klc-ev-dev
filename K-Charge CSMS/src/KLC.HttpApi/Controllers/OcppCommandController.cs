using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using KLC.Ocpp;

namespace KLC.HttpApi.Controllers;

[Area("app")]
[ControllerName("OcppCommands")]
[Route("api/app/ocpp-commands")]
public class OcppCommandController : AbpControllerBase
{
    private readonly IOcppCommandService _commandService;
    private readonly IOcppMessageDispatcher _messageDispatcher;

    public OcppCommandController(
        IOcppCommandService commandService,
        IOcppMessageDispatcher messageDispatcher)
    {
        _commandService = commandService;
        _messageDispatcher = messageDispatcher;
    }

    /// <summary>
    /// Get list of connected charge points
    /// </summary>
    [HttpGet("connected-chargepoints")]
    public async Task<List<string>> GetConnectedChargepointsAsync()
    {
        return await _messageDispatcher.GetConnectedChargepointsAsync();
    }

    /// <summary>
    /// Remote start a charging session
    /// </summary>
    [HttpPost("remote-start")]
    public async Task<OcppCommandResultDto> RemoteStartAsync(RemoteStartRequestDto request)
    {
        return await _commandService.RemoteStartAsync(request);
    }

    /// <summary>
    /// Remote stop a charging session
    /// </summary>
    [HttpPost("remote-stop")]
    public async Task<OcppCommandResultDto> RemoteStopAsync(RemoteStopRequestDto request)
    {
        return await _commandService.RemoteStopAsync(request);
    }

    /// <summary>
    /// Send reset command to charging station
    /// </summary>
    [HttpPost("reset")]
    public async Task<OcppCommandResultDto> ResetAsync(ResetRequestDto request)
    {
        return await _commandService.ResetAsync(request);
    }

    /// <summary>
    /// Unlock a connector
    /// </summary>
    [HttpPost("unlock-connector")]
    public async Task<OcppCommandResultDto> UnlockConnectorAsync(UnlockConnectorRequestDto request)
    {
        return await _commandService.UnlockConnectorAsync(request);
    }

    /// <summary>
    /// Change connector availability
    /// </summary>
    [HttpPost("change-availability")]
    public async Task<OcppCommandResultDto> ChangeAvailabilityAsync(ChangeAvailabilityRequestDto request)
    {
        return await _commandService.ChangeAvailabilityAsync(request);
    }

    /// <summary>
    /// Get configuration from a charging station
    /// </summary>
    [HttpGet("configuration/{chargePointId}")]
    public async Task<OcppCommandResultDto> GetConfigurationAsync(string chargePointId, [FromQuery] List<string> keys)
    {
        var request = new GetConfigurationRequestDto
        {
            ChargePointId = chargePointId,
            Keys = keys ?? new List<string>()
        };
        return await _commandService.GetConfigurationAsync(request);
    }

    /// <summary>
    /// Change configuration on a charging station
    /// </summary>
    [HttpPost("change-configuration")]
    public async Task<OcppCommandResultDto> ChangeConfigurationAsync(ChangeConfigurationRequestDto request)
    {
        return await _commandService.ChangeConfigurationAsync(request);
    }

    /// <summary>
    /// Trigger a message from the charging station
    /// </summary>
    [HttpPost("trigger-message")]
    public async Task<OcppCommandResultDto> TriggerMessageAsync(TriggerMessageRequestDto request)
    {
        return await _commandService.TriggerMessageAsync(request);
    }
}
