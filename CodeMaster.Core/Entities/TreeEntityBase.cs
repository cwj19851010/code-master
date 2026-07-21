using SqlSugar;

namespace CodeMaster.Core.Entities;

/// <summary>
/// 树形实体基类（支持路径枚举模型）
/// </summary>
public abstract class TreeEntityBase : EntityBase, ITree
{
    /// <summary>
    /// 父节点ID（根节点为null或0）
    /// </summary>
    [SugarColumn(ColumnName = "parent_id", IsNullable = true)]
    public long? ParentId { get; set; }

    /// <summary>
    /// 祖先节点路径（例如：0,1,5 表示根节点0 -> 节点1 -> 节点5）
    /// 用于快速查询所有子孙节点
    /// 不包含当前节点自己的ID
    /// </summary>
    [SugarColumn(ColumnName = "ancestors", Length = 500, IsNullable = true)]
    public string? Ancestors { get; set; }
}

/// <summary>
/// 树形实体基类（包含租户字段）
/// </summary>
public abstract class TreeEntityBaseWithTenant : TreeEntityBase, ITenantEntity
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnName = "tenant_id")]
    public long TenantId { get; set; }
}
