using System.Data;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Core.Enums;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.LocalAgent.Models;
using CodeMaster.LocalAgent.Services;
using Microsoft.Extensions.Options;

namespace CodeMaster.LocalAgent.Tests.Services;

public class LocalMetadataStoreTests
{
    [Fact]
    public async Task CreateAsync_creates_sqlite_metadata_database_and_normalizes_null_strings()
    {
        var metadataRoot = Path.Combine(Path.GetTempPath(), "CodeMaster.LocalAgent.Tests", Guid.NewGuid().ToString("N"));
        var projectPathOverride = Path.Combine(metadataRoot, "GeneratedProjects");
        Directory.CreateDirectory(metadataRoot);

        try
        {
            var store = new LocalMetadataStore(Options.Create(new LocalAgentOptions
            {
                MetadataRoot = metadataRoot
            }));

            var bundle = CreateBundle();
            string dbPath;

            using (var context = await store.CreateAsync(bundle, projectPathOverride))
            {
                dbPath = Directory.EnumerateFiles(metadataRoot, $"{bundle.Project.Id}*.metadata.db").Single();
                Assert.True(File.Exists(dbPath));

                var project = await context.Db.Queryable<Project>().FirstAsync(p => p.Id == bundle.Project.Id);
                Assert.Equal(projectPathOverride, project.ProjectPath);
                Assert.Equal(string.Empty, project.DisplayNameEn);
                Assert.Equal("LocalAgent", project.CreateBy);
                Assert.NotEqual(default, project.CreateTime);

                var module = await context.Db.Queryable<ProjectModule>().FirstAsync(m => m.Id == 2001);
                Assert.Equal(string.Empty, module.Icon);
                Assert.Equal(string.Empty, module.RoutePath);
                Assert.Equal("LocalAgent", module.CreateBy);

                var field = await context.Db.Queryable<EntityField>().FirstAsync(f => f.Id == 4001);
                Assert.True(field.IsMultiple);
                Assert.Equal(string.Empty, field.RelatedEntityName);
                Assert.Equal("LocalAgent", field.CreateBy);

                var columnTypes = ReadSqliteColumnTypes(context, "sys_entity_field");
                Assert.Equal("integer", columnTypes["is_multiple"]);
                Assert.Equal("integer", columnTypes["show_in_list"]);
                Assert.StartsWith("text", columnTypes["name"]);
            }

            Assert.False(File.Exists(dbPath));
            Assert.False(File.Exists(dbPath + "-wal"));
            Assert.False(File.Exists(dbPath + "-shm"));
        }
        finally
        {
            DeleteDirectoryWithRetries(metadataRoot);
        }
    }

    private static GenerationBundleDto CreateBundle()
    {
        return new GenerationBundleDto
        {
            Project = new Project
            {
                Id = 1001,
                ProjectName = "OrderManager",
                DisplayName = "Order Manager",
                DisplayNameEn = null,
                Description = null,
                DatabaseType = DatabaseType.SQLite,
                ConnectionString = "Data Source=OrderManager.db",
                ProjectPath = "D:\\ServerPath",
                LogoPath = null,
                CreateBy = null,
                CreateTime = default
            },
            Modules =
            {
                new ProjectModule
                {
                    Id = 2001,
                    ProjectId = 1001,
                    ModuleName = "Sales",
                    ModuleDescription = "Sales",
                    Icon = null,
                    RoutePath = null,
                    CreateBy = null,
                    CreateTime = default
                }
            },
            Entities =
            {
                new ModuleEntity
                {
                    Id = 3001,
                    ProjectId = 1001,
                    ModuleId = 2001,
                    Name = "Order",
                    Description = "Order",
                    TableName = null,
                    GenerateFrontend = true,
                    CreateBy = null,
                    CreateTime = default
                }
            },
            Fields =
            {
                new EntityField
                {
                    Id = 4001,
                    ModuleEntityId = 3001,
                    Name = "CustomerIds",
                    Description = "Customers",
                    DataType = "string",
                    FormControlType = "select-table",
                    SelectDataSource = null,
                    SelectOptions = null,
                    IsMultiple = true,
                    RelatedEntityName = null,
                    RelatedEntityIdField = null,
                    RelatedEntityDisplayFields = null,
                    ShowInList = true,
                    ShowInDetail = true,
                    ShowInAddForm = true,
                    ShowInEditForm = true,
                    FieldCategory = "Normal",
                    CreateBy = null,
                    CreateTime = default
                }
            },
            Relations =
            {
                new OneToManyRelation
                {
                    Id = 5001,
                    ModuleEntityId = 3001,
                    MasterField = "Id",
                    ChildEntityId = 3002,
                    ChildEntityName = "OrderItem",
                    ChildForeignKey = "OrderId",
                    CreateBy = null,
                    CreateTime = default
                }
            },
            PageTemplates =
            {
                new SysPageTemplate
                {
                    Id = 6001,
                    PageType = "index",
                    Name = "Index",
                    HtmlContent = "<template />",
                    ScriptSections = "{}",
                    CreateBy = null,
                    CreateTime = default
                }
            },
            FieldControlTemplates =
            {
                new SysFieldControlTemplate
                {
                    Id = 7001,
                    ControlType = "input",
                    PageSection = "list",
                    HtmlContent = "<span />",
                    ScriptSections = "{}",
                    CreateBy = null,
                    CreateTime = default
                }
            },
            ChildTemplates =
            {
                new SysChildTemplate
                {
                    Id = 8001,
                    PageType = "add",
                    ChildType = "card",
                    HtmlContent = "<el-table />",
                    ScriptSections = "{}",
                    CreateBy = null,
                    CreateTime = default
                }
            }
        };
    }

    private static Dictionary<string, string> ReadSqliteColumnTypes(LocalMetadataContext context, string tableName)
    {
        var table = context.Db.Ado.GetDataTable($"PRAGMA table_info('{tableName}')");

        return table.Rows.Cast<DataRow>()
            .ToDictionary(
                row => Convert.ToString(row["name"]) ?? string.Empty,
                row => (Convert.ToString(row["type"]) ?? string.Empty).ToLowerInvariant());
    }

    private static void DeleteDirectoryWithRetries(string path)
    {
        if (!Directory.Exists(path))
            return;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                ClearSqlitePools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                Thread.Sleep(100);
            }
        }
    }

    private static void ClearSqlitePools()
    {
        var sqliteConnectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
        sqliteConnectionType
            ?.GetMethod("ClearAllPools", Type.EmptyTypes)
            ?.Invoke(null, null);
    }
}
