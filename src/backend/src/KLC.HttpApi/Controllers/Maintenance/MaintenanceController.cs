using System;
using System.Threading.Tasks;
using KLC.Maintenance;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KLC.Controllers.Maintenance;

[ApiController]
[Route("api/v1/maintenance")]
[Authorize(KLCPermissions.Maintenance.Default)]
public class MaintenanceController : KLCController
{
    private readonly IMaintenanceAppService _maintenanceAppService;

    public MaintenanceController(IMaintenanceAppService maintenanceAppService)
    {
        _maintenanceAppService = maintenanceAppService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<MaintenanceTaskDto>>> GetListAsync(
        [FromQuery] GetMaintenanceTaskListDto input)
    {
        var result = await _maintenanceAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MaintenanceTaskDto>> GetAsync(Guid id)
    {
        var result = await _maintenanceAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<MaintenanceStatsDto>> GetStatsAsync()
    {
        var result = await _maintenanceAppService.GetStatsAsync();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(KLCPermissions.Maintenance.Create)]
    public async Task<ActionResult<MaintenanceTaskDto>> CreateAsync(CreateMaintenanceTaskDto input)
    {
        var result = await _maintenanceAppService.CreateAsync(input);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(KLCPermissions.Maintenance.Update)]
    public async Task<ActionResult<MaintenanceTaskDto>> UpdateAsync(Guid id, UpdateMaintenanceTaskDto input)
    {
        var result = await _maintenanceAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(KLCPermissions.Maintenance.Delete)]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        await _maintenanceAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(KLCPermissions.Maintenance.Update)]
    public async Task<ActionResult<MaintenanceTaskDto>> StartAsync(Guid id)
    {
        var result = await _maintenanceAppService.StartAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(KLCPermissions.Maintenance.Update)]
    public async Task<ActionResult<MaintenanceTaskDto>> CompleteAsync(Guid id, CompleteMaintenanceTaskDto input)
    {
        var result = await _maintenanceAppService.CompleteAsync(id, input);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(KLCPermissions.Maintenance.Update)]
    public async Task<ActionResult<MaintenanceTaskDto>> CancelAsync(Guid id, CancelMaintenanceTaskDto input)
    {
        var result = await _maintenanceAppService.CancelAsync(id, input);
        return Ok(result);
    }
}
