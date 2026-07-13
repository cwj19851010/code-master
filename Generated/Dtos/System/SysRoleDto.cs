using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// SysRole查询DTO
/// </summary>
public class SysRoleQueryDto : PagedQueryDto
{
    /// <summary>
    /// RoleName
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// RoleKey
    /// </summary>
    public string? RoleKey { get; set; }

    /// <summary>
    /// RoleSort
    /// </summary>
    public int? RoleSort { get; set; }

    /// <summary>
    /// DataScope
    /// </summary>
    public int? DataScope { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// SysRoleDTO
/// </summary>
public class SysRoleDto : DtoBase
{
    /// <summary>
    /// RoleName
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// RoleKey
    /// </summary>
    public string? RoleKey { get; set; }

    /// <summary>
    /// RoleSort
    /// </summary>
    public int RoleSort { get; set; }

    /// <summary>
    /// DataScope
    /// </summary>
    public int DataScope { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// DelFlag
    /// </summary>
    public int DelFlag { get; set; }

    /// <summary>
    /// Id
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// CreateBy
    /// </summary>
    public string? CreateBy { get; set; }

    /// <summary>
    /// CreateTime
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// UpdateBy
    /// </summary>
    public string? UpdateBy { get; set; }

    /// <summary>
    /// UpdateTime
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// 创建SysRoleDTO
/// </summary>
public class CreateSysRoleDto : CreateDtoBase
{
    /// <summary>
    /// RoleName
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// RoleKey
    /// </summary>
    public string? RoleKey { get; set; }

    /// <summary>
    /// RoleSort
    /// </summary>
    public int RoleSort { get; set; }

    /// <summary>
    /// DataScope
    /// </summary>
    public int DataScope { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// 更新SysRoleDTO
/// </summary>
public class UpdateSysRoleDto : UpdateDtoBase
{
    /// <summary>
    /// RoleName
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    /// RoleKey
    /// </summary>
    public string? RoleKey { get; set; }

    /// <summary>
    /// RoleSort
    /// </summary>
    public int RoleSort { get; set; }

    /// <summary>
    /// DataScope
    /// </summary>
    public int DataScope { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}
