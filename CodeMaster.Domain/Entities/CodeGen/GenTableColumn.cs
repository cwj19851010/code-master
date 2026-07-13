using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 代码生成列
/// </summary>
public class GenTableColumn : EntityBaseWithTenant
{
    /// <summary>
    /// 归属表ID
    /// </summary>
    public long TableId { get; set; }

    /// <summary>
    /// 列名
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 属性名（大写开头）
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 列描述
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// 列类型
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// C#类型
    /// </summary>
    public string? CsharpType { get; set; }

    /// <summary>
    /// 是否主键（0否 1是）
    /// </summary>
    public int IsPk { get; set; } = 0;

    /// <summary>
    /// 是否自增（0否 1是）
    /// </summary>
    public int IsIncrement { get; set; } = 0;

    /// <summary>
    /// 是否必填（0否 1是）
    /// </summary>
    public int IsRequired { get; set; } = 0;

    /// <summary>
    /// 是否在列表显示（0否 1是）
    /// </summary>
    public int ShowInList { get; set; } = 1;

    /// <summary>
    /// 是否在新增显示（0否 1是）
    /// </summary>
    public int ShowInAdd { get; set; } = 1;

    /// <summary>
    /// 是否在编辑显示（0否 1是）
    /// </summary>
    public int ShowInEdit { get; set; } = 1;

    /// <summary>
    /// 是否在详情显示（0否 1是）
    /// </summary>
    public int ShowInDetail { get; set; } = 1;

    /// <summary>
    /// 是否查询条件（0否 1是）
    /// </summary>
    public int IsQuery { get; set; } = 0;

    /// <summary>
    /// 查询方式（EQ/NE/GT/LT/LIKE/BETWEEN）
    /// </summary>
    public string? QueryType { get; set; } = "EQ";

    /// <summary>
    /// 表单类型（input/textarea/select/radio/checkbox/date/datetime等）
    /// </summary>
    public string? HtmlType { get; set; } = "input";

    /// <summary>
    /// 字典类型
    /// </summary>
    public string? DictType { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 状态（0未生成 1已完成 2已修改）
    /// </summary>
    public int Status { get; set; } = 0;
}
