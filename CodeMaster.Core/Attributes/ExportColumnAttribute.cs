namespace CodeMaster.Core.Attributes;

/// <summary>
/// 导出列配置特性
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExportColumnAttribute : Attribute
{
    /// <summary>
    /// 固定列名（如果不为空，则使用此列名而不是翻译）
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// 固定翻译键（用于多语言翻译）
    /// </summary>
    public string? TitleKey { get; set; }

    /// <summary>
    /// 列顺序
    /// </summary>
    public int Order { get; set; } = 0;

    public ExportColumnAttribute()
    {
    }

    public ExportColumnAttribute(string titleKey)
    {
        TitleKey = titleKey;
    }

    public ExportColumnAttribute(string titleKey, int order)
    {
        TitleKey = titleKey;
        Order = order;
    }
}
