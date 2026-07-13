namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 页面模板内容 DTO（用于可视化设计器）
/// </summary>
public class PageContentDto
{
    /// <summary>页面类型：index / add / edit / detail</summary>
    public string PageType { get; set; } = string.Empty;

    /// <summary>文件名（如 index.vue）</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>template 内部 HTML 内容</summary>
    public string TemplateHtml { get; set; } = string.Empty;

    /// <summary>完整 .vue 文件内容（用于保留 script/style 部分）</summary>
    public string FullContent { get; set; } = string.Empty;

    /// <summary>Component 树 JSON（由后端 VueTemplateParser 解析，前端直接渲染）</summary>
    public string? TreeJson { get; set; }

    /// <summary>style 部分内容（供设计器注入，实现所见即所得）</summary>
    public string? StyleContent { get; set; }
}
