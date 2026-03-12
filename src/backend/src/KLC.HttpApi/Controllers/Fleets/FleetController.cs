using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Fleets;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KLC.Controllers.Fleets;

[ApiController]
[Route("api/v1/fleets")]
[Authorize(KLCPermissions.Fleets.Default)]
public class FleetController : KLCController
{
    private readonly IFleetAppService _fleetAppService;

    public FleetController(IFleetAppService fleetAppService)
    {
        _fleetAppService = fleetAppService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<FleetDto>>> GetListAsync(
        [FromQuery] GetFleetListDto input)
    {
        var result = await _fleetAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FleetDetailDto>> GetAsync(Guid id)
    {
        var result = await _fleetAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(KLCPermissions.Fleets.Create)]
    public async Task<ActionResult<FleetDto>> CreateAsync(CreateFleetDto input)
    {
        var result = await _fleetAppService.CreateAsync(input);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(KLCPermissions.Fleets.Update)]
    public async Task<ActionResult<FleetDto>> UpdateAsync(Guid id, UpdateFleetDto input)
    {
        var result = await _fleetAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(KLCPermissions.Fleets.Delete)]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        await _fleetAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/vehicles")]
    [Authorize(KLCPermissions.Fleets.ManageVehicles)]
    public async Task<ActionResult<FleetVehicleDto>> AddVehicleAsync(Guid id, AddFleetVehicleDto input)
    {
        var result = await _fleetAppService.AddVehicleAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/vehicles/{vehicleId:guid}")]
    [Authorize(KLCPermissions.Fleets.ManageVehicles)]
    public async Task<ActionResult> RemoveVehicleAsync(Guid id, Guid vehicleId)
    {
        await _fleetAppService.RemoveVehicleAsync(id, vehicleId);
        return NoContent();
    }

    [HttpGet("{id:guid}/schedules")]
    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task<ActionResult<List<FleetChargingScheduleDto>>> GetSchedulesAsync(Guid id)
    {
        var result = await _fleetAppService.GetSchedulesAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/schedules")]
    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task<ActionResult<FleetChargingScheduleDto>> AddScheduleAsync(
        Guid id, CreateFleetScheduleDto input)
    {
        var result = await _fleetAppService.AddScheduleAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/schedules/{scheduleId:guid}")]
    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task<ActionResult> RemoveScheduleAsync(Guid id, Guid scheduleId)
    {
        await _fleetAppService.RemoveScheduleAsync(id, scheduleId);
        return NoContent();
    }

    [HttpPost("{id:guid}/allowed-station-groups/{stationGroupId:guid}")]
    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task<ActionResult<FleetAllowedStationGroupDto>> AddAllowedStationGroupAsync(
        Guid id, Guid stationGroupId)
    {
        var result = await _fleetAppService.AddAllowedStationGroupAsync(id, stationGroupId);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/allowed-station-groups/{stationGroupId:guid}")]
    [Authorize(KLCPermissions.Fleets.ManageSchedules)]
    public async Task<ActionResult> RemoveAllowedStationGroupAsync(Guid id, Guid stationGroupId)
    {
        await _fleetAppService.RemoveAllowedStationGroupAsync(id, stationGroupId);
        return NoContent();
    }

    [HttpGet("{id:guid}/analytics")]
    [Authorize(KLCPermissions.Fleets.ViewAnalytics)]
    public async Task<ActionResult<FleetAnalyticsDto>> GetAnalyticsAsync(
        Guid id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _fleetAppService.GetAnalyticsAsync(id, from, to);
        return Ok(result);
    }
}
