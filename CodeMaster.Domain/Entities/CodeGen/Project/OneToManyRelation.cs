using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 一对多关系配置
/// </summary>
[SugarTable("sys_one_to_many_relation")]
public class OneToManyRelation : EntityBaseWithTenant
{
    /// <summary>
    /// 主表实体 ID（ModuleEntity）
    /// </summary>
    [SugarColumn(ColumnName = "module_entity_id", IsNullable = false)]
    public long ModuleEntityId { get; set; }

    /// <summary>
    /// 主表关联字段名（不限于主键，如 Id、Code 等）
    /// </summary>
    [SugarColumn(ColumnName = "master_field", Length = 100, IsNullable = false)]
    public string MasterField { get; set; } = string.Empty;

    /// <summary>
    /// 子表实体 ID（ModuleEntity）
    /// </summary>
    [SugarColumn(ColumnName = "child_entity_id", IsNullable = false)]
    public long ChildEntityId { get; set; }

    /// <summary>
    /// 子表实体名称（冗余存储，如 OrderDetail）
    /// </summary>
    [SugarColumn(ColumnName = "child_entity_name", Length = 100, IsNullable = false)]
    public string ChildEntityName { get; set; } = string.Empty;

    /// <summary>
    /// 子表外键字段名（如 OrderId）
    /// </summary>
    [SugarColumn(ColumnName = "child_foreign_key", Length = 100, IsNullable = false)]
    public string ChildForeignKey { get; set; } = string.Empty;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order_num", IsNullable = false)]
    public int OrderNum { get; set; }
}
