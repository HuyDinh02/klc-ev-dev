using System;
using System.Threading.Tasks;
using KCharge.StationGroups;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KCharge.Controllers.StationGroups;

[ApiController]
[Route("api/v1/station-groups")]
public class StationGroupController : KChargeController
{
    private readonly IStationGroupAppService _stationGroupAppService;

    public StationGroupController(IStationGroupAppService stationGroupAppService)
    {
        _stationGroupAppService = stationGroupAppService;
    }

    [HttpPost]
    public async Task<ActionResult<StationGroupDto>> CreateAsync([FromBody] CreateStationGroupDto input)
    {
        var result = await _stationGroupAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<StationGroupListDto>>> GetListAsync([FromQuery] GetStationGroupListDto input)
    {
        var result = await _stationGroupAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StationGroupDetailDto>> GetAsync(Guid id)
    {
        var result = await _stationGroupAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StationGroupDto>> UpdateAsync(Guid id, [FromBody] UpdateStationGroupDto input)
    {
        var result = await _stationGroupAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        await _stationGroupAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{groupId:guid}/assign")]
    public async Task<ActionResult> AssignStationAsync(Guid groupId, [FromBody] AssignStationDto input)
    {
        await _stationGroupAppService.AssignStationAsync(groupId, input);
        return NoContent();
    }

    [HttpDelete("{groupId:guid}/stations/{stationId:guid}")]
    public async Task<ActionResult> UnassignStationAsync(Guid groupId, Guid stationId)
    {
        await _stationGroupAppService.UnassignStationAsync(groupId, stationId);
        return NoContent();
    }
}
