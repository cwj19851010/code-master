using CodeMaster.LocalAgent.Models;
using CodeMaster.LocalAgent.Services;

var builder = WebApplication.CreateBuilder(args);

var configuredOptions = builder.Configuration.GetSection("LocalAgent").Get<LocalAgentOptions>() ?? new LocalAgentOptions();
var runStdio = args.Any(arg => string.Equals(arg, "--stdio", StringComparison.OrdinalIgnoreCase)) ||
               string.Equals(configuredOptions.Mode, "stdio", StringComparison.OrdinalIgnoreCase);

if (runStdio)
{
    // stdout is reserved for newline-delimited JSON protocol messages in sidecar mode.
    builder.Logging.ClearProviders();
}

builder.Services.Configure<LocalAgentOptions>(builder.Configuration.GetSection("LocalAgent"));
builder.Services.AddHttpClient<CodeMasterServerClient>();
builder.Services.AddSingleton<LocalMetadataStore>();
builder.Services.AddSingleton<LocalCodegenExecutionService>();
builder.Services.AddSingleton<StdioLocalAgentHost>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

if (!runStdio)
{
    builder.WebHost.UseUrls($"http://127.0.0.1:{configuredOptions.Port}");
}

var app = builder.Build();

if (runStdio)
{
    await app.Services.GetRequiredService<StdioLocalAgentHost>().RunAsync();
    return;
}

app.UseCors();

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    name = "CodeMaster.LocalAgent",
    time = DateTimeOffset.UtcNow
}));

app.MapPost("/api/codegen/execution/{action}", async (
    string action,
    LocalExecutionRequest request,
    LocalCodegenExecutionService executor,
    HttpContext httpContext) =>
{
    if (!string.IsNullOrWhiteSpace(configuredOptions.ClientToken))
    {
        var clientToken = httpContext.Request.Headers["X-CodeMaster-Client-Token"].ToString();
        if (!StringComparer.Ordinal.Equals(clientToken, configuredOptions.ClientToken))
        {
            return Results.Unauthorized();
        }
    }

    var result = await executor.ExecuteAsync(action, request);
    return Results.Ok(result);
});

app.Run();
