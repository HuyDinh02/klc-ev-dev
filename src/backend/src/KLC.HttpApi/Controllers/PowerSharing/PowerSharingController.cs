using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Permissions;
using KLC.PowerSharing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLC.Controllers.PowerSharing;

[ApiController]
[Route("api/v1/power-sharing")]
[Authorize(KLCPermissions.PowerSharing.Default)]
public class PowerSharingController : KLCController
{
    private readonly IPowerSharingAppService _powerSharingAppService;

    public PowerSharingController(IPowerSharingAppService powerSharingAppService)
    {
        _powerSharingAppService = powerSharingAppService;
    }

    [HttpPost]
    [Authorize(KLCPermissions.PowerSharing.Create)]
    public async Task<ActionResult<PowerSharingGroupDto>> CreateAsync([FromBody] CreatePowerSharingGroupDto input)
    {
        var result = await _powerSharingAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<List<PowerSharingGroupListDto>>> GetListAsync([FromQuery] GetPowerSharingGroupListDto input)
    {
        var result = await _powerSharingAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PowerSharingGroupDto>> GetAsync(Guid id)
    {
        var result = await _powerSharingAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(KLCPermissions.PowerSharing.Update)]
    public async Task<ActionResult<PowerSharingGroupDto>> UpdateAsync(Guid id, [FromBody] UpdatePowerSharingGroupDto input)
    {
        var result = await _powerSharingAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(KLCPermissions.PowerSharing.Delete)]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        await _powerSharingAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(KLCPermissions.PowerSharing.Update)]
    public async Task<ActionResult> ActivateAsync(Guid id)
    {
        await _powerSharingAppService.ActivateAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(KLCPermissions.PowerSharing.Update)]
    public async Task<ActionResult> DeactivateAsync(Guid id)
    {
        await _powerSharingAppService.DeactivateAsync(id);
        return NoContent();
    }

    [HttpPost("{groupId:guid}/members")]
    [Authorize(KLCPermissions.PowerSharing.ManageMembers)]
    public async Task<ActionResult<PowerSharingMemberDto>> AddMemberAsync(Guid groupId, [FromBody] AddMemberDto input)
    {
        var result = await _powerSharingAppService.AddMemberAsync(groupId, input);
        return Ok(result);
    }

    [HttpDelete("{groupId:guid}/members/{connectorId:guid}")]
    [Authorize(KLCPermissions.PowerSharing.ManageMembers)]
    public async Task<ActionResult> RemoveMemberAsync(Guid groupId, Guid connectorId)
    {
        await _powerSharingAppService.RemoveMemberAsync(groupId, connectorId);
        return NoContent();
    }

    [HttpPost("{groupId:guid}/recalculate")]
    public async Task<ActionResult<List<PowerAllocationDto>>> RecalculateAsync(Guid groupId)
    {
        var result = await _powerSharingAppService.RecalculateAsync(groupId);
        return Ok(result);
    }

    [HttpGet("{groupId:guid}/load-profiles")]
    public async Task<ActionResult<List<SiteLoadProfileDto>>> GetLoadProfilesAsync(
        Guid groupId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _powerSharingAppService.GetLoadProfilesAsync(groupId, from, to);
        return Ok(result);
    }
}
