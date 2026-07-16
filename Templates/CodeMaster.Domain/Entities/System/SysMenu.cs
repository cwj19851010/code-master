using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 菜单实体
/// </summary>
public class SysMenu : TreeEntityBase
{
    /// <summary>
    /// 菜单名称
    /// </summary>
    public string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单标题语言键（用于前端本地化）
    /// </summary>
    public string? TitleKey { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int OrderNum { get; set; } = 0;

    /// <summary>
    /// 路由地址
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 组件路���
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 路由参数
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// 是否为外链（0否 1是）
    /// </summary>
    public int IsFrame { get; set; } = 0;

    /// <summary>
    /// 是否缓存（0不缓存 1缓存）
    /// </summary>
    public bool IsCache { get; set; } = true;

    /// <summary>
    /// 菜单类型（M目录 C菜单 F按钮）
    /// </summary>
    public string MenuType { get; set; } = "M";

    /// <summary>
    /// 显示状态（0显示 1隐藏）
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// 菜单状态（0正常 1停用）
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 权限标识
    /// </summary>
    public string? Perms { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 菜单范围（0=宿主专用 1=租户专用 2=共享）
    /// 0: 只有宿主可见
    /// 1: 只有租户可见
    /// 2: 宿主和租户都可见
    /// </summary>
    public int MenuScope { get; set; } = 2;
}
