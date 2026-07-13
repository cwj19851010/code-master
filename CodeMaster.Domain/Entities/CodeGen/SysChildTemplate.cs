using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 子表模板（card / dialog），含 [relation.*] markers
/// </summary>
[SugarTable("sys_child_templates")]
public class SysChildTemplate : EntityBase
{
    /// <summary>页面类型：add / edit</summary>
    [SugarColumn(ColumnName = "page_type", Length = 64)]
    public string PageType { get; set; } = string.Empty;

    /// <summary>子表部分类型：card（表格卡片）/ dialog（编辑弹窗）</summary>
    [SugarColumn(ColumnName = "child_type", Length = 64)]
    public string ChildType { get; set; } = string.Empty;

    /// <summary>HTML 内容，含 [relation.*] markers</summary>
    [SugarColumn(ColumnName = "html_content", Length = 4000)]
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>ScriptSection JSON</summary>
    [SugarColumn(ColumnName = "script_sections", Length = 8000)]
    public string ScriptSections { get; set; } = string.Empty;

    /// <summary>排序</summary>
    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }
}
