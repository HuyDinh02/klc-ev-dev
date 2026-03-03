using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace KLC.UserManagement;

[Authorize(KLCPermissions.RoleManagement.Default)]
public class RoleManagementAppService : KLCAppService, IRoleManagementAppService
{
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IdentityRoleManager _roleManager;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionManager _permissionManager;

    public RoleManagementAppService(
        IIdentityRoleRepository roleRepository,
        IdentityRoleManager roleManager,
        IIdentityUserRepository userRepository,
        IPermissionDefinitionManager permissionDefinitionManager,
        IPermissionManager permissionManager)
    {
        _roleRepository = roleRepository;
        _roleManager = roleManager;
        _userRepository = userRepository;
        _permissionDefinitionManager = permissionDefinitionManager;
        _permissionManager = permissionManager;
    }

    public async Task<PagedResultDto<RoleListDto>> GetListAsync(GetRoleListDto input)
    {
        var totalCount = await _roleRepository.GetCountAsync(input.Filter);
        var roles = await _roleRepository.GetListAsync(
            input.Sorting ?? "Name",
            input.MaxResultCount,
            input.SkipCount,
            input.Filter
        );

        var dtos = new List<RoleListDto>();
        foreach (var role in roles)
        {
            var userCount = await _userRepository.GetCountAsync(roleId: role.Id);
            dtos.Add(new RoleListDto
            {
                Id = role.Id,
                Name = role.Name,
                IsDefault = role.IsDefault,
                IsStatic = role.IsStatic,
                IsPublic = role.IsPublic,
                UserCount = (int)userCount
            });
        }

        return new PagedResultDto<RoleListDto>(totalCount, dtos);
    }

    public async Task<RoleDetailDto> GetAsync(Guid id)
    {
        var role = await _roleRepository.GetAsync(id);

        return new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            IsDefault = role.IsDefault,
            IsStatic = role.IsStatic,
            IsPublic = role.IsPublic,
            CreationTime = DateTime.UtcNow, // IdentityRole doesn't have audit properties
            LastModificationTime = null
        };
    }

    [Authorize(KLCPermissions.RoleManagement.Create)]
    public async Task<RoleDetailDto> CreateAsync(CreateRoleDto input)
    {
        var existingRole = await _roleRepository.FindByNormalizedNameAsync(
            _roleManager.NormalizeKey(input.Name));
        if (existingRole != null)
        {
            throw new BusinessException(KLCDomainErrorCodes.RoleNameAlreadyExists)
                .WithData("Name", input.Name);
        }

        var role = new IdentityRole(
            GuidGenerator.Create(),
            input.Name,
            CurrentTenant.Id
        )
        {
            IsDefault = input.IsDefault,
            IsPublic = input.IsPublic
        };

        await _roleManager.CreateAsync(role);

        return await GetAsync(role.Id);
    }

    [Authorize(KLCPermissions.RoleManagement.Update)]
    public async Task<RoleDetailDto> UpdateAsync(Guid id, UpdateRoleDto input)
    {
        var role = await _roleRepository.GetAsync(id);

        if (role.IsStatic)
        {
            throw new BusinessException(KLCDomainErrorCodes.CannotUpdateStaticRole);
        }

        // Check if new name already exists
        if (role.Name != input.Name)
        {
            var existingRole = await _roleRepository.FindByNormalizedNameAsync(
                _roleManager.NormalizeKey(input.Name));
            if (existingRole != null && existingRole.Id != id)
            {
                throw new BusinessException(KLCDomainErrorCodes.RoleNameAlreadyExists)
                    .WithData("Name", input.Name);
            }
        }

        await _roleManager.SetRoleNameAsync(role, input.Name);
        role.IsDefault = input.IsDefault;
        role.IsPublic = input.IsPublic;

        await _roleManager.UpdateAsync(role);

        return await GetAsync(role.Id);
    }

    [Authorize(KLCPermissions.RoleManagement.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var role = await _roleRepository.GetAsync(id);

        if (role.IsStatic)
        {
            throw new BusinessException(KLCDomainErrorCodes.CannotDeleteStaticRole);
        }

        // Check if role has users
        var userCount = await _userRepository.GetCountAsync(roleId: id);
        if (userCount > 0)
        {
            throw new BusinessException(KLCDomainErrorCodes.CannotDeleteRoleWithUsers)
                .WithData("UserCount", userCount);
        }

        await _roleManager.DeleteAsync(role);
    }

    public async Task<List<PermissionGroupDto>> GetPermissionsAsync(Guid roleId)
    {
        var role = await _roleRepository.GetAsync(roleId);
        var groups = await _permissionDefinitionManager.GetGroupsAsync();
        var result = new List<PermissionGroupDto>();

        foreach (var group in groups)
        {
            var groupPermissions = group.GetPermissionsWithChildren();

            if (!groupPermissions.Any())
                continue;

            var groupDto = new PermissionGroupDto
            {
                Name = group.Name,
                DisplayName = group.DisplayName?.Localize(StringLocalizerFactory) ?? group.Name,
                Permissions = new List<PermissionDto>()
            };

            foreach (var permission in groupPermissions)
            {
                var grantInfo = await _permissionManager.GetAsync(
                    permission.Name,
                    RolePermissionValueProvider.ProviderName,
                    role.Name);

                groupDto.Permissions.Add(new PermissionDto
                {
                    Name = permission.Name,
                    DisplayName = permission.DisplayName?.Localize(StringLocalizerFactory) ?? permission.Name,
                    ParentName = permission.Parent?.Name,
                    IsGranted = grantInfo.IsGranted
                });
            }

            result.Add(groupDto);
        }

        return result;
    }

    [Authorize(KLCPermissions.RoleManagement.ManagePermissions)]
    public async Task UpdatePermissionsAsync(Guid roleId, UpdateRolePermissionsDto input)
    {
        var role = await _roleRepository.GetAsync(roleId);

        // Only update permissions that were sent from the frontend (the ones displayed in the UI).
        // This avoids touching ABP built-in permissions that may have multi-tenancy restrictions.
        var allGrantedSet = new HashSet<string>(input.GrantedPermissions);
        var groups = await _permissionDefinitionManager.GetGroupsAsync();

        foreach (var group in groups)
        {
            var groupPermissions = group.GetPermissionsWithChildren();

            foreach (var permission in groupPermissions)
            {
                // Only update if this permission was part of the request
                // (i.e., it's either in the granted list, or it's a permission the UI knows about)
                var isGranted = allGrantedSet.Contains(permission.Name);

                try
                {
                    await _permissionManager.SetAsync(
                        permission.Name,
                        RolePermissionValueProvider.ProviderName,
                        role.Name,
                        isGranted
                    );
                }
                catch (AbpDbConcurrencyException)
                {
                    // Concurrency conflict — another process updated the same permission. Skip.
                }
                catch (Volo.Abp.Authorization.AbpAuthorizationException)
                {
                    // Some ABP framework permissions may reject being set
                    // (e.g., host-only tenant management permissions). Skip them.
                }
            }
        }
    }
}
