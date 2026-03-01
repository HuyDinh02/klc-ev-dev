using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KCharge.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace KCharge.Controllers.UserManagement;

[ApiController]
[Route("api/v1/users")]
public class UserManagementController : KChargeController
{
    private readonly IUserManagementAppService _userManagementAppService;

    public UserManagementController(IUserManagementAppService userManagementAppService)
    {
        _userManagementAppService = userManagementAppService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<UserListDto>>> GetListAsync([FromQuery] GetUserListDto input)
    {
        var result = await _userManagementAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetAsync(Guid id)
    {
        var result = await _userManagementAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateAsync([FromBody] CreateUserDto input)
    {
        var result = await _userManagementAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> UpdateAsync(Guid id, [FromBody] UpdateUserDto input)
    {
        var result = await _userManagementAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        await _userManagementAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/roles")]
    public async Task<ActionResult<List<string>>> GetUserRolesAsync(Guid id)
    {
        var result = await _userManagementAppService.GetUserRolesAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult> UpdateUserRolesAsync(Guid id, [FromBody] UpdateUserRolesDto input)
    {
        await _userManagementAppService.UpdateUserRolesAsync(id, input);
        return NoContent();
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<ActionResult<List<string>>> GetUserPermissionsAsync(Guid id)
    {
        var result = await _userManagementAppService.GetUserPermissionsAsync(id);
        return Ok(result);
    }

    [HttpPost("{id:guid}/lock")]
    public async Task<ActionResult> LockUserAsync(Guid id, [FromBody] LockUserDto input)
    {
        await _userManagementAppService.LockUserAsync(id, input);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<ActionResult> UnlockUserAsync(Guid id)
    {
        await _userManagementAppService.UnlockUserAsync(id);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult> ResetPasswordAsync(Guid id, [FromBody] ResetPasswordDto input)
    {
        await _userManagementAppService.ResetPasswordAsync(id, input);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/roles")]
public class RoleManagementController : KChargeController
{
    private readonly IRoleManagementAppService _roleManagementAppService;
    private readonly IUserManagementAppService _userManagementAppService;

    public RoleManagementController(
        IRoleManagementAppService roleManagementAppService,
        IUserManagementAppService userManagementAppService)
    {
        _roleManagementAppService = roleManagementAppService;
        _userManagementAppService = userManagementAppService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<RoleListDto>>> GetListAsync([FromQuery] GetRoleListDto input)
    {
        var result = await _roleManagementAppService.GetListAsync(input);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<RoleDto>>> GetAllRolesAsync()
    {
        var result = await _userManagementAppService.GetRolesAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDetailDto>> GetAsync(Guid id)
    {
        var result = await _roleManagementAppService.GetAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDetailDto>> CreateAsync([FromBody] CreateRoleDto input)
    {
        var result = await _roleManagementAppService.CreateAsync(input);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleDetailDto>> UpdateAsync(Guid id, [FromBody] UpdateRoleDto input)
    {
        var result = await _roleManagementAppService.UpdateAsync(id, input);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        await _roleManagementAppService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<ActionResult<List<PermissionGroupDto>>> GetPermissionsAsync(Guid id)
    {
        var result = await _roleManagementAppService.GetPermissionsAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:guid}/permissions")]
    public async Task<ActionResult> UpdatePermissionsAsync(Guid id, [FromBody] UpdateRolePermissionsDto input)
    {
        await _roleManagementAppService.UpdatePermissionsAsync(id, input);
        return NoContent();
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetAllPermissionsAsync()
    {
        var result = await _userManagementAppService.GetAllPermissionsAsync();
        return Ok(result);
    }
}
