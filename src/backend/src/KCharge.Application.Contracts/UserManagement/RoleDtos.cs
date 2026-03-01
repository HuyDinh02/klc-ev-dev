using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace KCharge.UserManagement;

public class RoleListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsStatic { get; set; }
    public bool IsPublic { get; set; }
    public int UserCount { get; set; }
}

public class RoleDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsStatic { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
}

public class GetRoleListDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}

public class CreateRoleDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsPublic { get; set; } = true;
}

public class UpdateRoleDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsPublic { get; set; }
}

public class UpdateRolePermissionsDto
{
    public List<string> GrantedPermissions { get; set; } = new();
}
