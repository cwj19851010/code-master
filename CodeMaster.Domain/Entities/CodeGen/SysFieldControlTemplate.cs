using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 字段控件模板 —— 每种控件类型 × 页面区域 = 一条记录
/// 页面区域: add / edit / search / list / detail
/// HTML 含 [field.name] [field.nameLower] [field.description] 等标记
/// ScriptSections JSON 含该控件需要的 imports/refs/reactives/functions
/// </summary>
[SugarTable("sys_field_control_templates")]
public class SysFieldControlTemplate : EntityBase
{
    /// <summary>控件类型：input / select / select-table / date / number / textarea / switch / editor / file / image / cascader / checkbox / radio / datetime</summary>
    [SugarColumn(ColumnName = "control_type", Length = 64)]
    public string ControlType { get; set; } = string.Empty;

    /// <summary>页面区域：add / edit / search / list / detail</summary>
    [SugarColumn(ColumnName = "page_section", Length = 64)]
    public string PageSection { get; set; } = string.Empty;

    /// <summary>控件 HTML 模板，含 [field.*] markers</summary>
    [SugarColumn(ColumnName = "html_content", Length = 4000)]
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>ScriptSection JSON</summary>
    [SugarColumn(ColumnName = "script_sections", Length = 8000)]
    public string ScriptSections { get; set; } = string.Empty;

    /// <summary>排序</summary>
    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }
}
