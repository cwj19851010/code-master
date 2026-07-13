using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// Vue 指令定义（全局通用，不绑定具体组件）
/// 如 v-if, v-show, v-for, v-model, v-permission 等
/// </summary>
[SugarTable("sys_directives")]
public class SysDirective : EntityBase
{
    [SugarColumn(ColumnName = "directive_name", Length = 64)]
    public string DirectiveName { get; set; } = string.Empty;

    /// <summary>是否有值表达式，如 v-else 无值，v-if="xxx" 有值</summary>
    [SugarColumn(ColumnName = "has_value")]
    public bool HasValue { get; set; } = true;

    /// <summary>值类型描述，如 "expression", "variable"</summary>
    [SugarColumn(ColumnName = "value_type", Length = 64, IsNullable = true)]
    public string? ValueType { get; set; }

    [SugarColumn(ColumnName = "description", Length = 256, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "is_common")]
    public bool IsCommon { get; set; }

    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }
}
