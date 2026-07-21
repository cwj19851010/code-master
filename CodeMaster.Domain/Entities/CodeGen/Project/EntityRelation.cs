using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

public enum EntityRelationCardinality
{
    OneToOne = 1,
    OneToMany = 2,
    ManyToOne = 3,
    ManyToMany = 4
}

public enum EntityRelationOwnership
{
    Owned = 1,
    Reference = 2,
    Snapshot = 3,
    ReadOnly = 4,
    Independent = 5
}

public enum EntityRelationDeleteBehavior
{
    Delete = 1,
    Keep = 2,
    Restrict = 3
}

[SugarTable("sys_entity_relation")]
public class EntityRelation : EntityBaseWithTenant
{
    [SugarColumn(ColumnName = "source_entity_id", IsNullable = false)]
    public long SourceEntityId { get; set; }

    [SugarColumn(ColumnName = "target_entity_id", IsNullable = false)]
    public long TargetEntityId { get; set; }

    [SugarColumn(ColumnName = "relation_name", Length = 100, IsNullable = false)]
    public string RelationName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "source_field", Length = 100, IsNullable = false)]
    public string SourceField { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "target_field", Length = 100, IsNullable = false)]
    public string TargetField { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "cardinality", IsNullable = false)]
    public EntityRelationCardinality Cardinality { get; set; } = EntityRelationCardinality.OneToOne;

    [SugarColumn(ColumnName = "ownership", IsNullable = false)]
    public EntityRelationOwnership Ownership { get; set; } = EntityRelationOwnership.Owned;

    [SugarColumn(ColumnName = "is_required", IsNullable = false)]
    public bool IsRequired { get; set; }

    [SugarColumn(ColumnName = "delete_behavior", IsNullable = false)]
    public EntityRelationDeleteBehavior DeleteBehavior { get; set; } = EntityRelationDeleteBehavior.Delete;

    [SugarColumn(ColumnName = "order_num", IsNullable = false)]
    public int OrderNum { get; set; }
}
