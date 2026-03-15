using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Operators;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Controllers.Operators;

[ApiController]
[Route("api/v1/operators")]
[Authorize(KLCPermissions.Operators.Default)]
public class OperatorController : KLCController
{
    private readonly IOperatorAppService _operatorAppService;

    public OperatorController(IOperatorAppService operatorAppService)
    {
        _operatorAppService = operatorAppService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OperatorDto>>> GetListAsync([FromQuery] GetOperatorListDto input)
    {
        var result = await _operatorAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OperatorDetailDto>> GetAsync(Guid id)
    {
        var result = await _operatorAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(KLCPermissions.Operators.Create)]
    public async Task<ActionResult<CreateOperatorResultDto>> CreateAsync([FromBody] CreateOperatorDto input)
    {
        var result = await _operatorAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Operator.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(KLCPermissions.Operators.Update)]
    public async Task<ActionResult<OperatorDetailDto>> UpdateAsync(Guid id, [FromBody] UpdateOperatorDto input)
    {
        var result = await _operatorAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(KLCPermissions.Operators.Delete)]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        await _operatorAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/regenerate-api-key")]
    [Authorize(KLCPermissions.Operators.Update)]
    public async Task<ActionResult<OperatorApiKeyDto>> RegenerateApiKeyAsync(Guid id)
    {
        var result = await _operatorAppService.RegenerateApiKeyAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/stations/{stationId:guid}")]
    [Authorize(KLCPermissions.Operators.ManageStations)]
    public async Task<ActionResult> AddStationAsync(Guid id, Guid stationId)
    {
        await _operatorAppService.AddStationAsync(id, stationId);
        return NoContent();
    }

    [HttpDelete("{id:guid}/stations/{stationId:guid}")]
    [Authorize(KLCPermissions.Operators.ManageStations)]
    public async Task<ActionResult> RemoveStationAsync(Guid id, Guid stationId)
    {
        await _operatorAppService.RemoveStationAsync(id, stationId);
        return NoContent();
    }

    [HttpGet("{id:guid}/webhook-logs")]
    public async Task<ActionResult<List<OperatorWebhookLogDto>>> GetWebhookLogsAsync(Guid id, [FromQuery] GetWebhookLogsDto input)
    {
        var result = await _operatorAppService.GetWebhookLogsAsync(id, input);
        return Ok(result);
    }
}
