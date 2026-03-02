using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Controllers.Vehicles;

[ApiController]
[Route("api/v1/vehicles")]
public class VehicleController : KLCController
{
    private readonly IVehicleAppService _vehicleAppService;

    public VehicleController(IVehicleAppService vehicleAppService)
    {
        _vehicleAppService = vehicleAppService;
    }

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> CreateAsync([FromBody] CreateVehicleDto input)
    {
        var result = await _vehicleAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> GetAsync(Guid id)
    {
        var result = await _vehicleAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleDto>>> GetMyVehiclesAsync()
    {
        var result = await _vehicleAppService.GetMyVehiclesAsync();
        return Ok(result);
    }

    [HttpGet("default")]
    public async Task<ActionResult<VehicleDto>> GetDefaultVehicleAsync()
    {
        var result = await _vehicleAppService.GetDefaultVehicleAsync();
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> UpdateAsync(Guid id, [FromBody] UpdateVehicleDto input)
    {
        var result = await _vehicleAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        await _vehicleAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<ActionResult> SetAsDefaultAsync(Guid id)
    {
        await _vehicleAppService.SetAsDefaultAsync(id);
        return NoContent();
    }
}
