using System;
using System.Threading.Tasks;
using KCharge.Stations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KCharge.Controllers.Stations;

[ApiController]
[Route("api/v1/stations")]
public class StationController : KChargeController
{
    private readonly IStationAppService _stationAppService;

    public StationController(IStationAppService stationAppService)
    {
        _stationAppService = stationAppService;
    }

    /// <summary>
    /// Creates a new charging station.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<StationDto>> CreateAsync([FromBody] CreateStationDto input)
    {
        var result = await _stationAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    /// <summary>
    /// Gets a station by ID with all connectors.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<StationDto>> GetAsync(Guid id)
    {
        var result = await _stationAppService.GetAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Gets a paginated list of stations.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResultDto<StationListDto>>> GetListAsync([FromQuery] GetStationListDto input)
    {
        var result = await _stationAppService.GetListAsync(input);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing charging station.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StationDto>> UpdateAsync(Guid id, [FromBody] UpdateStationDto input)
    {
        var result = await _stationAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    /// <summary>
    /// Decommissions a station (preserves history).
    /// </summary>
    [HttpPost("{id:guid}/decommission")]
    public async Task<ActionResult> DecommissionAsync(Guid id)
    {
        await _stationAppService.DecommissionAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Enables a station.
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    public async Task<ActionResult> EnableAsync(Guid id)
    {
        await _stationAppService.EnableAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Disables a station.
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    public async Task<ActionResult> DisableAsync(Guid id)
    {
        await _stationAppService.DisableAsync(id);
        return NoContent();
    }
}
