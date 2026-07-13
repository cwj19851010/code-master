using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

[SugarTable("sys_components")]
public class SysComponent : EntityBase
{
    [SugarColumn(ColumnName = "name", Length = 128)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "tag", Length = 128)]
    public string Tag { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "link", Length = 512)]
    public string Link { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "group_id")]
    public long GroupId { get; set; }

    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }

    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(GroupId))]
    public SysComponentGroup? Group { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(SysComponentProperty.ComponentId))]
    public List<SysComponentProperty>? Properties { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(SysComponentSlot.ComponentId))]
    public List<SysComponentSlot>? Slots { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(SysComponentEvent.ComponentId))]
    public List<SysComponentEvent>? Events { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(SysComponentExpose.ComponentId))]
    public List<SysComponentExpose>? Exposes { get; set; }
}
