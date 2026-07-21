using System;
using System.Linq;
using System.Threading.Tasks;
using CodeMaster.Application.Services.CodeGen.Relations;
using CodeMaster.Domain.Entities.CodeGen;
using Xunit;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class EntityRelationGraphTests
{
    [Fact]
    public async Task BuildForProject_AdaptsLegacyAndOrdersNewEdges()
    {
        var (db, dbFile) = CreateDb();
        try
        {
            SeedEntity(db, 1, 1, "Order", "Id");
            SeedEntity(db, 2, 1, "OrderItem", "OrderId");
            SeedEntity(db, 3, 1, "OrderDetail", "OrderId");

            db.Insertable(new OneToManyRelation
            {
                Id = 100,
                ModuleEntityId = 1,
                MasterField = "Id",
                ChildEntityId = 2,
                ChildEntityName = "OrderItem",
                ChildForeignKey = "OrderId",
                OrderNum = 20,
                CreateBy = "test",
                CreateTime = DateTime.UtcNow,
                UpdateUserId = 0
            }).ExecuteCommand();
            db.Insertable(new EntityRelation
            {
                Id = 101,
                SourceEntityId = 1,
                TargetEntityId = 3,
                RelationName = "Detail",
                SourceField = "Id",
                TargetField = "OrderId",
                Cardinality = EntityRelationCardinality.OneToOne,
                Ownership = EntityRelationOwnership.Owned,
                DeleteBehavior = EntityRelationDeleteBehavior.Delete,
                OrderNum = 10,
                CreateBy = "test",
                CreateTime = DateTime.UtcNow,
                UpdateUserId = 0
            }).ExecuteCommand();

            var graph = await new EntityRelationGraphBuilder(db).BuildForProjectAsync(1);

            Assert.Collection(
                graph.GetOwnedOutgoing(1),
                edge =>
                {
                    Assert.False(edge.IsLegacy);
                    Assert.Equal(EntityRelationCardinality.OneToOne, edge.Cardinality);
                    Assert.Equal("Detail", edge.RelationName);
                },
                edge =>
                {
                    Assert.True(edge.IsLegacy);
                    Assert.Equal(EntityRelationCardinality.OneToMany, edge.Cardinality);
                    Assert.Equal("OrderItem", edge.RelationName);
                });
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    [Fact]
    public async Task Validate_RejectsOwnedCycle()
    {
        var (db, dbFile) = CreateDb();
        try
        {
            SeedEntity(db, 1, 1, "Order", "Id", "DetailId");
            SeedEntity(db, 2, 1, "OrderDetail", "OrderId", "Id");

            db.Insertable(new EntityRelation
            {
                Id = 201,
                SourceEntityId = 1,
                TargetEntityId = 2,
                RelationName = "Detail",
                SourceField = "Id",
                TargetField = "OrderId",
                Cardinality = EntityRelationCardinality.OneToOne,
                Ownership = EntityRelationOwnership.Owned,
                DeleteBehavior = EntityRelationDeleteBehavior.Delete,
                CreateBy = "test",
                CreateTime = DateTime.UtcNow,
                UpdateUserId = 0
            }).ExecuteCommand();

            var reverse = new EntityRelation
            {
                Id = 202,
                SourceEntityId = 2,
                TargetEntityId = 1,
                RelationName = "OrderBackReference",
                SourceField = "Id",
                TargetField = "DetailId",
                Cardinality = EntityRelationCardinality.OneToOne,
                Ownership = EntityRelationOwnership.Owned,
                DeleteBehavior = EntityRelationDeleteBehavior.Delete
            };

            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new EntityRelationGraphBuilder(db).ValidateAsync(reverse));

            Assert.Contains("cycle", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    [Fact]
    public async Task GetAffectedRoots_TraversesNestedOwnedEdges()
    {
        var graph = new EntityRelationGraph(new[]
        {
            Edge(1, 10, 20, "Items", EntityRelationCardinality.OneToMany, legacy: true),
            Edge(2, 20, 30, "Detail", EntityRelationCardinality.OneToOne),
            Edge(3, 30, 40, "Supplier", EntityRelationCardinality.ManyToOne, EntityRelationOwnership.Reference)
        });

        Assert.Equal(new long[] { 10 }, graph.GetAffectedRoots(30).OrderBy(x => x));
        Assert.Equal(new long[] { 40 }, graph.GetAffectedRoots(40).OrderBy(x => x));
        Assert.Equal(new long[] { 20, 30 }, graph.GetOwnedDescendants(10));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Validate_RejectsMismatchedRelationFieldTypes()
    {
        var (db, dbFile) = CreateDb();
        try
        {
            SeedEntity(db, 1, 1, "Order", "Id");
            SeedEntity(db, 2, 1, "OrderDetail", "OrderId");
            db.Updateable<EntityField>()
                .SetColumns(x => x.DataType == "string")
                .Where(x => x.ModuleEntityId == 2 && x.Name == "OrderId")
                .ExecuteCommand();

            var relation = new EntityRelation
            {
                SourceEntityId = 1,
                TargetEntityId = 2,
                RelationName = "Detail",
                SourceField = "Id",
                TargetField = "OrderId",
                Cardinality = EntityRelationCardinality.OneToOne,
                Ownership = EntityRelationOwnership.Owned,
                DeleteBehavior = EntityRelationDeleteBehavior.Delete
            };

            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new EntityRelationGraphBuilder(db).ValidateAsync(relation));

            Assert.Contains("types must match", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    [Fact]
    public async Task Validate_AllowsReferenceManyToOneForeignKeyToPrimaryKey()
    {
        var (db, dbFile) = CreateDb();
        try
        {
            SeedEntity(db, 1, 1, "Order", "Id", "CustomerId");
            SeedEntity(db, 2, 1, "Customer", "Id");

            var relation = new EntityRelation
            {
                SourceEntityId = 1,
                TargetEntityId = 2,
                RelationName = "Customer",
                SourceField = "CustomerId",
                TargetField = "Id",
                Cardinality = EntityRelationCardinality.ManyToOne,
                Ownership = EntityRelationOwnership.Reference,
                DeleteBehavior = EntityRelationDeleteBehavior.Restrict
            };

            await new EntityRelationGraphBuilder(db).ValidateAsync(relation);
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    [Fact]
    public async Task Validate_AllowsFrameworkImplicitIdPrimaryKey()
    {
        var (db, dbFile) = CreateDb();
        try
        {
            SeedEntity(db, 1, 1, "Order", "CustomerId");
            SeedEntity(db, 2, 1, "Customer", "Name");

            var relation = new EntityRelation
            {
                SourceEntityId = 1,
                TargetEntityId = 2,
                RelationName = "Customer",
                SourceField = "CustomerId",
                TargetField = "Id",
                Cardinality = EntityRelationCardinality.ManyToOne,
                Ownership = EntityRelationOwnership.Reference,
                DeleteBehavior = EntityRelationDeleteBehavior.Restrict
            };

            await new EntityRelationGraphBuilder(db).ValidateAsync(relation);
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    [Fact]
    public async Task Validate_RejectsOwnedOneToManyInNewRelationStore()
    {
        var (db, dbFile) = CreateDb();
        try
        {
            SeedEntity(db, 1, 1, "Order", "Id");
            SeedEntity(db, 2, 1, "OrderItem", "Id", "OrderId");
            var relation = new EntityRelation
            {
                SourceEntityId = 1,
                TargetEntityId = 2,
                RelationName = "Items",
                SourceField = "Id",
                TargetField = "OrderId",
                Cardinality = EntityRelationCardinality.OneToMany,
                Ownership = EntityRelationOwnership.Owned,
                DeleteBehavior = EntityRelationDeleteBehavior.Delete
            };

            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new EntityRelationGraphBuilder(db).ValidateAsync(relation));

            Assert.Contains("one-to-one", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    private static EntityRelationEdge Edge(
        long id,
        long source,
        long target,
        string name,
        EntityRelationCardinality cardinality,
        EntityRelationOwnership ownership = EntityRelationOwnership.Owned,
        bool legacy = false) =>
        new(id, source, target, name, "Id", source == 10 ? "OrderId" : "ParentId", cardinality,
            ownership, true, EntityRelationDeleteBehavior.Delete, (int)id, legacy);

    private static (SqlSugar.ISqlSugarClient Db, string DbFile) CreateDb()
    {
        var result = CodeGeneratorServiceTests.CreateMetadataDb();
        result.Db.CodeFirst.InitTables(typeof(OneToManyRelation), typeof(EntityRelation));
        return result;
    }

    private static void SeedEntity(
        SqlSugar.ISqlSugarClient db,
        long id,
        long projectId,
        string name,
        params string[] fields)
    {
        db.Insertable(new ModuleEntity
        {
            Id = id,
            ProjectId = projectId,
            ModuleId = 1,
            Name = name,
            Description = name,
            TableName = name.ToLowerInvariant(),
            HasPrimaryKey = true,
            CreateBy = "test",
            CreateTime = DateTime.UtcNow,
            UpdateUserId = 0
        }).ExecuteCommand();

        var fieldRows = fields.Distinct().Select((field, index) => new EntityField
        {
            Id = id * 100 + index + 1,
            ModuleEntityId = id,
            Name = field,
            Description = field,
            DataType = "long",
            IsPrimaryKey = field == "Id",
            CreateBy = "test",
            CreateTime = DateTime.UtcNow,
            UpdateUserId = 0
        }).ToList();
        db.Insertable(fieldRows).ExecuteCommand();
    }
}
