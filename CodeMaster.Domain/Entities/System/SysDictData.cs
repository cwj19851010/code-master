using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 字典数据
/// </summary>
public class SysDictData : EntityBaseWithTenant
{
    /// <summary>
    /// 字典类型
    /// </summary>
    public string DictType { get; set; } = string.Empty;

    /// <summary>
    /// 标签（用于显示，如果 LangKey 为空则直接显示此值）
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 值（用于存储和传输）
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 国际化键（如果有值，前端优先使用 $t(LangKey) 显示）
    /// </summary>
    public string? LangKey { get; set; }

    /// <summary>
    /// 是否默认（0否 1是）
    /// </summary>
    public int IsDefault { get; set; } = 0;

    /// <summary>
    /// 状态（0正常 1停用）
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;
}
