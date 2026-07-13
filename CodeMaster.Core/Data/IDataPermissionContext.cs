namespace CodeMaster.Core.Data;

/// <summary>
/// 数据权限上下文
/// </summary>
public interface IDataPermissionContext
{
    /// <summary>
    /// 是否启用数据权限过滤
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 是否管理员（管理员不受数据权限限制）
    /// </summary>
    bool IsAdmin { get; set; }

    /// <summary>
    /// 当前用户ID
    /// </summary>
    long? UserId { get; set; }

    /// <summary>
    /// 当前部门ID
    /// </summary>
    long? DeptId { get; set; }

    /// <summary>
    /// 当前部门路径（祖先路径，格式：0,1,2,3）
    /// </summary>
    string? DeptAncestors { get; set; }

    /// <summary>
    /// 数据可见范围（1全部 2部门 3部门及以下 4本人）
    /// </summary>
    int DataScope { get; set; }
}
