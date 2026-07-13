using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

[SugarTable("sys_component_events")]
public class SysComponentEvent : EntityBase
{
    [SugarColumn(ColumnName = "component_id")]
    public long ComponentId { get; set; }

    [SugarColumn(ColumnName = "event_name", Length = 128)]
    public string EventName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "event_type", Length = 128, IsNullable = true)]
    public string? EventType { get; set; }

    [SugarColumn(ColumnName = "type_description", Length = 2048, IsNullable = true)]
    public string? TypeDescription { get; set; }

    [SugarColumn(ColumnName = "is_common")]
    public bool IsCommon { get; set; }

    [SugarColumn(ColumnName = "is_single")]
    public bool IsSingle { get; set; }

    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }
}
