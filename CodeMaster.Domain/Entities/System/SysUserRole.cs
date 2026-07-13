using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 用户角色关联实体
/// </summary>
[SugarTable("sys_user_roles")]
public class SysUserRole : IEntity
{
    /// <summary>
    /// 主键ID（复合主键的情况下，这个字段不使用）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public long Id { get; set; }
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsPrimaryKey = true)]
    public long UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnName = "role_id", IsPrimaryKey = true)]
    public long RoleId { get; set; }
}
