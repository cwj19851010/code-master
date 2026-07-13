using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 页面主模板（index/add/edit/detail），HTML 含 [gen.*] [relation.*] markers
/// </summary>
[SugarTable("sys_page_templates")]
public class SysPageTemplate : EntityBase
{
    /// <summary>页面类型：index / add / edit / detail</summary>
    [SugarColumn(ColumnName = "page_type", Length = 64)]
    public string PageType { get; set; } = string.Empty;

    /// <summary>模板名称（显示用）</summary>
    [SugarColumn(ColumnName = "name", Length = 128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>主模板 HTML，含 [gen.entityDescription] [gen.addColumns] [gen.relationCards] 等标记</summary>
    [SugarColumn(ColumnName = "html_content", Length = 8000)]
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>默认 ScriptSection JSON（imports/uses/refs/reactives/functions/hooks）</summary>
    [SugarColumn(ColumnName = "script_sections", Length = 8000)]
    public string ScriptSections { get; set; } = string.Empty;

    /// <summary>是否系统模板</summary>
    [SugarColumn(ColumnName = "is_system")]
    public bool IsSystem { get; set; } = true;

    /// <summary>排序</summary>
    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }
}
