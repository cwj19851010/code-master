namespace CodeMaster.Core.Attributes;

/// <summary>
/// 导出外键配置特性
/// 用于指定外键关联的实体和显示字段
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExportForeignKeyAttribute : Attribute
{
    /// <summary>
    /// 关联的实体类型
    /// </summary>
    public Type EntityType { get; set; }

    /// <summary>
    /// 关联的字段名（实体中的属性名）
    /// </summary>
    public string RelatedField { get; set; }

    /// <summary>
    /// 显示字段名（要显示的属性名）
    /// </summary>
    public string DisplayField { get; set; }

    /// <summary>
    /// 导出时的列名（可选）
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// 导出时的翻译键（可选）
    /// </summary>
    public string? TitleKey { get; set; }

    /// <summary>
    /// 列顺序
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// 导入时是否允许通过显示字段反查外键ID
    /// </summary>
    public bool AllowImportByDisplay { get; set; } = true;

    public ExportForeignKeyAttribute(Type entityType, string relatedField, string displayField)
    {
        EntityType = entityType;
        RelatedField = relatedField;
        DisplayField = displayField;
    }
}
