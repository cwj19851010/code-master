using System.Collections.Generic;
using System;
using System.IO;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.CodeGenerator.Tests.Fixtures;
using CodeMaster.Domain.Entities.CodeGen;
using SqlSugar;
using Xunit;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class CodeGeneratorServiceTests
{
    private readonly CodeGeneratorService _service;

    public CodeGeneratorServiceTests()
    {
        // Pass null for ISqlSugarClient since we are testing methods that don't hit the DB
        _service = new CodeGeneratorService(null!);
    }

    [Fact]
    public void CalculateInterfaceList_ShouldReturnCorrectInterfaces_ForFullEntity()
    {
        // Arrange
        var entity = MockEntityData.GetFullFeatureEntity();

        // Act
        var result = _service.CalculateInterfaceList(entity);

        // Assert
        Assert.Contains("IEntity", result);
        Assert.Contains("ITenant", result);
        Assert.Contains("IDept", result);
        Assert.Contains("IAuditEntity", result);
        Assert.Contains("ISoftDelete", result);
        Assert.DoesNotContain("ITree", result);
        Assert.Equal(", IEntity, ITenant, IDept, IAuditEntity, ISoftDelete", result);
    }

    [Fact]
    public void BuildTemplateContext_ShouldMapFieldsCorrectly()
    {
        // Arrange
        var entity = MockEntityData.GetFullFeatureEntity();
        var fields = MockEntityData.GetFullFeatureEntityFields();
        var relations = MockEntityData.GetEmptyRelations();

        var (db, dbFile) = CreateMetadataDb();
        try
        {
            SeedRelatedEntityMetadata(db);
            var service = new CodeGeneratorService(db);

            // Act
            var scriptObject = service.BuildTemplateContext(entity, fields, relations, "CodeMaster.Tests", "CodeGen");

            // Assert
            var hasEntity = scriptObject.Contains("entity");
            Assert.True(hasEntity);
        
            var entityDict = (IDictionary<string, object?>)scriptObject["entity"];
            Assert.Equal("employee", entityDict["name_lower"]);
            Assert.Equal("business_employee", entityDict["table_name"]);
            Assert.True((bool)entityDict["has_tenant"]);
            Assert.False((bool)entityDict["is_tree"]);

            // Check fields
            var fieldsList = (List<Dictionary<string, object?>>)entityDict["fields"];
            Assert.Equal(2, fieldsList.Count);
        
            var firstField = fieldsList[0];
            Assert.Equal("Name", firstField["name"]);
            Assert.Equal("input", firstField["form_control_type"]);
        
            var secondField = fieldsList[1];
            Assert.Equal("cascader", secondField["form_control_type"]);
            Assert.Equal("Department", secondField["related_entity_name"]);
            Assert.Equal("name", secondField["related_display_label"]); // Should be parsed to camelCase 'name'
        }
        finally
        {
            db.Dispose();
            TryDelete(dbFile);
        }
    }

    [Theory]
    [InlineData("Order", "orders")]
    [InlineData("OrderItem", "order_items")]
    [InlineData("Address", "address")]
    [InlineData("HTTPLog", "http_logs")]
    public void BuildTemplateContext_ShouldUseUnprefixedDefaultTableName(
        string entityName,
        string expectedTableName)
    {
        var entity = new ModuleEntity
        {
            Name = entityName,
            Description = entityName,
            TableName = null
        };

        var context = _service.BuildTemplateContext(
            entity,
            new List<EntityField>(),
            new List<OneToManyRelation>(),
            "Demo",
            "Sales");

        var entityContext = (IDictionary<string, object?>)context["entity"];
        Assert.Equal(expectedTableName, entityContext["table_name"]);
    }

    [Theory]
    [InlineData(true, false, "ReadOnlyApplicationService", "IReadOnlyApplicationService", true)]
    [InlineData(false, false, "QueryApplicationService", "IQueryApplicationService", false)]
    [InlineData(true, true, "ReadOnlyTreeApplicationService", "IReadOnlyTreeApplicationService", true)]
    public async Task ReadOnlyGeneration_ShouldFollowPrimaryKeyAndTreeCapabilities(
        bool hasPrimaryKey,
        bool isTree,
        string expectedServiceBase,
        string expectedServiceContract,
        bool expectsGetById)
    {
        var (db, dbFile) = CreateMetadataDb();
        try
        {
            const long entityId = 100;
            db.Insertable(new ModuleEntity
            {
                Id = entityId,
                ProjectId = 1,
                ModuleId = 1,
                Name = "ReadonlyReport",
                Description = "Readonly report",
                TableName = "readonly_report",
                HasPrimaryKey = hasPrimaryKey,
                IsTree = isTree,
                IsReadOnly = true,
                HasAudit = false,
                HasSoftDelete = false,
                GenerateFrontend = true,
                CreateBy = "test",
                UpdateUserId = 0
            }).ExecuteCommand();

            var fields = new List<EntityField>();
            if (hasPrimaryKey)
            {
                fields.Add(new EntityField
                {
                    Id = 101,
                    ModuleEntityId = entityId,
                    Name = "Id",
                    Description = "Primary key",
                    DataType = "long",
                    IsPrimaryKey = true,
                    IsRequired = true,
                    IsSystemField = true,
                    CreateBy = "test",
                    UpdateUserId = 0
                });
            }
            if (isTree)
            {
                fields.Add(new EntityField { Id = 102, ModuleEntityId = entityId, Name = "ParentId", Description = "Parent", DataType = "long?", IsNullable = true, IsSystemField = true, CreateBy = "test", UpdateUserId = 0 });
                fields.Add(new EntityField { Id = 103, ModuleEntityId = entityId, Name = "Ancestors", Description = "Ancestors", DataType = "string?", IsNullable = true, IsSystemField = true, CreateBy = "test", UpdateUserId = 0 });
            }
            fields.Add(new EntityField
            {
                Id = 104,
                ModuleEntityId = entityId,
                Name = "Name",
                Description = "Name",
                DataType = "string",
                ShowInList = true,
                ShowInSearch = true,
                ShowInDetail = true,
                CreateBy = "test",
                UpdateUserId = 0
            });
            db.Insertable(fields).ExecuteCommand();

            var generator = new CodeGeneratorService(db);
            var entityCode = await generator.GenerateEntityAutoAsync(entityId, "Demo", "Reports");
            var dtoCode = await generator.GenerateDtoAsync(entityId, "Demo", "Reports");
            var serviceCode = await generator.GenerateServiceImplementationAsync(entityId, "Demo", "Reports");
            var serviceInterfaceCode = await generator.GenerateServiceInterfaceAsync(entityId, "Demo", "Reports");
            var apiCode = await generator.GenerateFrontendApiAsync(entityId, "Demo", "Reports");

            Assert.Contains($"public class ReadonlyReportService : {expectedServiceBase}<", serviceCode);
            Assert.Contains(expectedServiceContract, serviceInterfaceCode);
            Assert.DoesNotContain("CreateReadonlyReportDto", dtoCode);
            Assert.DoesNotContain("UpdateReadonlyReportDto", dtoCode);
            Assert.Contains("[SugarTable(\"readonly_report\")]", entityCode);
            Assert.Contains("public partial class ReadonlyReport : IBaseEntity", entityCode);

            if (hasPrimaryKey)
            {
                Assert.Contains("IEntity", entityCode);
                Assert.Contains("public long Id", entityCode);
            }
            else
            {
                Assert.DoesNotContain("IEntity", entityCode);
                Assert.DoesNotContain("public long Id", entityCode);
            }

            if (expectsGetById)
                Assert.Contains("export function getById", apiCode);
            else
                Assert.DoesNotContain("export function getById", apiCode);

            Assert.DoesNotContain("export function create", apiCode);
            Assert.DoesNotContain("export function update", apiCode);
            Assert.DoesNotContain("export function deleteById", apiCode);
        }
        finally
        {
            db.Dispose();
            TryDelete(dbFile);
        }
    }

    [Fact]
    public async Task EntityGeneration_ShouldComposeCapabilityInterfacesAndPhysicalSystemFields()
    {
        var (db, dbFile) = CreateMetadataDb();
        try
        {
            const long entityId = 200;
            db.Insertable(new ModuleEntity
            {
                Id = entityId,
                ProjectId = 1,
                ModuleId = 1,
                Name = "AuditedRecord",
                Description = "Audited record",
                HasPrimaryKey = true,
                HasTenant = true,
                HasDataPermission = true,
                HasAudit = true,
                HasSoftDelete = true,
                CreateBy = "test",
                UpdateUserId = 0
            }).ExecuteCommand();
            db.Insertable(new[]
            {
                SystemField(201, entityId, "Id", "long", isPrimaryKey: true),
                SystemField(202, entityId, "TenantId", "long"),
                SystemField(203, entityId, "DeptId", "long?", isNullable: true),
                SystemField(204, entityId, "DeptAncestors", "string?", isNullable: true),
                SystemField(205, entityId, "CreateUserId", "long?", isNullable: true),
                SystemField(206, entityId, "CreateBy", "string?", isNullable: true),
                SystemField(207, entityId, "CreateTime", "DateTime", defaultValue: "DateTime.UtcNow"),
                SystemField(208, entityId, "UpdateUserId", "long?", isNullable: true),
                SystemField(209, entityId, "UpdateBy", "string?", isNullable: true),
                SystemField(210, entityId, "UpdateTime", "DateTime?", isNullable: true),
                SystemField(211, entityId, "IsDeleted", "bool"),
                SystemField(212, entityId, "DeleteTime", "DateTime?", isNullable: true),
                SystemField(213, entityId, "DeleteBy", "string?", isNullable: true),
                SystemField(214, entityId, "DeleteUserId", "long?", isNullable: true)
            }).ExecuteCommand();

            var code = await new CodeGeneratorService(db).GenerateEntityAutoAsync(entityId, "Demo", "Records");

            Assert.Contains("[SugarTable(\"audited_records\")]", code);
            Assert.Contains("public partial class AuditedRecord : IBaseEntity, IEntity, ITenant, IDept, IAuditEntity, ISoftDelete", code);
            Assert.Contains("public long Id", code);
            Assert.Contains("public DateTime CreateTime { get; set; } = DateTime.UtcNow;", code);
            Assert.Contains("public bool IsDeleted", code);
            Assert.DoesNotContain("EntityBase", code);
        }
        finally
        {
            db.Dispose();
            TryDelete(dbFile);
        }
    }

    [Fact]
    public async Task EntityGeneration_ShouldApplyFieldStorageMetadata()
    {
        var (db, dbFile) = CreateMetadataDb();
        try
        {
            const long entityId = 220;
            db.Insertable(new ModuleEntity
            {
                Id = entityId,
                ProjectId = 1,
                ModuleId = 1,
                Name = "StorageMetadata",
                Description = "Storage metadata",
                HasPrimaryKey = true,
                CreateBy = "test",
                UpdateUserId = 0
            }).ExecuteCommand();
            db.Insertable(new[]
            {
                SystemField(221, entityId, "Id", "long", isPrimaryKey: true),
                new EntityField
                {
                    Id = 222,
                    ModuleEntityId = entityId,
                    Name = "Code",
                    Description = "Code",
                    DataType = "string",
                    MaxLength = 64,
                    IsRequired = true,
                    DefaultValue = "   ",
                    CreateBy = "test",
                    UpdateUserId = 0
                },
                new EntityField
                {
                    Id = 223,
                    ModuleEntityId = entityId,
                    Name = "Amount",
                    Description = "Amount",
                    DataType = "decimal",
                    Precision = 18,
                    Scale = 2,
                    CreateBy = "test",
                    UpdateUserId = 0
                },
                new EntityField
                {
                    Id = 224,
                    ModuleEntityId = entityId,
                    Name = "DisplayText",
                    Description = "Display text",
                    DataType = "string",
                    IsIgnore = true,
                    DefaultValue = "planned",
                    CreateBy = "test",
                    UpdateUserId = 0
                }
            }).ExecuteCommand();

            var code = await new CodeGeneratorService(db).GenerateEntityAutoAsync(entityId, "Demo", "Records");

            Assert.Contains("[SugarColumn(ColumnName = \"code\", Length = 64)]", code);
            Assert.Contains("public string Code { get; set; } = default!;", code);
            Assert.DoesNotContain("= ;", code);
            Assert.Contains("public string DisplayText { get; set; } = @\"planned\";", code);
            Assert.Contains("[SugarColumn(ColumnName = \"amount\", Length = 18, DecimalDigits = 2)]", code);
            Assert.Contains("[SugarColumn(IsIgnore = true)]", code);
        }
        finally
        {
            db.Dispose();
            TryDelete(dbFile);
        }
    }

    [Fact]
    public void StaticChoiceControls_ShouldNormalizeOptionsAndUseChoiceDetailTemplate()
    {
        var optionsMethod = typeof(TemplateCodeGenerator).GetMethod(
            "BuildStaticChoiceOptionsLiteral",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var mapMethod = typeof(TemplateCodeGenerator).GetMethod(
            "MapControlType",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(optionsMethod);
        Assert.NotNull(mapMethod);
        var options = (string)optionsMethod!.Invoke(null, new object?[] { "Pending,Paid" })!;
        var detailControl = (string)mapMethod!.Invoke(null, new object[] { "radio-group", "detail", "string" })!;

        Assert.Contains("\"label\":\"Pending\"", options);
        Assert.Contains("\"value\":\"Paid\"", options);
        Assert.Equal("select", detailControl);
    }

    private static EntityField SystemField(
        long id,
        long entityId,
        string name,
        string dataType,
        bool isPrimaryKey = false,
        bool isNullable = false,
        string? defaultValue = null) => new()
    {
        Id = id,
        ModuleEntityId = entityId,
        Name = name,
        Description = name,
        DataType = dataType,
        IsPrimaryKey = isPrimaryKey,
        IsRequired = isPrimaryKey,
        IsNullable = isNullable,
        IsSystemField = true,
        DefaultValue = defaultValue,
        CreateBy = "test",
        UpdateUserId = 0
    };

    internal static (ISqlSugarClient Db, string DbFile) CreateMetadataDb()
    {
        var dbFile = Path.Combine(Path.GetTempPath(), "CodeMaster_CodeGenTest_" + Guid.NewGuid().ToString("N") + ".db");
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"Data Source={dbFile};Mode=ReadWriteCreate;",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
        db.CodeFirst.InitTables(
            typeof(ModuleEntity),
            typeof(EntityField),
            typeof(ProjectModule),
            typeof(OneToManyRelation),
            typeof(EntityRelation));
        return (db, dbFile);
    }

    internal static void SeedRelatedEntityMetadata(ISqlSugarClient db)
    {
        db.Insertable(new ProjectModule
        {
            Id = 2,
            ProjectId = 1,
            ModuleName = "Organization",
            ModuleDescription = "Organization",
            Icon = "folder",
            RoutePath = "/organization",
            LastSyncTime = DateTime.UtcNow,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();

        db.Insertable(new ModuleEntity
        {
            Id = 2,
            ProjectId = 1,
            ModuleId = 2,
            Name = "Department",
            Description = "Department",
            TableName = "sys_department",
            CreateBy = "test",
            UpdateUserId = 0
        }).ExecuteCommand();
    }

    internal static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup for Windows file handles released after test process exits.
        }
    }
}
