namespace CodeMaster.Core.Entities;

/// <summary>
/// 部门数据权限接口
/// 实现此接口的实体将启用部门/本人数据可见范围过滤
/// </summary>
public interface IDept : IDataPermission
{
    /// <summary>
    /// 部门ID（创建人所在部门）
    /// </summary>
    long? DeptId { get; set; }

    /// <summary>
    /// 部门路径（创建人所在部门的祖先路径，用于数据权限过滤）
    /// 格式：0,1,2,3 表示从根部门到当前部门的完整��径
    /// </summary>
    string? DeptAncestors { get; set; }
}
