using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace KLC.UserManagement;

public interface IUserManagementAppService : IApplicationService
{
    Task<PagedResultDto<UserListDto>> GetListAsync(GetUserListDto input);
    Task<UserDto> GetAsync(Guid id);
    Task<UserDto> CreateAsync(CreateUserDto input);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto input);
    Task DeleteAsync(Guid id);
    Task<List<RoleDto>> GetRolesAsync();
    Task<List<string>> GetUserRolesAsync(Guid userId);
    Task UpdateUserRolesAsync(Guid userId, UpdateUserRolesDto input);
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
    Task LockUserAsync(Guid userId, LockUserDto input);
    Task UnlockUserAsync(Guid userId);
    Task ResetPasswordAsync(Guid userId, ResetPasswordDto input);
}
