using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.McpServer.Services;
using SqlSugar;

namespace CodeMaster.McpServer.Tools;

/// <summary>
/// MCP tool that generates code for entities and can validate generated projects.
/// </summary>
public class CodeGenTool
{
    private readonly IModuleEntityService _entityService;
    private readonly ISqlSugarClient _db;
    private readonly ProjectContextResolver _contextResolver;

    public CodeGenTool(IModuleEntityService entityService, ISqlSugarClient db, ProjectContextResolver contextResolver)
    {
        _entityService = entityService;
        _db = db;
        _contextResolver = contextResolver;
    }

    public static McpTool Definition => new()
    {
        Name = "generate_code",
        Description = "Generate CodeMaster code through the shared generation service. Supports one entity, one module, or the whole project, with full or incremental mode.",
        InputType = typeof(CodeGenInput),
        InputSchema = JsonSerializer.SerializeToNode(new
        {
            type = "object",
            properties = new
            {
                entityId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Entity id. String is accepted for large long values." },
                entityName = new { type = "string", description = "Entity name, for example Order. Use with projectId when entityId is unknown." },
                projectId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Project id. Required for project/module generation or entityName lookup." },
                moduleId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Optional module id. When supplied without entityId, all entities in the module are generated." },
                mode = new { type = "string", description = "full or incremental. Defaults to full." },
                validateBuild = new { type = "boolean", description = "Run dotnet build on the generated solution after generation. Defaults to true." },
                validateFrontendBuild = new { type = "boolean", description = "Run npm run build in the generated Vue project after generation. Defaults to false because it is slower." },
                buildTimeoutSeconds = new { type = "integer", description = "Timeout for each validation command. Defaults to 300 seconds." },
                workspacePath = new { type = "string", description = "Optional generated project directory. Used to resolve projectId from .codemaster/project-context.json." }
            }
        })!
    };

    public async Task<object?> HandleAsync(object? input)
    {
        var args = (CodeGenInput?)input ?? throw new ArgumentException("Invalid input");
        args.ProjectId = await McpProjectContextHelper.ResolveProjectIdAsync(_contextResolver, args.ProjectId, args.WorkspacePath);
        var target = await ResolveTargetsAsync(args);
        if (target.Error != null)
            return new { success = false, message = target.Error };

        var project = target.Project!;
        var entities = target.Entities;
        var mode = NormalizeMode(args.Mode);

        Console.Error.WriteLine($"[CodeGenTool] Generating {entities.Count} entity/entities for project {project.ProjectName} ({project.Id}) with mode={mode}");

        var generatedItems = new List<object>();
        var allGenerated = true;
        foreach (var entity in entities)
        {
            var generated = mode == "incremental"
                ? await _entityService.GenerateIncrementalCodeAsync(entity.Id)
                : await _entityService.GenerateCodeAsync(entity.Id);

            allGenerated &= generated;
            generatedItems.Add(new
            {
                entityId = entity.Id.ToString(),
                entityName = entity.Name,
                mode,
                generated
            });
        }

        object? solutionBuild = null;
        object? frontendBuild = null;

        if (allGenerated && args.ValidateBuild)
            solutionBuild = await BuildSolutionAsync(project, args.BuildTimeoutSeconds);

        if (allGenerated && args.ValidateFrontendBuild)
            frontendBuild = await BuildFrontendAsync(project, args.BuildTimeoutSeconds);

        var validationSucceeded = IsValidationSuccess(solutionBuild) && IsValidationSuccess(frontendBuild);
        var success = allGenerated && validationSucceeded;

        return new
        {
            success,
            projectId = project.Id.ToString(),
            projectName = project.ProjectName,
            projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath),
            mode,
            entityCount = entities.Count,
            generatedItems,
            solutionBuild,
            frontendBuild,
            message = BuildMessage(allGenerated, validationSucceeded, args)
        };
    }

    private async Task<(List<ModuleEntity> Entities, Project? Project, string? Error)> ResolveTargetsAsync(CodeGenInput args)
    {
        var query = _db.Queryable<ModuleEntity>().Where(e => !e.IsDeleted);

        if (args.EntityId > 0)
        {
            query = query.Where(e => e.Id == args.EntityId);
        }
        else if (!string.IsNullOrWhiteSpace(args.EntityName) && args.ProjectId > 0)
        {
            query = query.Where(e => e.ProjectId == args.ProjectId && e.Name == args.EntityName);
        }
        else if (args.ModuleId > 0)
        {
            query = query.Where(e => e.ModuleId == args.ModuleId);
        }
        else if (args.ProjectId > 0)
        {
            query = query.Where(e => e.ProjectId == args.ProjectId);
        }
        else
        {
            return (new List<ModuleEntity>(), null, "Provide entityId, entityName plus projectId, moduleId, or projectId.");
        }

        var entities = (await query.ToListAsync())
            .OrderBy(e => e.OrderNum)
            .ThenBy(e => e.Name)
            .ToList();
        if (entities.Count == 0)
            return (entities, null, "No matching entities found.");

        var projectId = entities.First().ProjectId;
        if (entities.Any(e => e.ProjectId != projectId))
            return (entities, null, "Resolved entities belong to multiple projects, which is not supported in one generate_code call.");

        var project = await _db.Queryable<Project>()
            .Where(p => !p.IsDeleted && p.Id == projectId)
            .FirstAsync();

        if (project == null)
            return (entities, null, $"Project {projectId} was not found.");

        return (entities, project, null);
    }

    private async Task<object> BuildSolutionAsync(Project project, int timeoutSeconds)
    {
        var projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath);
        var slnPath = Path.Combine(projectPath, $"{project.ProjectName}.sln");
        if (!File.Exists(slnPath))
            return new { skipped = true, success = true, reason = $"Solution file not found: {slnPath}" };

        return await RunCommandAsync("dotnet", new[] { "build", slnPath }, projectPath, timeoutSeconds);
    }

    private async Task<object> BuildFrontendAsync(Project project, int timeoutSeconds)
    {
        var projectPath = GetFullProjectPath(project.ProjectName, project.ProjectPath);
        var vuePath = Path.Combine(projectPath, $"{project.ProjectName}.Vue");
        if (!Directory.Exists(vuePath))
            return new { skipped = true, success = true, reason = $"Vue project directory not found: {vuePath}" };

        return await RunCommandAsync(NpmCommand(), new[] { "run", "build" }, vuePath, timeoutSeconds);
    }

    private static async Task<object> RunCommandAsync(string fileName, string[] arguments, string workingDirectory, int timeoutSeconds)
    {
        var output = new StringBuilder();
        var startedAt = DateTimeOffset.UtcNow;

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new
            {
                success = false,
                command = FormatCommand(fileName, arguments),
                workingDirectory,
                exitCode = -1,
                output = ex.Message,
                durationMs = 0
            };
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 1)));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            output.AppendLine($"Command timed out after {timeoutSeconds} seconds.");
        }

        var elapsed = DateTimeOffset.UtcNow - startedAt;
        return new
        {
            success = process.HasExited && process.ExitCode == 0,
            command = FormatCommand(fileName, arguments),
            workingDirectory,
            exitCode = process.HasExited ? process.ExitCode : -1,
            output = TrimOutput(output.ToString()),
            durationMs = (long)elapsed.TotalMilliseconds
        };
    }

    private static bool IsValidationSuccess(object? result)
    {
        if (result == null)
            return true;

        var property = result.GetType().GetProperty("success");
        return property?.GetValue(result) as bool? ?? false;
    }

    private static string BuildMessage(bool generated, bool validationSucceeded, CodeGenInput args)
    {
        if (!generated)
            return "Code generation failed. Check project configuration and entity fields.";

        if ((args.ValidateBuild || args.ValidateFrontendBuild) && !validationSucceeded)
            return "Code was generated, but validation failed. Inspect solutionBuild/frontendBuild output.";

        return args.ValidateBuild || args.ValidateFrontendBuild
            ? "Code generated and validation passed."
            : "Code generated.";
    }

    private static string NormalizeMode(string? mode) =>
        string.Equals(mode, "incremental", StringComparison.OrdinalIgnoreCase) ? "incremental" : "full";

    private static string GetFullProjectPath(string projectName, string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            return string.Empty;

        return targetPath.EndsWith(projectName, StringComparison.OrdinalIgnoreCase)
            ? targetPath
            : Path.Combine(targetPath, projectName);
    }

    private static string NpmCommand() =>
        OperatingSystem.IsWindows() ? "npm.cmd" : "npm";

    private static string FormatCommand(string fileName, string[] arguments) =>
        string.Join(" ", new[] { fileName }.Concat(arguments.Select(QuoteArgument)));

    private static string QuoteArgument(string argument) =>
        argument.Contains(' ') ? $"\"{argument}\"" : argument;

    private static string TrimOutput(string output)
    {
        const int maxLength = 12000;
        return output.Length <= maxLength ? output : output[^maxLength..];
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort cleanup after validation timeout.
        }
    }
}

public class CodeGenInput
{
    public long EntityId { get; set; }
    public string? EntityName { get; set; }
    public long ProjectId { get; set; }
    public long ModuleId { get; set; }
    public string? Mode { get; set; }
    public bool ValidateBuild { get; set; } = true;
    public bool ValidateFrontendBuild { get; set; }
    public int BuildTimeoutSeconds { get; set; } = 300;
    public string? WorkspacePath { get; set; }
}
