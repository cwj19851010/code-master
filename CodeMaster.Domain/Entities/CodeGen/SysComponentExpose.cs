using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

[SugarTable("sys_component_exposes")]
public class SysComponentExpose : EntityBase
{
    [SugarColumn(ColumnName = "component_id")]
    public long ComponentId { get; set; }

    [SugarColumn(ColumnName = "expose_name", Length = 128)]
    public string ExposeName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "expose_type", Length = 128, IsNullable = true)]
    public string? ExposeType { get; set; }

    [SugarColumn(ColumnName = "type_description", Length = 2048, IsNullable = true)]
    public string? TypeDescription { get; set; }

    [SugarColumn(ColumnName = "is_common")]
    public bool IsCommon { get; set; }

    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }
}
