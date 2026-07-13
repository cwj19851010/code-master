using SqlSugar;

namespace CodeMaster.Core.Entities;

/// <summary>
/// 数据权限实体基类
/// 继承此基类的实体将自动启用数据权限过滤
/// </summary>
public abstract class DataPermissionEntityBase : EntityBaseWithTenant, IDept
{
    /// <summary>
    /// 部门ID（创建人所在部门）
    /// </summary>
    [SugarColumn(ColumnName = "dept_id", IsNullable = true)]
    public long? DeptId { get; set; }

    /// <summary>
    /// 部门路径（创建人所在部门的祖先路径，用于数据权限过滤）
    /// 格式：0,1,2,3 表示从根部门到当前部门的完整路径
    /// </summary>
    [SugarColumn(ColumnName = "dept_ancestors", Length = 500, IsNullable = true)]
    public string? DeptAncestors { get; set; }

    // CreateUserId 已在 EntityBase 中定义
}
