using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 角色菜单关联实体
/// </summary>
[SugarTable("sys_role_menus")]
public class SysRoleMenu : IEntity
{
    /// <summary>
    /// 主键ID（复合主键的情况下，这个字段不使用）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public long Id { get; set; }
    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnName = "role_id", IsPrimaryKey = true)]
    public long RoleId { get; set; }

    /// <summary>
    /// 菜单ID
    /// </summary>
    [SugarColumn(ColumnName = "menu_id", IsPrimaryKey = true)]
    public long MenuId { get; set; }
}
