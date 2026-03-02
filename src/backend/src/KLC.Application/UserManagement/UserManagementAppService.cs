using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace KLC.UserManagement;

[Authorize(KLCPermissions.UserManagement.Default)]
public class UserManagementAppService : KLCAppService, IUserManagementAppService
{
    private readonly IdentityUserManager _userManager;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionManager _permissionManager;

    public UserManagementAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        IPermissionDefinitionManager permissionDefinitionManager,
        IPermissionManager permissionManager)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionDefinitionManager = permissionDefinitionManager;
        _permissionManager = permissionManager;
    }

    public async Task<PagedResultDto<UserListDto>> GetListAsync(GetUserListDto input)
    {
        var totalCount = await _userRepository.GetCountAsync(
            filter: input.Filter,
            roleId: null,
            isLockedOut: input.IsLockedOut
        );

        var users = await _userRepository.GetListAsync(
            sorting: input.Sorting ?? "CreationTime DESC",
            maxResultCount: input.MaxResultCount,
            skipCount: input.SkipCount,
            filter: input.Filter,
            roleId: null,
            isLockedOut: input.IsLockedOut
        );

        var dtos = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(new UserListDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Name = user.Name,
                Surname = user.Surname,
                IsActive = user.IsActive,
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                Roles = roles.ToList(),
                CreationTime = user.CreationTime
            });
        }

        return new PagedResultDto<UserListDto>(totalCount, dtos);
    }

    public async Task<UserDto> GetAsync(Guid id)
    {
        var user = await _userManager.GetByIdAsync(id);
        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Roles = roles.ToList(),
            CreationTime = user.CreationTime,
            LastModificationTime = user.LastModificationTime
        };
    }

    [Authorize(KLCPermissions.UserManagement.Create)]
    public async Task<UserDto> CreateAsync(CreateUserDto input)
    {
        // Check if username exists
        var existingByUsername = await _userRepository.FindByNormalizedUserNameAsync(
            _userManager.NormalizeName(input.UserName));
        if (existingByUsername != null)
        {
            throw new BusinessException("KLC:UserNameAlreadyExists")
                .WithData("UserName", input.UserName);
        }

        // Check if email exists
        var existingByEmail = await _userRepository.FindByNormalizedEmailAsync(
            _userManager.NormalizeEmail(input.Email));
        if (existingByEmail != null)
        {
            throw new BusinessException("KLC:EmailAlreadyExists")
                .WithData("Email", input.Email);
        }

        var user = new IdentityUser(
            GuidGenerator.Create(),
            input.UserName,
            input.Email,
            CurrentTenant.Id
        )
        {
            Name = input.Name,
            Surname = input.Surname
        };

        user.SetIsActive(input.IsActive);
        await _userManager.SetLockoutEnabledAsync(user, input.LockoutEnabled);

        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            user.SetPhoneNumber(input.PhoneNumber, false);
        }

        var result = await _userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            throw new BusinessException("KLC:UserCreationFailed")
                .WithData("Errors", string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Assign roles
        if (input.RoleNames.Any())
        {
            await _userManager.SetRolesAsync(user, input.RoleNames);
        }

        return await GetAsync(user.Id);
    }

    [Authorize(KLCPermissions.UserManagement.Update)]
    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto input)
    {
        var user = await _userManager.GetByIdAsync(id);

        // Check if username exists (if changed)
        if (user.UserName != input.UserName)
        {
            var existingByUsername = await _userRepository.FindByNormalizedUserNameAsync(
                _userManager.NormalizeName(input.UserName));
            if (existingByUsername != null && existingByUsername.Id != id)
            {
                throw new BusinessException("KLC:UserNameAlreadyExists")
                    .WithData("UserName", input.UserName);
            }
        }

        // Check if email exists (if changed)
        if (user.Email != input.Email)
        {
            var existingByEmail = await _userRepository.FindByNormalizedEmailAsync(
                _userManager.NormalizeEmail(input.Email));
            if (existingByEmail != null && existingByEmail.Id != id)
            {
                throw new BusinessException("KLC:EmailAlreadyExists")
                    .WithData("Email", input.Email);
            }
        }

        await _userManager.SetUserNameAsync(user, input.UserName);
        await _userManager.SetEmailAsync(user, input.Email);

        user.Name = input.Name;
        user.Surname = input.Surname;
        user.SetIsActive(input.IsActive);
        await _userManager.SetLockoutEnabledAsync(user, input.LockoutEnabled);

        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            user.SetPhoneNumber(input.PhoneNumber, user.PhoneNumberConfirmed);
        }

        await _userManager.UpdateAsync(user);

        return await GetAsync(user.Id);
    }

    [Authorize(KLCPermissions.UserManagement.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var user = await _userManager.GetByIdAsync(id);

        // Prevent deleting the current user
        if (CurrentUser.Id == id)
        {
            throw new BusinessException("KLC:CannotDeleteCurrentUser");
        }

        await _userManager.DeleteAsync(user);
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await _roleRepository.GetListAsync();

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            IsDefault = r.IsDefault,
            IsStatic = r.IsStatic,
            IsPublic = r.IsPublic
        }).ToList();
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await _userManager.GetByIdAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    [Authorize(KLCPermissions.UserManagement.ManageRoles)]
    public async Task UpdateUserRolesAsync(Guid userId, UpdateUserRolesDto input)
    {
        var user = await _userManager.GetByIdAsync(userId);
        await _userManager.SetRolesAsync(user, input.RoleNames);
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        var permissions = new List<PermissionDto>();
        var allPermissions = await _permissionDefinitionManager.GetPermissionsAsync();

        foreach (var permission in allPermissions)
        {
            permissions.Add(new PermissionDto
            {
                Name = permission.Name,
                DisplayName = permission.DisplayName?.Localize(StringLocalizerFactory) ?? permission.Name,
                ParentName = permission.Parent?.Name,
                IsGranted = false
            });
        }

        return permissions;
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        var user = await _userManager.GetByIdAsync(userId);
        var grantedPermissions = new List<string>();
        var permissions = await GetAllPermissionsAsync();

        foreach (var permission in permissions)
        {
            var grantInfo = await _permissionManager.GetAsync(
                permission.Name,
                UserPermissionValueProvider.ProviderName,
                userId.ToString());
            if (grantInfo.IsGranted)
            {
                grantedPermissions.Add(permission.Name);
            }
        }

        return grantedPermissions;
    }

    [Authorize(KLCPermissions.UserManagement.Update)]
    public async Task LockUserAsync(Guid userId, LockUserDto input)
    {
        var user = await _userManager.GetByIdAsync(userId);

        if (CurrentUser.Id == userId)
        {
            throw new BusinessException("KLC:CannotLockCurrentUser");
        }

        var lockoutEnd = input.LockDurationSeconds == 0
            ? DateTimeOffset.MaxValue
            : DateTimeOffset.UtcNow.AddSeconds(input.LockDurationSeconds);

        await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
    }

    [Authorize(KLCPermissions.UserManagement.Update)]
    public async Task UnlockUserAsync(Guid userId)
    {
        var user = await _userManager.GetByIdAsync(userId);
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
    }

    [Authorize(KLCPermissions.UserManagement.Update)]
    public async Task ResetPasswordAsync(Guid userId, ResetPasswordDto input)
    {
        var user = await _userManager.GetByIdAsync(userId);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, input.NewPassword);

        if (!result.Succeeded)
        {
            throw new BusinessException("KLC:PasswordResetFailed")
                .WithData("Errors", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
