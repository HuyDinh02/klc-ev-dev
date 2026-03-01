using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace KCharge.UserManagement;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
}

public class UserListDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string FullName => $"{Name} {Surname}".Trim();
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreationTime { get; set; }
}

public class GetUserListDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public string? RoleName { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsLockedOut { get; set; }
}

public class CreateUserDto
{
    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Name { get; set; }

    [StringLength(64)]
    public string? Surname { get; set; }

    [Phone]
    [StringLength(16)]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public bool LockoutEnabled { get; set; } = true;

    public List<string> RoleNames { get; set; } = new();
}

public class UpdateUserDto
{
    [Required]
    [StringLength(256)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Name { get; set; }

    [StringLength(64)]
    public string? Surname { get; set; }

    [Phone]
    [StringLength(16)]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public bool LockoutEnabled { get; set; }
}

public class UpdateUserRolesDto
{
    public List<string> RoleNames { get; set; } = new();
}

public class LockUserDto
{
    /// <summary>
    /// Lock duration in seconds. Use 0 for permanent lock.
    /// </summary>
    public int LockDurationSeconds { get; set; } = 3600;
}

public class ResetPasswordDto
{
    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsStatic { get; set; }
    public bool IsPublic { get; set; }
}

public class PermissionDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public bool IsGranted { get; set; }
}

public class PermissionGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();
}
