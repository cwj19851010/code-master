using CodeMaster.Core.Entities;
using CodeMaster.Core.Enums;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 职位实体
/// </summary>
public class SysPost : EntityBaseWithTenant
{
    /// <summary>
    /// 职位名称
    /// </summary>
    public string PostName { get; set; } = string.Empty;

    /// <summary>
    /// 数据可见范围（1本人 2部门 3全部）
    /// </summary>
    public int DataScope { get; set; } = (int)PostDataScope.Self;
}
