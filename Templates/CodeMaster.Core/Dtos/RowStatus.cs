namespace CodeMaster.Core.Dtos;

/// <summary>
/// 行状态（用于子表增删改追踪）
/// </summary>
public enum RowStatus
{
    /// <summary>未修改</summary>
    Unchanged = 0,
    /// <summary>新增</summary>
    Added = 1,
    /// <summary>修改</summary>
    Modified = 2,
    /// <summary>删除</summary>
    Deleted = 3
}
