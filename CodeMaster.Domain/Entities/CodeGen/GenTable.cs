using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 代码生成表
/// </summary>
public class GenTable : EntityBaseWithTenant
{
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 实体名（大写开头）
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// 表描述
    /// </summary>
    public string? TableComment { get; set; }

    /// <summary>
    /// 功能名
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// 所属模块ID
    /// </summary>
    public long ModuleId { get; set; }

    /// <summary>
    /// 是否只读（0否 1是）
    /// </summary>
    public int IsReadOnly { get; set; } = 0;

    /// <summary>
    /// 是否只生成DTO（0否 1是）
    /// </summary>
    public int OnlyDto { get; set; } = 0;

    /// <summary>
    /// 是否树形结构（0否 1是）
    /// </summary>
    public int IsTree { get; set; } = 0;

    /// <summary>
    /// 是否被用作子表（0否 1是）
    /// </summary>
    public int IsChild { get; set; } = 0;

    /// <summary>
    /// 生成状态（0未生成 1已完成 2已修改）
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 树形父字段
    /// </summary>
    public string? TreeParentField { get; set; }

    /// <summary>
    /// 树形名称字段
    /// </summary>
    public string? TreeNameField { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public string? FunctionAuthor { get; set; }

    /// <summary>
    /// 生成路径
    /// </summary>
    public string? GenPath { get; set; }
}
