using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.UserManagement;

public interface IRoleManagementAppService : IApplicationService
{
    Task<PagedResultDto<RoleListDto>> GetListAsync(GetRoleListDto input);
    Task<RoleDetailDto> GetAsync(Guid id);
    Task<RoleDetailDto> CreateAsync(CreateRoleDto input);
    Task<RoleDetailDto> UpdateAsync(Guid id, UpdateRoleDto input);
    Task DeleteAsync(Guid id);
    Task<List<PermissionGroupDto>> GetPermissionsAsync(Guid roleId);
    Task UpdatePermissionsAsync(Guid roleId, UpdateRolePermissionsDto input);
}
