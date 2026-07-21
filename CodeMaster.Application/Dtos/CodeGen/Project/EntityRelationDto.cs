using CodeMaster.Core.Dtos;
using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Application.Dtos.CodeGen;

public class EntityRelationDto : EntityDto
{
    public long SourceEntityId { get; set; }

    public long TargetEntityId { get; set; }

    public string RelationName { get; set; } = string.Empty;

    public string SourceField { get; set; } = string.Empty;

    public string TargetField { get; set; } = string.Empty;

    public EntityRelationCardinality Cardinality { get; set; }

    public EntityRelationOwnership Ownership { get; set; }

    public bool IsRequired { get; set; }

    public EntityRelationDeleteBehavior DeleteBehavior { get; set; }

    public int OrderNum { get; set; }

    public string? TargetEntityName { get; set; }

    public string? TargetEntityDescription { get; set; }
}

public class CreateEntityRelationDto
{
    public long TargetEntityId { get; set; }

    public string RelationName { get; set; } = string.Empty;

    public string SourceField { get; set; } = string.Empty;

    public string TargetField { get; set; } = string.Empty;

    public EntityRelationCardinality Cardinality { get; set; } = EntityRelationCardinality.OneToOne;

    public EntityRelationOwnership Ownership { get; set; } = EntityRelationOwnership.Owned;

    public bool IsRequired { get; set; }

    public EntityRelationDeleteBehavior DeleteBehavior { get; set; } = EntityRelationDeleteBehavior.Delete;

    public int OrderNum { get; set; }
}

public class UpdateEntityRelationDto : CreateEntityRelationDto
{
}

public class UpdateEntityRelationWithIdDto : UpdateEntityRelationDto
{
    public long Id { get; set; }
}
