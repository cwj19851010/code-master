using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 组件分组（对应 Element Plus 的 Basic/Form/Data/Navigation/Feedback 等分组）
/// </summary>
[SugarTable("sys_component_groups")]
public class SysComponentGroup : EntityBase
{
    [SugarColumn(ColumnName = "group_name", Length = 64)]
    public string GroupName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "group_code", Length = 64)]
    public string GroupCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "icon", Length = 64, IsNullable = true)]
    public string? Icon { get; set; }

    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }

    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(SysComponent.GroupId))]
    public List<SysComponent>? Components { get; set; }
}
