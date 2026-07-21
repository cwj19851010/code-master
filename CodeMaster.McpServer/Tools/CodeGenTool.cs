using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.LocalAgent.Models;
using CodeMaster.LocalAgent.Services;
using CodeMaster.McpServer.Services;

namespace CodeMaster.McpServer.Tools;

public sealed class CodeGenTool
{
    private readonly CodeMasterApiClient _apiClient;
    private readonly LocalCodegenExecutionService _localExecutor;
    private readonly ProjectContextResolver _contextResolver;

    public CodeGenTool(
        CodeMasterApiClient apiClient,
        LocalCodegenExecutionService localExecutor,
        ProjectContextResolver contextResolver)
    {
        _apiClient = apiClient;
        _localExecutor = localExecutor;
        _contextResolver = contextResolver;
    }

    public static McpTool Definition => new()
    {
        Name = "generate_code",
        Description = "Generate CodeMaster code locally from a metadata bundle downloaded through CodeMaster WebApi. Supports one entity, one module, or a whole project in full or incremental mode.",
        InputType = typeof(CodeGenInput),
        InputSchema = JsonSerializer.SerializeToNode(new
        {
            type = "object",
            properties = new
            {
                entityId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Entity id." },
                entityName = new { type = "string", description = "Entity name, for example Order. Use with projectId when entityId is unknown." },
                projectId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Project id." },
                moduleId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Optional module id." },
                mode = new { type = "string", description = "full or incremental. Defaults to full." },
                validateBuild = new { type = "boolean", description = "Run dotnet build after generation. Defaults to true." },
                validateFrontendBuild = new { type = "boolean", description = "Run npm run build after generation. Defaults to false." },
                buildTimeoutSeconds = new { type = "integer", description = "Reserved for compatibility. LocalAgent currently uses its configured build timeout." },
                workspacePath = new { type = "string", description = "Optional generated project directory." }
            }
        })!
    };

    public async Task<object?> HandleAsync(object? input)
    {
        var args = (CodeGenInput?)input ?? throw new ArgumentException("Invalid input");
        args.ProjectId = await McpProjectContextHelper.ResolveProjectIdAsync(
            _contextResolver,
            args.ProjectId,
            args.WorkspacePath);

        var target = await ResolveTargetsAsync(args);
        if (target.Error != null)
            return new { success = false, message = target.Error };

        var session = await _apiClient.ResolveSessionAsync(args.WorkspacePath);
        var mode = string.Equals(args.Mode, "incremental", StringComparison.OrdinalIgnoreCase)
            ? "incremental"
            : "full";
        var action = mode == "incremental" ? "generateProjectIncrementalCode" : "generateProjectCode";
        var isProjectWide = args.EntityId <= 0 && string.IsNullOrWhiteSpace(args.EntityName) && args.ModuleId <= 0;
        var requestedEntityIds = isProjectWide
            ? new List<long>()
            : target.Entities.Select(entity => entity.Id).ToList();
        var generationResult = await ExecuteLocalAsync(
            action,
            target.Project!.Id,
            null,
            requestedEntityIds,
            args,
            session);
        var generatedEntityIds = ReadGeneratedEntityIds(generationResult.Data);
        if (generatedEntityIds == null)
            generatedEntityIds = target.Entities.Select(entity => entity.Id).ToHashSet();
        var generatedItems = target.Entities
            .Where(entity => generatedEntityIds.Contains(entity.Id))
            .Select(entity => new
            {
                entityId = entity.Id.ToString(),
                entityName = entity.Name,
                mode,
                generated = generationResult.Success,
                generationResult.Message,
                generationResult.Output
            })
            .ToList();
        var allGenerated = generationResult.Success;

        LocalExecutionResult? solutionBuild = null;
        LocalExecutionResult? frontendBuild = null;
        if (allGenerated && args.ValidateBuild)
            solutionBuild = await ExecuteLocalAsync("buildProject", target.Project!.Id, null, null, args, session);

        if (allGenerated && args.ValidateFrontendBuild)
            frontendBuild = await ExecuteLocalAsync("buildFrontend", target.Project!.Id, null, null, args, session);

        var success = allGenerated &&
                      (solutionBuild?.Success ?? true) &&
                      (frontendBuild?.Success ?? true);

        return new
        {
            success,
            projectId = target.Project!.Id.ToString(),
            target.Project.ProjectName,
            projectPath = args.WorkspacePath ?? target.Project.ProjectPath,
            mode,
            requestedEntityCount = target.Entities.Count,
            generatedEntityCount = generatedEntityIds.Count,
            generationResult,
            generatedItems,
            solutionBuild,
            frontendBuild,
            message = success
                ? "Code generated and requested validation completed."
                : "Code generation or validation failed. Inspect generatedItems and build output."
        };
    }

    private async Task<(List<ModuleEntity> Entities, Project? Project, string? Error)> ResolveTargetsAsync(CodeGenInput args)
    {
        if (args.ProjectId <= 0 && args.EntityId > 0)
        {
            var entity = await _apiClient.GetAsync<ModuleEntityDto>(
                $"/api/codegen/moduleentity/getbyid/{args.EntityId}",
                args.WorkspacePath);
            args.ProjectId = entity.ProjectId;
        }

        if (args.ProjectId <= 0 && args.ModuleId > 0)
        {
            var module = await _apiClient.GetAsync<ProjectModuleDto>(
                $"/api/codegen/projectmodule/getbyid/{args.ModuleId}",
                args.WorkspacePath);
            args.ProjectId = module.ProjectId;
        }

        if (args.ProjectId <= 0)
            return (new(), null, "Provide entityId, entityName plus projectId, moduleId, or projectId.");

        var bundle = await _apiClient.GetAsync<GenerationBundleDto>(
            $"/api/codegen/project/{args.ProjectId}/generation-bundle",
            args.WorkspacePath);

        IEnumerable<ModuleEntity> query = bundle.Entities.Where(entity => !entity.IsDeleted);
        if (args.EntityId > 0)
            query = query.Where(entity => entity.Id == args.EntityId);
        else if (!string.IsNullOrWhiteSpace(args.EntityName))
            query = query.Where(entity => string.Equals(entity.Name, args.EntityName, StringComparison.OrdinalIgnoreCase));
        else if (args.ModuleId > 0)
            query = query.Where(entity => entity.ModuleId == args.ModuleId);

        var entities = query
            .OrderBy(entity => entity.OrderNum)
            .ThenBy(entity => entity.Name)
            .ToList();

        return entities.Count == 0
            ? (entities, bundle.Project, "No matching entities found.")
            : (entities, bundle.Project, null);
    }

    private Task<LocalExecutionResult> ExecuteLocalAsync(
        string action,
        long projectId,
        long? entityId,
        IReadOnlyCollection<long>? entityIds,
        CodeGenInput args,
        McpSession session)
    {
        var payload = JsonSerializer.SerializeToElement(new
        {
            projectId,
            entityId,
            entityIds,
            targetPath = args.WorkspacePath
        });

        return _localExecutor.ExecuteAsync(action, new LocalExecutionRequest
        {
            Action = action,
            Payload = payload,
            ServerBaseUrl = session.ServerBaseUrl,
            AccessToken = session.AccessToken
        });
    }

    private static HashSet<long>? ReadGeneratedEntityIds(object? data)
    {
        if (data == null)
            return null;

        var element = JsonSerializer.SerializeToElement(data);
        if (!element.TryGetProperty("entityIds", out var entityIds) || entityIds.ValueKind != JsonValueKind.Array)
            return null;

        var result = new HashSet<long>();
        foreach (var item in entityIds.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var numericId))
                result.Add(numericId);
            else if (item.ValueKind == JsonValueKind.String && long.TryParse(item.GetString(), out var stringId))
                result.Add(stringId);
        }

        return result;
    }
}

public sealed class CodeGenInput
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
