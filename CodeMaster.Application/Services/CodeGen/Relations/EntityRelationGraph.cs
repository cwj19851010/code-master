using CodeMaster.Domain.Entities.CodeGen;
using SqlSugar;

namespace CodeMaster.Application.Services.CodeGen.Relations;

public sealed record EntityRelationEdge(
    long Id,
    long SourceEntityId,
    long TargetEntityId,
    string RelationName,
    string SourceField,
    string TargetField,
    EntityRelationCardinality Cardinality,
    EntityRelationOwnership Ownership,
    bool IsRequired,
    EntityRelationDeleteBehavior DeleteBehavior,
    int OrderNum,
    bool IsLegacy);

public sealed class EntityRelationGraph
{
    private readonly IReadOnlyList<EntityRelationEdge> _edges;

    public EntityRelationGraph(IEnumerable<EntityRelationEdge> edges)
    {
        _edges = edges
            .OrderBy(x => x.OrderNum)
            .ThenBy(x => x.Id)
            .ToList();
    }

    public IReadOnlyList<EntityRelationEdge> Edges => _edges;

    public IReadOnlyList<EntityRelationEdge> GetOutgoing(long entityId) =>
        _edges.Where(x => x.SourceEntityId == entityId).ToList();

    public IReadOnlyList<EntityRelationEdge> GetOwnedOutgoing(long entityId) =>
        _edges.Where(x => x.SourceEntityId == entityId && x.Ownership == EntityRelationOwnership.Owned).ToList();

    public IReadOnlyList<long> GetOwnedDescendants(long entityId)
    {
        var result = new List<long>();
        var visited = new HashSet<long> { entityId };
        var queue = new Queue<long>();
        queue.Enqueue(entityId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var edge in GetOwnedOutgoing(current))
            {
                if (!visited.Add(edge.TargetEntityId)) continue;
                result.Add(edge.TargetEntityId);
                queue.Enqueue(edge.TargetEntityId);
            }
        }
        return result;
    }

    public IReadOnlySet<long> GetAffectedRoots(long changedEntityId)
    {
        var roots = new HashSet<long>();
        var visited = new HashSet<long>();
        var queue = new Queue<long>();
        queue.Enqueue(changedEntityId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;

            var parents = _edges
                .Where(x => x.TargetEntityId == current && x.Ownership == EntityRelationOwnership.Owned)
                .Select(x => x.SourceEntityId)
                .Distinct()
                .ToList();

            if (parents.Count == 0)
            {
                roots.Add(current);
                continue;
            }

            foreach (var parent in parents)
                queue.Enqueue(parent);
        }

        return roots;
    }
}

public sealed class EntityRelationGraphBuilder
{
    private readonly ISqlSugarClient _db;

    public EntityRelationGraphBuilder(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<EntityRelationGraph> BuildForProjectAsync(long projectId, bool includeDeleted = false)
    {
        var entityIds = await _db.Queryable<ModuleEntity>()
            .ClearFilter()
            .Where(x => x.ProjectId == projectId && (includeDeleted || !x.IsDeleted))
            .Select(x => x.Id)
            .ToListAsync();

        if (entityIds.Count == 0)
            return new EntityRelationGraph(Array.Empty<EntityRelationEdge>());

        var edges = new List<EntityRelationEdge>();

        if (_db.DbMaintenance.IsAnyTable("sys_one_to_many_relation"))
        {
            var legacy = await _db.Queryable<OneToManyRelation>()
                .ClearFilter()
                .Where(x => entityIds.Contains(x.ModuleEntityId) && (includeDeleted || !x.IsDeleted))
                .ToListAsync();

            edges.AddRange(legacy.Select(x => new EntityRelationEdge(
                x.Id,
                x.ModuleEntityId,
                x.ChildEntityId,
                string.IsNullOrWhiteSpace(x.ChildEntityName) ? $"Relation{x.Id}" : x.ChildEntityName,
                x.MasterField,
                x.ChildForeignKey,
                EntityRelationCardinality.OneToMany,
                EntityRelationOwnership.Owned,
                true,
                EntityRelationDeleteBehavior.Delete,
                x.OrderNum,
                true)));
        }

        if (_db.DbMaintenance.IsAnyTable("sys_entity_relation"))
        {
            var current = await _db.Queryable<EntityRelation>()
                .ClearFilter()
                .Where(x => entityIds.Contains(x.SourceEntityId) && (includeDeleted || !x.IsDeleted))
                .ToListAsync();

            edges.AddRange(current.Select(x => new EntityRelationEdge(
                x.Id,
                x.SourceEntityId,
                x.TargetEntityId,
                x.RelationName,
                x.SourceField,
                x.TargetField,
                x.Cardinality,
                x.Ownership,
                x.IsRequired,
                x.DeleteBehavior,
                x.OrderNum,
                false)));
        }

        return new EntityRelationGraph(edges);
    }

    public async Task ValidateAsync(EntityRelation relation, long? ignoredRelationId = null)
    {
        if (relation.SourceEntityId <= 0 || relation.TargetEntityId <= 0)
            throw new InvalidOperationException("Relation source and target entities are required.");
        if (relation.SourceEntityId == relation.TargetEntityId)
            throw new InvalidOperationException("An entity cannot own itself.");
        if (string.IsNullOrWhiteSpace(relation.RelationName))
            throw new InvalidOperationException("Relation name is required.");
        if (string.IsNullOrWhiteSpace(relation.SourceField) || string.IsNullOrWhiteSpace(relation.TargetField))
            throw new InvalidOperationException("Relation source and target fields are required.");
        if (relation.Ownership == EntityRelationOwnership.Owned &&
            relation.Cardinality != EntityRelationCardinality.OneToOne)
        {
            throw new InvalidOperationException("New owned relations currently support one-to-one cardinality only.");
        }
        if (relation.Cardinality == EntityRelationCardinality.ManyToMany)
            throw new InvalidOperationException("Many-to-many relations require an explicit junction entity and are not supported directly.");

        var entities = await _db.Queryable<ModuleEntity>()
            .ClearFilter()
            .Where(x => x.Id == relation.SourceEntityId || x.Id == relation.TargetEntityId)
            .ToListAsync();
        var source = entities.FirstOrDefault(x => x.Id == relation.SourceEntityId && !x.IsDeleted);
        var target = entities.FirstOrDefault(x => x.Id == relation.TargetEntityId && !x.IsDeleted);

        if (source == null || target == null)
            throw new InvalidOperationException("Relation source or target entity does not exist.");
        if (source.ProjectId != target.ProjectId)
            throw new InvalidOperationException("Relation entities must belong to the same project.");
        if (relation.Ownership == EntityRelationOwnership.Owned && (source.IsReadOnly || target.IsReadOnly))
            throw new InvalidOperationException("Owned one-to-one relations require writable source and target entities.");
        if (relation.Ownership == EntityRelationOwnership.Owned && !target.HasPrimaryKey)
            throw new InvalidOperationException("Owned one-to-one target entities require a primary key.");

        var fields = await _db.Queryable<EntityField>()
            .ClearFilter()
            .Where(x => (x.ModuleEntityId == relation.SourceEntityId && x.Name == relation.SourceField) ||
                        (x.ModuleEntityId == relation.TargetEntityId && x.Name == relation.TargetField))
            .ToListAsync();
        var sourceField = ResolveField(source, fields, relation.SourceField);
        var targetField = ResolveField(target, fields, relation.TargetField);
        if (sourceField == null)
            throw new InvalidOperationException($"Source field '{relation.SourceField}' does not exist.");
        if (targetField == null)
            throw new InvalidOperationException($"Target field '{relation.TargetField}' does not exist.");
        switch (relation.Cardinality)
        {
            case EntityRelationCardinality.OneToMany when !sourceField.IsPrimaryKey:
                throw new InvalidOperationException("One-to-many relations must use the source entity primary key.");
            case EntityRelationCardinality.ManyToOne when !targetField.IsPrimaryKey:
                throw new InvalidOperationException("Many-to-one relations must target the target entity primary key.");
            case EntityRelationCardinality.OneToOne
                when relation.Ownership == EntityRelationOwnership.Owned && !sourceField.IsPrimaryKey:
                throw new InvalidOperationException("Owned one-to-one relations must use the source entity primary key.");
            case EntityRelationCardinality.OneToOne
                when relation.Ownership != EntityRelationOwnership.Owned && !targetField.IsPrimaryKey:
                throw new InvalidOperationException("Reference one-to-one relations must target the target entity primary key.");
        }
        if (!string.Equals(NormalizeDataType(sourceField.DataType), NormalizeDataType(targetField.DataType), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Relation field types must match: {sourceField.DataType} and {targetField.DataType}.");
        }

        if (_db.DbMaintenance.IsAnyTable("sys_entity_relation"))
        {
            var duplicateQuery = _db.Queryable<EntityRelation>()
                .ClearFilter()
                .Where(x => !x.IsDeleted &&
                            x.TargetEntityId == relation.TargetEntityId &&
                            x.TargetField == relation.TargetField &&
                            x.Cardinality == EntityRelationCardinality.OneToOne &&
                            x.Ownership == EntityRelationOwnership.Owned);
            if (ignoredRelationId.HasValue)
            {
                var ignoredId = ignoredRelationId.Value;
                duplicateQuery = duplicateQuery.Where(x => x.Id != ignoredId);
            }
            var duplicateTarget = await duplicateQuery.AnyAsync();
            if (duplicateTarget)
                throw new InvalidOperationException("The owned one-to-one target field is already used by another relation.");
        }

        var graph = await BuildForProjectAsync(source.ProjectId);
        var edges = graph.Edges
            .Where(x => !ignoredRelationId.HasValue || x.IsLegacy || x.Id != ignoredRelationId.Value)
            .ToList();
        edges.Add(new EntityRelationEdge(
            relation.Id,
            relation.SourceEntityId,
            relation.TargetEntityId,
            relation.RelationName,
            relation.SourceField,
            relation.TargetField,
            relation.Cardinality,
            relation.Ownership,
            relation.IsRequired,
            relation.DeleteBehavior,
            relation.OrderNum,
            false));

        if (CreatesOwnedCycle(edges, relation.SourceEntityId, relation.TargetEntityId, out var cycle))
            throw new InvalidOperationException($"Owned relation cycle detected: {string.Join(" -> ", cycle)}");
    }

    private static string NormalizeDataType(string? dataType) =>
        (dataType ?? string.Empty).Trim().TrimEnd('?');

    private static EntityField? ResolveField(
        ModuleEntity entity,
        IReadOnlyCollection<EntityField> fields,
        string fieldName)
    {
        var field = fields.FirstOrDefault(x =>
            x.ModuleEntityId == entity.Id && x.Name == fieldName && !x.IsDeleted);
        if (field != null) return field;

        if (!entity.HasPrimaryKey || !string.Equals(fieldName, "Id", StringComparison.OrdinalIgnoreCase))
            return null;

        return new EntityField
        {
            ModuleEntityId = entity.Id,
            Name = "Id",
            Description = "Primary key",
            DataType = "long",
            IsPrimaryKey = true,
            IsSystemField = true
        };
    }

    private static bool CreatesOwnedCycle(
        IReadOnlyList<EntityRelationEdge> edges,
        long sourceEntityId,
        long targetEntityId,
        out IReadOnlyList<long> cycle)
    {
        var path = new List<long> { sourceEntityId };
        var visited = new HashSet<long>();

        bool Visit(long current)
        {
            path.Add(current);
            if (current == sourceEntityId && path.Count > 2)
                return true;
            if (!visited.Add(current))
            {
                path.RemoveAt(path.Count - 1);
                return false;
            }

            foreach (var edge in edges.Where(x =>
                         x.SourceEntityId == current && x.Ownership == EntityRelationOwnership.Owned))
            {
                if (Visit(edge.TargetEntityId)) return true;
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }

        if (Visit(targetEntityId))
        {
            cycle = path.ToList();
            return true;
        }

        cycle = Array.Empty<long>();
        return false;
    }
}
