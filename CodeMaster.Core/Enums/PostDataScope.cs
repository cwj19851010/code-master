namespace CodeMaster.Core.Enums;

/// <summary>
/// 职位数据可见范围
/// </summary>
public enum PostDataScope
{
    /// <summary>
    /// 全部数据权限
    /// </summary>
    All = 1,

    /// <summary>
    /// 本部门数据权限
    /// </summary>
    Dept = 2,

    /// <summary>
    /// 本部门及以下数据权限
    /// </summary>
    DeptAndBelow = 3,

    /// <summary>
    /// 仅本人数据权限
    /// </summary>
    Self = 4
}
