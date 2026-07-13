using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 语言（全局共享，不区分租户）
/// </summary>
public class SysLang : EntityBase
{
    /// <summary>
    /// 语言代码（如 zh-CN、en-US）
    /// </summary>
    public string LangCode { get; set; } = string.Empty;

    /// <summary>
    /// 语言名称
    /// </summary>
    public string LangName { get; set; } = string.Empty;

    /// <summary>
    /// 是否默认（0否 1是）
    /// </summary>
    public int IsDefault { get; set; } = 0;

    /// <summary>
    /// 是否启用（0启用 1停用）
    /// </summary>
    public int IsEnabled { get; set; } = 0;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;
}
