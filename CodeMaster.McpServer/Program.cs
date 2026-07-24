using CodeMaster.LocalAgent.Models;
using CodeMaster.LocalAgent.Services;
using CodeMaster.McpServer.Services;
using CodeMaster.McpServer.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yitter.IdGenerator;

// MCP stdio reserves stdout for JSON-RPC frames. Route library Console.WriteLine output to stderr.
Console.SetOut(Console.Error);

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables();

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

var workerId = builder.Configuration.GetValue<ushort>("SnowflakeId:WorkerId", (ushort)1);
YitIdHelper.SetIdGenerator(new IdGeneratorOptions { WorkerId = workerId });

builder.Services.AddCodeMasterLocalExecution(builder.Configuration);
builder.Services.AddHttpClient<CodeMasterApiClient>();
builder.Services.AddSingleton<McpAuthStore>();
builder.Services.AddSingleton<ProjectContextResolver>();
builder.Services.AddSingleton<McpSessionResolver>();

builder.Services.AddTransient<AuthTool>();
builder.Services.AddTransient<QueryTool>();
builder.Services.AddTransient<AnalyzeTool>();
builder.Services.AddTransient<ProjectTool>();
builder.Services.AddTransient<ModuleTool>();
builder.Services.AddTransient<EntityTool>();
builder.Services.AddTransient<ProjectOperationTool>();
builder.Services.AddTransient<CodeGenTool>();
builder.Services.AddTransient<ProjectStructureTool>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "codemaster-mcp", Version = "2.0.0" };
    })
    .WithStdioServerTransport()
    .WithTools<CodeMasterMcpTools>();

await builder.Build().RunAsync();
