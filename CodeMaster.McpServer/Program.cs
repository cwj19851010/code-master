using CodeMaster.Application.Services;
using CodeMaster.Core.Data;
using CodeMaster.Core.MultiTenancy;
using CodeMaster.Core.Repositories;
using CodeMaster.Infrastructure.Caching.Extensions;
using CodeMaster.Infrastructure.DynamicApi;
using CodeMaster.Infrastructure.MultiTenancy;
using CodeMaster.Infrastructure.Persistence.SqlSugar;
using CodeMaster.McpServer;
using CodeMaster.McpServer.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Yitter.IdGenerator;

// ─── Configuration ───
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var protocolOutput = new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false))
{
    AutoFlush = true
};
Console.SetOut(Console.Error);

// 雪花ID
var workerId = config.GetValue<ushort>("SnowflakeId:WorkerId", (ushort)1);
YitIdHelper.SetIdGenerator(new IdGeneratorOptions { WorkerId = workerId });

// ─── DI Setup ───
var services = new ServiceCollection();

services.AddSingleton<IConfiguration>(config);
services.AddHttpContextAccessor();
services.AddSingleton<IDataFilter, DataFilter>();
services.AddScoped<IDataPermissionContext, DataPermissionContext>();
services.AddScoped<ITenantContext, TenantContext>();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

// SqlSugar
var connStr = config.GetConnectionString("DefaultConnection")
    ?? "Data Source=CodeMaster_Test.db";
var dbProvider = config["DbProvider"] ?? "Sqlite";
var dbType = dbProvider switch
{
    "Sqlite" => DbType.Sqlite,
    "MySql" => DbType.MySql,
    "Oracle" => DbType.Oracle,
    "PostgreSQL" => DbType.PostgreSQL,
    _ => DbType.SqlServer
};

services.AddScoped<ISqlSugarClient>(sp =>
{
    var tenantContext = sp.GetService<ITenantContext>();
    var dataFilter = sp.GetRequiredService<IDataFilter>();
    var dataPermissionContext = sp.GetService<IDataPermissionContext>();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SqlSugar");
    return SqlSugarSetup.CreateSqlSugarClient(connStr, tenantContext, dataFilter, dataPermissionContext, dbType, logger);
});

// 仓储
services.AddRepositories();

// Application Services
var appAssembly = System.Reflection.Assembly.Load("CodeMaster.Application");
services.AddApplicationServices(appAssembly);

// 注册 Excel 服务
services.AddScoped<CodeMaster.Core.Services.IExcelService, CodeMaster.Infrastructure.Services.ExcelService>();

// 缓存
services.AddCaching(config);

// Tools
services.AddScoped<EntityTool>();
services.AddScoped<ProjectTool>();
services.AddScoped<ModuleTool>();
services.AddScoped<ProjectOperationTool>();
services.AddScoped<ProjectStructureTool>();
services.AddScoped<CodeGenTool>();
services.AddScoped<QueryTool>();
services.AddScoped<AnalyzeTool>();

var sp = services.BuildServiceProvider();

// ─── MCP Protocol ───
var protocol = new McpProtocol("codemaster-mcp", "1.0.0", protocolOutput);

protocol.RegisterTool(new McpTool
{
    Name = QueryTool.Definition.Name,
    Description = QueryTool.Definition.Description,
    InputSchema = QueryTool.Definition.InputSchema,
    InputType = QueryTool.Definition.InputType,
    Handler = async (args) =>
    {
        using var scope = sp.CreateScope();
        var tool = scope.ServiceProvider.GetRequiredService<QueryTool>();
        return await tool.HandleAsync(args);
    }
});

protocol.RegisterTool(new McpTool
{
    Name = AnalyzeTool.Definition.Name,
    Description = AnalyzeTool.Definition.Description,
    InputSchema = AnalyzeTool.Definition.InputSchema,
    InputType = AnalyzeTool.Definition.InputType,
    Handler = async (args) =>
    {
        using var scope = sp.CreateScope();
        var tool = scope.ServiceProvider.GetRequiredService<AnalyzeTool>();
        return await tool.HandleAsync(args);
    }
});

protocol.RegisterTool(new McpTool
{
    Name = ProjectTool.Definition.Name,
    Description = ProjectTool.Definition.Description,
    InputSchema = ProjectTool.Definition.InputSchema,
    InputType = ProjectTool.Definition.InputType,
    Handler = async (args) =>
    {
        using var scope = sp.CreateScope();
        var tool = scope.ServiceProvider.GetRequiredService<ProjectTool>();
        return await tool.HandleAsync(args);
    }
});

protocol.RegisterTool(new McpTool
{
    Name = ModuleTool.Definition.Name,
    Description = ModuleTool.Definition.Description,
    InputSchema = ModuleTool.Definition.InputSchema,
    InputType = ModuleTool.Definition.InputType,
    Handler = async (args) =>
    {
        using var scope = sp.CreateScope();
        var tool = scope.ServiceProvider.GetRequiredService<ModuleTool>();
        return await tool.HandleAsync(args);
    }
});

protocol.RegisterTool(new McpTool
{
    Name = EntityTool.Definition.Name,
    Description = EntityTool.Definition.Description,
    InputSchema = EntityTool.Definition.InputSchema,
    InputType = EntityTool.Definition.InputType,
    Handler = async (args) =>
    {
        using var scope = sp.CreateScope();
        var tool = scope.ServiceProvider.GetRequiredService<EntityTool>();
        return await tool.HandleAsync(args);
    }
});

protocol.RegisterTool(new McpTool
{
    Name = ProjectOperationTool.Definition.Name,
    Description = ProjectOperationTool.Definition.Description,
    InputSchema = ProjectOperationTool.Definition.InputSchema,
    InputType = ProjectOperationTool.Definition.InputType,
    Handler = async (args) =>
    {
        using var scope = sp.CreateScope();
        var tool = scope.ServiceProvider.GetRequiredService<ProjectOperationTool>();
        return await tool.HandleAsync(args);
    }
});

protocol.RegisterTool(new McpTool
{
    Name = CodeGenTool.Definition.Name,
    Description = CodeGenTool.Definition.Description,
    InputSchema = CodeGenTool.Definition.InputSchema,
    InputType = CodeGenTool.Definition.InputType,
    Handler = async (args) =>
    {
        using var scope = sp.CreateScope();
        var tool = scope.ServiceProvider.GetRequiredService<CodeGenTool>();
        return await tool.HandleAsync(args);
    }
});

protocol.RegisterTool(new McpTool
{
    Name = ProjectStructureTool.Definition.Name,
    Description = ProjectStructureTool.Definition.Description,
    InputSchema = ProjectStructureTool.Definition.InputSchema,
    InputType = ProjectStructureTool.Definition.InputType,
    Handler = async (args) =>
    {
        using var scope = sp.CreateScope();
        var tool = scope.ServiceProvider.GetRequiredService<ProjectStructureTool>();
        return await tool.HandleAsync(args);
    }
});

// ─── Run ───
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.Error.WriteLine($"[CodeMaster.McpServer] Starting with {dbProvider} database...");
Console.Error.WriteLine("[CodeMaster.McpServer] Tools: query_project, analyze_requirements, save_project, save_module, create_or_update_entity, run_project_operation, generate_code, get_project_structure");
Console.Error.WriteLine("[CodeMaster.McpServer] Operations include initialize, initialize_step, start/stop frontend/backend/all, status, migrate_database, and build.");
Console.Error.WriteLine("[CodeMaster.McpServer] Ready.");

try
{
    await protocol.RunAsync(cts.Token);
}
catch (OperationCanceledException) { }
finally
{
    Console.Error.WriteLine("[CodeMaster.McpServer] Shut down.");
}
