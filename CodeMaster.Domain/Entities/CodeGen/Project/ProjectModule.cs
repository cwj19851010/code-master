using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 项目模块实体
/// </summary>
public class ProjectModule : EntityBaseWithTenant
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 模块名称（英文，用于权限标识和路由）
    /// 例如：sales, inventory, finance
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述（中文，用于显示）
    /// 例如：销售管理, 库存管理, 财务管理
    /// </summary>
    public string ModuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 路由路径
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// 是否已同步到目标项目菜单
    /// </summary>
    public bool IsSynced { get; set; }

    /// <summary>
    /// 最后同步时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
