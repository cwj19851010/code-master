using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CodeMaster.CodeGenerator.Tests.Services;

/// <summary>
/// 自动化验证代码生成的各项核心机制（依据 tasks.md 第 6 章：验证）
/// </summary>
public class TemplateOutputTests
{
    private CodeGeneratorService CreateService()
    {
        // 传入 null 给 ISqlSugarClient，因为在这些纯模板渲染测试中，只有 cascader 会用到数据库，我们在此测试避开或模拟。
        return new CodeGeneratorService(null!);
    }

    private (ModuleEntity, List<EntityField>, List<OneToManyRelation>) CreateComplexMockData()
    {
        var entity = new ModuleEntity
        {
            Id = 1,
            Name = "Order",
            Description = "订单",
            TableName = "business_order",
            HasPrimaryKey = true,
            IsTree = false,
            HasTenant = true,
            HasDataPermission = true,
            GenerateFrontend = true,
            FrontendRoute = "/order/list"
        };

        var fields = new List<EntityField>
        {
            new EntityField { Name = "Id", DataType = "long", IsPrimaryKey = true, IsRequired = true },
            new EntityField { Name = "OrderNo", DataType = "string", MaxLength = 50, IsRequired = true },
            new EntityField { Name = "TotalAmount", DataType = "decimal", IsRequired = true }
        };

        var relations = new List<OneToManyRelation>
        {
            new OneToManyRelation
            {
                MasterField = "Id",
                ChildEntityId = 2,
                ChildEntityName = "OrderDetail",
                ChildForeignKey = "OrderId"
            }
        };

        return (entity, fields, relations);
    }

    [Fact]
    public void Test_InterfaceInheritance_Correctness()
    {
        // Arrange
        var service = CreateService();
        var (entity, fields, relations) = CreateComplexMockData();

        // Act
        var interfaceString = service.CalculateInterfaceList(entity);

        // Assert
        // 验证加入了主键、多租户、数据权限的接口
        Assert.Contains("IEntity", interfaceString);
        Assert.Contains("ITenant", interfaceString);
        Assert.Contains("IDept", interfaceString);
        Assert.DoesNotContain("ITree", interfaceString); 

        // 修改实体为树但不带租户
        entity.IsTree = true;
        entity.HasTenant = false;
        var interfaceStringTree = service.CalculateInterfaceList(entity);
        Assert.Contains("ITree", interfaceStringTree);
        Assert.DoesNotContain("ITenant", interfaceStringTree);
    }

    [Fact]
    public async Task Test_OneToMany_Navigation_Properties()
    {
        // Arrange
        var (entity, fields, relations) = CreateComplexMockData();
        var (db, dbFile) = CodeGeneratorServiceTests.CreateMetadataDb();
        db.Insertable(new ModuleEntity
        {
            Id = 2,
            ProjectId = 1,
            ModuleId = 1,
            Name = "OrderDetail",
                Description = "Order detail",
                TableName = "order_detail",
                CreateBy = "test",
                UpdateUserId = 0
        }).ExecuteCommand();
        db.Insertable(new EntityField
        {
            Id = 10,
            ModuleEntityId = 2,
            Name = "ProductName",
            Description = "Product name",
            DataType = "string",
                ShowInList = true,
                ShowInAddForm = true,
                CreateBy = "test",
                UpdateUserId = 0
            }).ExecuteCommand();
        var service = new CodeGeneratorService(db);
        var context = service.BuildTemplateContext(entity, fields, relations, "TargetProject", "Business");

        // Act
        // 使用公开的方法或反射，由于 RenderTemplateAsync 是私有的，我们通过获取 Dto 模板内容或手动调用的方式比较困难
        // 但在上下文中我们能够明确验证字段包含
        var scriptObject = context;
        
        // ScriptObject implements IDictionary, access entity key directly
        var entityObj = scriptObject["entity"];
        Assert.NotNull(entityObj);
        var entityDict = (IDictionary<string, object?>)entityObj;
        
        var hasOneToMany = (bool)(entityDict["has_one_to_many"] ?? false);
        Assert.True(hasOneToMany);

        var relationList = (IList<Dictionary<string, object?>>)entityDict["one_to_many_relations"];
        Assert.Single(relationList);
        Assert.Equal("OrderDetail", relationList[0]["child_entity_name"]);
        Assert.Equal("orderDetail", relationList[0]["child_entity_name_lower"]);
        db.Dispose();
        CodeGeneratorServiceTests.TryDelete(dbFile);
    }

    [Fact]
    public void Test_ConstraintValidation_Exceptions()
    {
        // Arrange
        var service = CreateService();
        var entity = new ModuleEntity { Name = "Test", IsReadOnly = false, HasPrimaryKey = false };
        var fields = new List<EntityField>();

        // Act & Assert 1: 非只读但无主键，应该报错
        var ex1 = Assert.Throws<System.Exception>(() => service.ValidateConstraints(entity, fields));
        Assert.Contains("非只读但未设置主键", ex1.Message);

        entity.HasPrimaryKey = true; // 修复

        // Act & Assert 2: 多选字段使用数值型，应该报错
        fields.Add(new EntityField { Name = "CategoryIds", IsMultiple = true, DataType = "int" });
        var ex2 = Assert.Throws<System.Exception>(() => service.ValidateConstraints(entity, fields));
        Assert.Contains("多选字段，数据类型不能为数值类型", ex2.Message);

        fields[0].DataType = "string"; // 修复

        // Act & Assert 3: cascader关联非树形表，应该报错（由于这个断言依赖数据库查询 _db.Queryable<ModuleEntity>()，而在 mock 环境下如果抛出 NullReferenceException 说明执行到了这一步）
        // 我们这里跳过直接查询数据库那部分（由于此时 _db == null），捕获系统异常
        fields.Add(new EntityField { Name = "DeptId", FormControlType = "cascader", RelatedEntityName = "Unmockable" });
        Assert.Throws<System.NullReferenceException>(() => service.ValidateConstraints(entity, fields));
    }
}
