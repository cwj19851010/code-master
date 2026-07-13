using System.Reflection;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Migrator.Persistence.EfCore;
using CodeMaster.Migrator.SeedData;
using CodeMaster.Migrator.SeedData.Core;
using CodeMaster.Migrator.SeedData.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if DB_MYSQL
using MySqlConnector;
#endif
using Yitter.IdGenerator;

// 检查是否是 EF 设计时工具调用
var cmdArgs = Environment.GetCommandLineArgs();
if (cmdArgs.Any(a => a.Contains("ef") || a.Contains("migrations")))
{
    // 设计时，不执行迁移逻辑
    return 0;
}

Console.WriteLine("===========================================");
Console.WriteLine("  CodeMaster 数据库迁移和种子数据初始化");
Console.WriteLine("===========================================\n");

// 1. 配置
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("未找到数据库连接字符串");

var dbProvider = configuration["DbProvider"] ?? "SqlServer";
Console.WriteLine($"数据库提供程序: {dbProvider}");
Console.WriteLine($"连接字符串: {connectionString}\n");

#if DB_MYSQL
await EnsureMySqlDatabaseExistsAsync(connectionString);
#endif

// 2. 配置雪花ID生成器
var workerId = configuration.GetValue<ushort>("SnowflakeId:WorkerId", (ushort)1);
YitIdHelper.SetIdGenerator(new IdGeneratorOptions { WorkerId = workerId });

// 3. 配置 DbContext
var services = new ServiceCollection();
services.AddDbContext<CodeMasterDbContext>(opts =>
{
#if DB_MYSQL
    opts.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
#elif DB_PGSQL
    opts.UseNpgsql(connectionString);
#elif DB_SQLITE
    opts.UseSqlite(connectionString);
#elif DB_ORACLE
    opts.UseOracle(connectionString);
#else
    opts.UseSqlServer(connectionString);
#endif
});

var serviceProvider = services.BuildServiceProvider();
using var scope = serviceProvider.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<CodeMasterDbContext>();

try
{
    // 4. 执行迁移
    Console.WriteLine("【步骤 1】执行数据库迁移...");
#if DB_MYSQL
    if (await IsEmptyMySqlDatabaseAsync(dbContext))
    {
        await dbContext.Database.EnsureCreatedAsync();
        await MarkAllMigrationsAppliedAsync(dbContext);
    }
    else
    {
        await dbContext.Database.MigrateAsync();
    }
#else
    await dbContext.Database.MigrateAsync();
#endif
    Console.WriteLine("✓ 数据库迁移完成\n");

    // 5. 初始化种子数据
    Console.WriteLine("【步骤 2】初始化种子数据...\n");
    await SeedDataAsync(dbContext);
    Console.WriteLine("\n✓ 种子数据初始化完成\n");

    Console.WriteLine("===========================================");
    Console.WriteLine("  ✓ 所有操作完成！");
    Console.WriteLine("===========================================");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ 错误: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"内部异常: {ex.InnerException.Message}");
        if (ex.InnerException.InnerException != null)
        {
            Console.WriteLine($"更深层异常: {ex.InnerException.InnerException.Message}");
        }
    }
    Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
    return 1;
}

return 0;

#if DB_MYSQL
static async Task EnsureMySqlDatabaseExistsAsync(string connectionString)
{
    var builder = new MySqlConnectionStringBuilder(connectionString);
    var databaseName = builder.Database;
    if (string.IsNullOrWhiteSpace(databaseName))
    {
        return;
    }

    builder.Database = "";

    await using var connection = new MySqlConnection(builder.ConnectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{EscapeMySqlIdentifier(databaseName)}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
    await command.ExecuteNonQueryAsync();
}

static string EscapeMySqlIdentifier(string value)
{
    return value.Replace("`", "``");
}

static async Task<bool> IsEmptyMySqlDatabaseAsync(CodeMasterDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State == System.Data.ConnectionState.Closed;
    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_type = 'BASE TABLE'
              AND table_name <> '__EFMigrationsHistory'
            """;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) == 0;
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static async Task MarkAllMigrationsAppliedAsync(CodeMasterDbContext dbContext)
{
    var migrations = dbContext.Database.GetMigrations().ToList();
    if (migrations.Count == 0)
    {
        return;
    }

    var productVersion = typeof(Migration).Assembly
        .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?
        .InformationalVersion
        ?? typeof(Migration).Assembly.GetName().Version?.ToString()
        ?? "9.0.0";

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
            `MigrationId` varchar(150) NOT NULL,
            `ProductVersion` varchar(32) NOT NULL,
            CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
        );
        """);

    foreach (var migration in migrations)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ({migration}, {productVersion});
            """);
    }
}
#endif

/// <summary>
/// 种子数据初始化主流程
/// </summary>
static async Task SeedDataAsync(CodeMasterDbContext dbContext)
{
    // 1. 初始化基础数据（租户、部门、职位、角色、用户）
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("【阶段 1】初始化基础数据");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    var baseDataModule = new BaseDataModule();
    await baseDataModule.AddInitialDataAsync(dbContext);
    await dbContext.SaveChangesAsync();

    var adminRole = await dbContext.Set<SysRole>()
        .FirstOrDefaultAsync(r => r.RoleKey == "admin")
        ?? throw new Exception("未找到管理员角色");
    var adminRoleId = adminRole.Id;

    // 2. 初始化语言数据
    Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("【阶段 2】初始化语言数据");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    var langModule = new LangModule();
    await langModule.AddInitialDataAsync(dbContext);
    await dbContext.SaveChangesAsync();

    // 3. 添加全局翻译
    Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("【阶段 3】添加全局翻译");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    var globalTranslations = GlobalTranslations.GetTranslations();
    await MenuBuilder.AddTranslationsAsync(dbContext, globalTranslations, "global");
    await dbContext.SaveChangesAsync();

    // 4. 创建顶级目录菜单（自动扫描）
    Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("【阶段 4】创建顶级目录菜单");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    await CreateTopLevelDirectoriesAsync(dbContext, adminRoleId);
    await dbContext.SaveChangesAsync();

    // 5. 动态加载并初始化所有模块（自动扫描）
    Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("【阶段 5】初始化模块菜单和翻译");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

    var modules = LoadAllModules();
    Console.WriteLine($"从程序集加载了 {modules.Count} 个模块\n");

    // 6. 逐个初始化模块
    foreach (var module in modules)
    {
        Console.WriteLine($"\n▶ {module.ModuleName}");

        // 6.1 检查菜单是否已存在
        var hasMenu = await module.HasMenuAsync(dbContext);
        if (!hasMenu)
        {
            // 6.2 添加菜单
            await module.AddMenusAsync(dbContext, adminRoleId);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            Console.WriteLine("  - 菜单已存在，跳过");
        }

        // 6.3 添加初始数据
        await module.AddInitialDataAsync(dbContext);
        await dbContext.SaveChangesAsync();

        // 6.4 添加翻译
        var translations = module.GetTranslations();
        if (translations.Count > 0)
        {
            await MenuBuilder.AddTranslationsAsync(dbContext, translations, module.ModuleName.ToLower());
            await dbContext.SaveChangesAsync();
        }
    }

    Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("【完成】所有模块初始化完成");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
}

/// <summary>
/// 自动扫描并加载所有模块
/// </summary>
static List<ISeedModule> LoadAllModules()
{
    var modules = new List<ISeedModule>();
    var seedModuleType = typeof(ISeedModule);

    var allModuleTypes = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && seedModuleType.IsAssignableFrom(t))
        .ToList();

    Console.WriteLine($"  发现 {allModuleTypes.Count} 个模块类");

    foreach (var type in allModuleTypes)
    {
        try
        {
            var instance = Activator.CreateInstance(type) as ISeedModule;
            if (instance != null)
            {
                modules.Add(instance);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ 加载模块失败 {type.Name}: {ex.Message}");
        }
    }

    return modules;
}

/// <summary>
/// 自动扫描并创建顶级目录菜单
/// </summary>
static async Task CreateTopLevelDirectoriesAsync(CodeMasterDbContext dbContext, long adminRoleId)
{
    Console.WriteLine("\n▶ 创建顶级目录菜单");

    var directoryType = typeof(ITopLevelDirectory);
    var directories = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && directoryType.IsAssignableFrom(t))
        .Select(t => Activator.CreateInstance(t) as ITopLevelDirectory)
        .Where(d => d != null)
        .OrderBy(d => d!.Order)
        .ToList();

    Console.WriteLine($"  发现 {directories.Count} 个顶级目录");

    foreach (var dir in directories)
    {
        var exists = await dbContext.Set<SysMenu>()
            .AnyAsync(m => m.Path == dir.Path && m.MenuType == "M");

        if (!exists)
        {
            var menuId = await MenuBuilder.CreateDirectoryAsync(
                dbContext,
                dir.Name,
                dir.TitleKey,
                dir.Path,
                dir.Icon,
                dir.Order
            );
            await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, new List<long> { menuId });
            Console.WriteLine($"  - 创建目录: {dir.Name}");
        }
        else
        {
            Console.WriteLine($"  - 目录已存在: {dir.Name}");
        }
    }
}
