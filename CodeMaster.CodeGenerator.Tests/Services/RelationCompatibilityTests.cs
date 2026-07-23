using System;
using System.Collections.Generic;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;
using Xunit;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class RelationCompatibilityTests
{
    [Fact]
    public void BuildTemplateContext_PreservesLegacyOneToManyShape()
    {
        var (db, dbFile) = CodeGeneratorServiceTests.CreateMetadataDb();
        try
        {
            db.CodeFirst.InitTables(typeof(OneToManyRelation));

            var parent = CreateEntity(10, "Order", "orders");
            var child = CreateEntity(11, "OrderItem", "order_items", isChildTable: true);
            db.Insertable(new[] { parent, child }).ExecuteCommand();

            db.Insertable(new[]
            {
                CreateField(101, parent.Id, "OrderNo", showInList: true),
                CreateField(201, child.Id, "OrderId"),
                CreateField(202, child.Id, "ProductName", showInList: true, showInAddForm: true),
                CreateField(203, child.Id, "InternalNote")
            }).ExecuteCommand();

            var relation = new OneToManyRelation
            {
                Id = 301,
                ModuleEntityId = parent.Id,
                MasterField = "Id",
                ChildEntityId = child.Id,
                ChildEntityName = child.Name,
                ChildForeignKey = "OrderId",
                OrderNum = 7,
                CreateBy = "test",
                CreateTime = DateTime.UtcNow,
                UpdateUserId = 0
            };

            var context = new CodeGeneratorService(db).BuildTemplateContext(
                parent,
                db.Queryable<EntityField>().Where(x => x.ModuleEntityId == parent.Id).ToList(),
                new List<OneToManyRelation> { relation },
                "CompatibilityProject",
                "Orders");

            var entity = Assert.IsAssignableFrom<IDictionary<string, object?>>(context["entity"]);
            Assert.True(Assert.IsType<bool>(entity["has_one_to_many"]));

            var relations = Assert.IsType<List<Dictionary<string, object?>>>(entity["one_to_many_relations"]);
            var generated = Assert.Single(relations);

            Assert.Equal("Id", generated["master_field"]);
            Assert.Equal(child.Id, generated["child_entity_id"]);
            Assert.Equal("OrderItem", generated["child_entity_name"]);
            Assert.Equal("orderItem", generated["child_entity_name_lower"]);
            Assert.Equal("orderitem", generated["child_entity_name_all_lower"]);
            Assert.Equal("OrderId", generated["child_foreign_key"]);

            var listFields = Assert.IsType<List<Dictionary<string, object?>>>(generated["child_fields"]);
            Assert.Single(listFields);
            Assert.Equal("ProductName", listFields[0]["name"]);

            var editableFields = Assert.IsType<List<Dictionary<string, object?>>>(generated["child_editable_fields"]);
            Assert.Single(editableFields);
            Assert.Equal("ProductName", editableFields[0]["name"]);
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    [Fact]
    public void BuildTemplateContext_PreservesLegacySelectTableShape()
    {
        var (db, dbFile) = CodeGeneratorServiceTests.CreateMetadataDb();
        try
        {
            var order = CreateEntity(20, "Order", "orders");
            var customer = CreateEntity(21, "Customer", "customers");
            db.Insertable(new[] { order, customer }).ExecuteCommand();

            var customerId = CreateField(401, order.Id, "CustomerId", showInAddForm: true);
            customerId.FormControlType = "select-table";
            customerId.RelatedEntityName = "Customer";
            customerId.RelatedEntityIdField = "Id";
            customerId.RelatedEntityDisplayFields = "[\"Name\",\"Code\"]";
            customerId.IsMultiple = false;
            db.Insertable(customerId).ExecuteCommand();

            var context = new CodeGeneratorService(db).BuildTemplateContext(
                order,
                new List<EntityField> { customerId },
                new List<OneToManyRelation>(),
                "CompatibilityProject",
                "Orders");

            var entity = Assert.IsAssignableFrom<IDictionary<string, object?>>(context["entity"]);
            var selectFields = Assert.IsType<List<Dictionary<string, object?>>>(entity["select_table_fields"]);
            var generated = Assert.Single(selectFields);

            Assert.Equal("CustomerId", generated["name"]);
            Assert.Equal("customerId", generated["name_lower"]);
            Assert.Equal("Customer", generated["related_entity_name"]);
            Assert.Equal("customer", generated["related_entity_name_lower"]);
            Assert.Equal("Id", generated["related_entity_id_field"]);
            Assert.Equal("id", generated["related_entity_id_field_lower"]);
            Assert.Equal("name", generated["related_display_label"]);
            var displayFields = Assert.IsType<List<Dictionary<string, object?>>>(generated["related_display_fields_list"]);
            Assert.Collection(
                displayFields,
                item =>
                {
                    Assert.Equal("Name", item["name"]);
                    Assert.Equal("name", item["name_lower"]);
                },
                item =>
                {
                    Assert.Equal("Code", item["name"]);
                    Assert.Equal("code", item["name_lower"]);
                });
            Assert.False(Assert.IsType<bool>(generated["is_multiple"]));
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    [Fact]
    public void BuildTemplateContext_AllowsOwnedOneToOneUsingImplicitId()
    {
        var (db, dbFile) = CodeGeneratorServiceTests.CreateMetadataDb();
        try
        {
            var order = CreateEntity(30, "Order", "orders");
            var detail = CreateEntity(31, "OrderDetail", "order_details", isChildTable: true);
            db.Insertable(new[] { order, detail }).ExecuteCommand();
            db.Insertable(CreateField(501, detail.Id, "OrderId")).ExecuteCommand();

            var context = new CodeGeneratorService(db).BuildTemplateContext(
                order,
                new List<EntityField>(),
                new List<OneToManyRelation>(),
                "CompatibilityProject",
                "Orders",
                new List<EntityRelation>
                {
                    new()
                    {
                        Id = 601,
                        SourceEntityId = order.Id,
                        TargetEntityId = detail.Id,
                        RelationName = "Detail",
                        SourceField = "Id",
                        TargetField = "OrderId",
                        Cardinality = EntityRelationCardinality.OneToOne,
                        Ownership = EntityRelationOwnership.Owned,
                        DeleteBehavior = EntityRelationDeleteBehavior.Delete
                    }
                });

            var entity = Assert.IsAssignableFrom<IDictionary<string, object?>>(context["entity"]);
            var relations = Assert.IsType<List<Dictionary<string, object?>>>(entity["owned_one_relations"]);
            var generated = Assert.Single(relations);
            Assert.Equal("long", generated["source_data_type"]);
            Assert.Equal("id", generated["create_source_expression"]);
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    [Fact]
    public void BuildTemplateContext_UsesBusinessFieldForOwnedOneToOne()
    {
        var (db, dbFile) = CodeGeneratorServiceTests.CreateMetadataDb();
        try
        {
            var order = CreateEntity(40, "Order", "orders");
            var detail = CreateEntity(41, "OrderDetail", "order_details", isChildTable: true);
            db.Insertable(new[] { order, detail }).ExecuteCommand();
            var orderNo = CreateField(701, order.Id, "OrderNo");
            var detailOrderNo = CreateField(702, detail.Id, "OrderNo");
            db.Insertable(new[] { orderNo, detailOrderNo }).ExecuteCommand();

            var context = new CodeGeneratorService(db).BuildTemplateContext(
                order,
                new List<EntityField> { orderNo },
                new List<OneToManyRelation>(),
                "CompatibilityProject",
                "Orders",
                new List<EntityRelation>
                {
                    new()
                    {
                        Id = 801,
                        SourceEntityId = order.Id,
                        TargetEntityId = detail.Id,
                        RelationName = "Detail",
                        SourceField = "OrderNo",
                        TargetField = "OrderNo",
                        Cardinality = EntityRelationCardinality.OneToOne,
                        Ownership = EntityRelationOwnership.Owned,
                        DeleteBehavior = EntityRelationDeleteBehavior.Delete
                    }
                });

            var entity = Assert.IsAssignableFrom<IDictionary<string, object?>>(context["entity"]);
            var relations = Assert.IsType<List<Dictionary<string, object?>>>(entity["owned_one_relations"]);
            var generated = Assert.Single(relations);
            Assert.Equal("string", generated["source_data_type"]);
            Assert.Equal("entity.OrderNo", generated["create_source_expression"]);
        }
        finally
        {
            db.Dispose();
            CodeGeneratorServiceTests.TryDelete(dbFile);
        }
    }

    private static ModuleEntity CreateEntity(long id, string name, string tableName, bool isChildTable = false) => new()
    {
        Id = id,
        ProjectId = 1,
        ModuleId = 1,
        Name = name,
        Description = name,
        TableName = tableName,
        HasPrimaryKey = true,
        GenerateFrontend = true,
        IsChildTable = isChildTable,
        CreateBy = "test",
        CreateTime = DateTime.UtcNow,
        UpdateUserId = 0
    };

    private static EntityField CreateField(
        long id,
        long entityId,
        string name,
        bool showInList = false,
        bool showInAddForm = false) => new()
    {
        Id = id,
        ModuleEntityId = entityId,
        Name = name,
        Description = name,
        DataType = "string",
        FormControlType = "input",
        ShowInList = showInList,
        ShowInAddForm = showInAddForm,
        CreateBy = "test",
        CreateTime = DateTime.UtcNow,
        UpdateUserId = 0
    };
}
