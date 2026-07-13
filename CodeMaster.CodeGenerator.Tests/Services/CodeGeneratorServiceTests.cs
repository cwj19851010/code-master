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
        db.CodeFirst.InitTables(typeof(ModuleEntity), typeof(EntityField), typeof(ProjectModule));
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
