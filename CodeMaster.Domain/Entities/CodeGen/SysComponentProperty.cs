using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

[SugarTable("sys_component_properties")]
public class SysComponentProperty : EntityBase
{
    [SugarColumn(ColumnName = "component_id")]
    public long ComponentId { get; set; }

    [SugarColumn(ColumnName = "prop_name", Length = 128)]
    public string PropName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "prop_type", Length = 128, IsNullable = true)]
    public string? PropType { get; set; }

    [SugarColumn(ColumnName = "type_description", Length = 2048, IsNullable = true)]
    public string? TypeDescription { get; set; }

    [SugarColumn(ColumnName = "default_value", Length = 256, IsNullable = true)]
    public string? DefaultValue { get; set; }

    [SugarColumn(ColumnName = "description", Length = 512, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "is_common")]
    public bool IsCommon { get; set; }

    [SugarColumn(ColumnName = "enum_values", Length = 1024, IsNullable = true)]
    public string? EnumValues { get; set; }

    [SugarColumn(ColumnName = "is_advanced")]
    public bool IsAdvanced { get; set; }

    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }
}
