using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 角色实体
/// </summary>
public class SysRole : EntityBaseWithTenant
{
    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色权限字符串
    /// </summary>
    public string RoleKey { get; set; } = string.Empty;

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int RoleSort { get; set; } = 0;

    /// <summary>
    /// 数据范围（1全部数据权限 2自定数据权限 3本部门数据权限 4本部门及以下数据权限 5仅本人数据权限）
    /// </summary>
    public int DataScope { get; set; } = 1;

    /// <summary>
    /// Whether this role grants tenant administrator capabilities.
    /// </summary>
    public bool IsTenantAdmin { get; set; } = false;

    /// <summary>
    /// 状态（0正常 1停用）
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 删除标志（0未删除 1已删除）
    /// </summary>
    public int DelFlag { get; set; } = 0;
}
