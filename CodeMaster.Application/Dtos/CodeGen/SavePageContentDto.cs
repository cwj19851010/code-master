namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 保存页面模板请求
/// </summary>
public class SavePageContentDto
{
    /// <summary>template HTML 字符串（简单模式）</summary>
    public string? TemplateHtml { get; set; }

    /// <summary>Component 树 JSON（可视化编辑器模式，后端反序列化后用 VueTemplateSerializer 写盘）</summary>
    public string? TreeJson { get; set; }
}
