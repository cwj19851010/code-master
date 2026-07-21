using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Infrastructure.Persistence.Repositories;
using SqlSugar;

namespace CodeMaster.CodeGenerator.Tests.Services;

public class ProjectModuleServiceTests : IDisposable
{
    private readonly string _databasePath;
    private readonly ISqlSugarClient _db;

    public ProjectModuleServiceTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"codemaster_project_module_{Guid.NewGuid():N}.db");
        _db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"Data Source={_databasePath};Mode=ReadWriteCreate;",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        _db.CodeFirst.InitTables(typeof(Project), typeof(ProjectModule));
    }

    [Fact]
    public async Task GetPagedListAsync_FiltersByProjectAndModuleName()
    {
        _db.Insertable(new[]
        {
            CreateModule(1, 100, "Sales", 2),
            CreateModule(2, 100, "SalesReport", 1),
            CreateModule(3, 200, "Sales", 1)
        }).ExecuteCommand();

        var service = new ProjectModuleService(
            new Repository<ProjectModule>(_db),
            null!,
            new Repository<Project>(_db),
            _db);

        var result = await service.GetPagedListAsync(new ProjectModuleQueryDto
        {
            ProjectId = 100,
            ModuleName = "Sales",
            PageNum = 1,
            PageSize = 20
        });

        Assert.Equal(2, result.Total);
        Assert.All(result.Items, module => Assert.Equal(100, module.ProjectId));
        Assert.Equal(new[] { "SalesReport", "Sales" }, result.Items.Select(module => module.ModuleName));
    }

    public void Dispose()
    {
        _db.Ado.Close();
        _db.Dispose();
        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch
            {
                // Ignore cleanup failures caused by a delayed SQLite file handle release.
            }
        }
    }

    private static ProjectModule CreateModule(long id, long projectId, string name, int orderNum)
    {
        return new ProjectModule
        {
            Id = id,
            ProjectId = projectId,
            ModuleName = name,
            ModuleDescription = name,
            Icon = string.Empty,
            OrderNum = orderNum,
            RoutePath = string.Empty,
            Remark = string.Empty,
            CreateBy = "test",
            UpdateUserId = 0
        };
    }
}
