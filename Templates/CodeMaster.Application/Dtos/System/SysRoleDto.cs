namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 角色DTO
/// </summary>
public class SysRoleDto
{
    public long Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleKey { get; set; } = string.Empty;
    public int RoleSort { get; set; }
    public bool IsTenantAdmin { get; set; }
    public int Status { get; set; }
    public DateTime CreateTime { get; set; }
    public string Remark { get; set; } = string.Empty;
}

/// <summary>
/// 创建角色DTO
/// </summary>
public class CreateSysRoleDto
{
    public string RoleName { get; set; } = string.Empty;
    public string RoleKey { get; set; } = string.Empty;
    public int RoleSort { get; set; }
    public int Status { get; set; } = 0;
    public string Remark { get; set; } = string.Empty;
    public List<long> MenuIds { get; set; } = new();
}

/// <summary>
/// 更新角色DTO
/// </summary>
public class UpdateSysRoleDto
{
    public long Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleKey { get; set; } = string.Empty;
    public int RoleSort { get; set; }
    public int Status { get; set; }
    public string Remark { get; set; } = string.Empty;
    public List<long> MenuIds { get; set; } = new();
}

/// <summary>
/// 角色查询DTO
/// </summary>
public class SysRoleQueryDto
{
    public string? RoleName { get; set; }
    public string? RoleKey { get; set; }
    public int? Status { get; set; }
    public DateTime? BeginTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
