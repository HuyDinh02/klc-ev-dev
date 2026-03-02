using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KLC.Controllers.Sessions;

[ApiController]
[Route("api/v1/sessions")]
public class SessionController : KLCController
{
    private readonly ISessionAppService _sessionAppService;

    public SessionController(ISessionAppService sessionAppService)
    {
        _sessionAppService = sessionAppService;
    }

    [HttpPost("start")]
    public async Task<ActionResult<ChargingSessionDto>> StartAsync([FromBody] StartSessionDto input)
    {
        var result = await _sessionAppService.StartAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/stop")]
    public async Task<ActionResult<ChargingSessionDto>> StopAsync(Guid id, [FromBody] StopSessionDto? input = null)
    {
        var result = await _sessionAppService.StopAsync(id, input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ChargingSessionDto>> GetAsync(Guid id)
    {
        var result = await _sessionAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<ActiveSessionDto>> GetActiveSessionAsync()
    {
        var result = await _sessionAppService.GetActiveSessionAsync();
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet("history")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResultDto<SessionListDto>>> GetHistoryAsync([FromQuery] GetSessionListDto input)
    {
        var result = await _sessionAppService.GetHistoryAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}/meter-values")]
    [AllowAnonymous]
    public async Task<ActionResult<List<MeterValueDto>>> GetMeterValuesAsync(Guid id)
    {
        var result = await _sessionAppService.GetMeterValuesAsync(id);
        return Ok(result);
    }
}

[ApiController]
[Route("api/v1/admin/sessions")]
public class AdminSessionController : KLCController
{
    private readonly ISessionAppService _sessionAppService;

    public AdminSessionController(ISessionAppService sessionAppService)
    {
        _sessionAppService = sessionAppService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResultDto<SessionListDto>>> GetAllSessionsAsync([FromQuery] GetSessionListDto input)
    {
        var result = await _sessionAppService.GetAllSessionsAsync(input);
        return Ok(result);
    }
}
