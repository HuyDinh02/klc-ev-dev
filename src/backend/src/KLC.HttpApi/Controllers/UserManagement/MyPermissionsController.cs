using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Authorization.Permissions;

namespace KLC.Controllers.UserManagement;

/// <summary>
/// Returns the current user's granted KLC permissions.
/// Any authenticated user can call this — no specific permission required.
/// </summary>
[ApiController]
[Route("api/v1/my-permissions")]
[Authorize]
public class MyPermissionsController : KLCController
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionChecker _permissionChecker;

    public MyPermissionsController(
        IPermissionDefinitionManager permissionDefinitionManager,
        IPermissionChecker permissionChecker)
    {
        _permissionDefinitionManager = permissionDefinitionManager;
        _permissionChecker = permissionChecker;
    }

    [HttpGet]
    public async Task<ActionResult<List<string>>> GetMyPermissionsAsync()
    {
        var groups = await _permissionDefinitionManager.GetGroupsAsync();
        var klcGroup = groups.FirstOrDefault(g => g.Name == KLCPermissions.GroupName);
        if (klcGroup == null) return Ok(new List<string>());

        var allPermissions = klcGroup.GetPermissionsWithChildren();
        var granted = new List<string>();

        foreach (var permission in allPermissions)
        {
            if (await _permissionChecker.IsGrantedAsync(permission.Name))
            {
                granted.Add(permission.Name);
            }
        }

        return Ok(granted);
    }
}
