using SqlSugar;

namespace CodeMaster.Core.Entities;

/// <summary>
/// 带部门数据权限的实体基类（包含租户字段和部门字段）
/// </summary>
public abstract class EntityBaseWithDept : EntityBaseWithTenant, IDept
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
}
