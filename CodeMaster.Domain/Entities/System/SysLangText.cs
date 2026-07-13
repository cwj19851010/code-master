using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 语言文本（全局共享，不区分租户）
/// </summary>
public class SysLangText : EntityBase
{
    /// <summary>
    /// 语言代码
    /// </summary>
    public string LangCode { get; set; } = string.Empty;

    /// <summary>
    /// 语言键（不区分界面）
    /// </summary>
    public string LangKey { get; set; } = string.Empty;

    /// <summary>
    /// 语言值
    /// </summary>
    public string LangValue { get; set; } = string.Empty;

    /// <summary>
    /// 分类（可选）
    /// </summary>
    public string? Category { get; set; }
}
