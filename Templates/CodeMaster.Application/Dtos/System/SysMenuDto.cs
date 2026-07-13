using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 菜单DTO
/// </summary>
public class SysMenuDto : EntityDto
{
    public string MenuName { get; set; } = string.Empty;
    public string? TitleKey { get; set; }
    public long? ParentId { get; set; }
    public string? Ancestors { get; set; }
    public int OrderNum { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Query { get; set; }
    public int IsFrame { get; set; }
    public int IsCache { get; set; }
    public string MenuType { get; set; } = string.Empty;
    public int Visible { get; set; }
    public int Status { get; set; }
    public string? Perms { get; set; }
    public string? Icon { get; set; }
    public int MenuScope { get; set; }
    public List<SysMenuDto>? Children { get; set; }
}

/// <summary>
/// 创建菜单DTO
/// </summary>
public class CreateSysMenuDto
{
    public string MenuName { get; set; } = string.Empty;
    public string? TitleKey { get; set; }
    public long? ParentId { get; set; }
    public int OrderNum { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Query { get; set; }
    public int IsFrame { get; set; } = 1;
    public int IsCache { get; set; } = 0;
    public string MenuType { get; set; } = "M";
    public int Visible { get; set; } = 0;
    public int Status { get; set; } = 0;
    public string? Perms { get; set; }
    public string? Icon { get; set; }
    public int MenuScope { get; set; } = 2;
    public string? Remark { get; set; }
}

/// <summary>
/// 更新菜单DTO
/// </summary>
public class UpdateSysMenuDto
{
    public long? NewParentId { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public string? TitleKey { get; set; }
    public int OrderNum { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Query { get; set; }
    public int IsFrame { get; set; }
    public int IsCache { get; set; }
    public string MenuType { get; set; } = string.Empty;
    public int Visible { get; set; }
    public int Status { get; set; }
    public string? Perms { get; set; }
    public string? Icon { get; set; }
    public int MenuScope { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 菜单查询DTO
/// </summary>
public class SysMenuQueryDto : PagedQueryDto
{
    public string? MenuName { get; set; }
    public int? Status { get; set; }
}
