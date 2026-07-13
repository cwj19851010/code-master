using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.Infrastructure.Persistence.Repositories;
using CodeMaster.Infrastructure.Persistence.SqlSugar;
using CodeMaster.Infrastructure.Services;
using CodeMaster.LocalAgent.Models;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace CodeMaster.LocalAgent.Services;

public class LocalMetadataStore
{
    private readonly LocalAgentOptions _options;

    public LocalMetadataStore(IOptions<LocalAgentOptions> options)
    {
        _options = options.Value;
    }

    public async Task<LocalMetadataContext> CreateAsync(GenerationBundleDto bundle, string? projectPathOverride = null)
    {
        var metadataRoot = GetMetadataRoot();
        Directory.CreateDirectory(metadataRoot);

        CleanupOldMetadataFiles(metadataRoot, bundle.Project.Id);

        var dbPath = Path.Combine(metadataRoot, $"{bundle.Project.Id}.{Guid.NewGuid():N}.metadata.db");
        var db = SqlSugarSetup.CreateSqlSugarClient($"Data Source={dbPath}", dbType: DbType.Sqlite);
        db.DbMaintenance.CreateDatabase();
        db.CodeFirst.InitTables(
            typeof(Project),
            typeof(ProjectModule),
            typeof(ModuleEntity),
            typeof(EntityField),
            typeof(OneToManyRelation),
            typeof(SysPageTemplate),
            typeof(SysFieldControlTemplate),
            typeof(SysChildTemplate));

        var project = bundle.Project;
        if (!string.IsNullOrWhiteSpace(projectPathOverride))
        {
            project.ProjectPath = projectPathOverride;
        }

        NormalizeAuditFields(project);
        NormalizeAuditFields(bundle.Modules);
        NormalizeAuditFields(bundle.Entities);
        NormalizeAuditFields(bundle.Fields);
        NormalizeAuditFields(bundle.Relations);
        NormalizeAuditFields(bundle.PageTemplates);
        NormalizeAuditFields(bundle.FieldControlTemplates);
        NormalizeAuditFields(bundle.ChildTemplates);

        await db.Insertable(project).ExecuteCommandAsync();
        if (bundle.Modules.Count > 0)
            await db.Insertable(bundle.Modules).ExecuteCommandAsync();
        if (bundle.Entities.Count > 0)
            await db.Insertable(bundle.Entities).ExecuteCommandAsync();
        if (bundle.Fields.Count > 0)
            await db.Insertable(bundle.Fields).ExecuteCommandAsync();
        if (bundle.Relations.Count > 0)
            await db.Insertable(bundle.Relations).ExecuteCommandAsync();
        if (bundle.PageTemplates.Count > 0)
            await db.Insertable(bundle.PageTemplates).ExecuteCommandAsync();
        if (bundle.FieldControlTemplates.Count > 0)
            await db.Insertable(bundle.FieldControlTemplates).ExecuteCommandAsync();
        if (bundle.ChildTemplates.Count > 0)
            await db.Insertable(bundle.ChildTemplates).ExecuteCommandAsync();

        return new LocalMetadataContext(db, dbPath);
    }

    public string GetMetadataRoot()
    {
        if (!string.IsNullOrWhiteSpace(_options.MetadataRoot))
            return _options.MetadataRoot;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
            localAppData = AppContext.BaseDirectory;

        return Path.Combine(localAppData, "CodeMaster", "LocalAgent");
    }

    public string GetTemplatesDirectory()
    {
        return Path.Combine(GetMetadataRoot(), "Templates");
    }

    private static void NormalizeAuditFields(IEnumerable<EntityBase> entities)
    {
        foreach (var entity in entities)
        {
            NormalizeAuditFields(entity);
        }
    }

    private static void NormalizeAuditFields(EntityBase entity)
    {
        entity.CreateBy ??= "LocalAgent";
        entity.UpdateUserId ??= 0;
        NormalizeStringProperties(entity);

        if (entity.CreateTime == default)
            entity.CreateTime = DateTime.UtcNow;
    }

    private static void NormalizeStringProperties(EntityBase entity)
    {
        foreach (var property in entity.GetType().GetProperties())
        {
            if (property.PropertyType != typeof(string) || !property.CanRead || !property.CanWrite)
                continue;

            if (property.GetValue(entity) == null)
                property.SetValue(entity, string.Empty);
        }
    }

    private static void CleanupOldMetadataFiles(string metadataRoot, long projectId)
    {
        foreach (var path in Directory.EnumerateFiles(metadataRoot, $"{projectId}*.metadata.db"))
        {
            TryDeleteMetadataFiles(path);
        }
    }

    internal static void TryDeleteMetadataFiles(string dbPath)
    {
        ClearSqlitePools();
        CollectGarbage();

        TryDeleteFile(dbPath);
        TryDeleteFile(dbPath + "-wal");
        TryDeleteFile(dbPath + "-shm");
    }

    private static void TryDeleteFile(string path)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                File.Delete(path);
                return;
            }
            catch when (attempt < 4)
            {
                ClearSqlitePools();
                CollectGarbage();
                Thread.Sleep(100);
            }
            catch
            {
                // Old local metadata files are best-effort cleanup only.
                return;
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

    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

public sealed class LocalMetadataContext : IDisposable
{
    private readonly ISqlSugarClient _db;
    private readonly string _dbPath;

    public LocalMetadataContext(ISqlSugarClient db, string dbPath)
    {
        _db = db;
        _dbPath = dbPath;
        var excelService = new ExcelService(_db);

        ModuleEntityService = new ModuleEntityService(
            new Repository<ModuleEntity>(_db),
            excelService,
            _db);

        ProjectModuleService = new ProjectModuleService(
            new Repository<ProjectModule>(_db),
            excelService,
            new Repository<Project>(_db),
            _db);
    }

    public ISqlSugarClient Db => _db;

    public ModuleEntityService ModuleEntityService { get; }

    public ProjectModuleService ProjectModuleService { get; }

    public void Dispose()
    {
        try
        {
            _db.Ado.Close();
        }
        catch
        {
        }

        if (_db is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch
            {
            }
        }

        LocalMetadataStore.TryDeleteMetadataFiles(_dbPath);
    }
}
