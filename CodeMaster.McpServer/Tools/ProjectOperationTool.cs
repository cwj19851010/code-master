using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.McpServer.Services;

namespace CodeMaster.McpServer.Tools;

/// <summary>
/// MCP tool for project initialization and runtime operations.
/// </summary>
public class ProjectOperationTool
{
    private readonly IProjectService _projectService;
    private readonly ProjectContextResolver _contextResolver;

    public ProjectOperationTool(IProjectService projectService, ProjectContextResolver contextResolver)
    {
        _projectService = projectService;
        _contextResolver = contextResolver;
    }

    public static McpTool Definition => new()
    {
        Name = "run_project_operation",
        Description = "Run CodeMaster project operations through the shared project service: initialize, initialize_step, start_frontend, start_backend, start_all, stop_frontend, stop_backend, stop_all, status, migrate_database, or build.",
        InputType = typeof(ProjectOperationInput),
        InputSchema = JsonSerializer.SerializeToNode(new
        {
            type = "object",
            properties = new
            {
                operation = new { type = "string", description = "initialize, initialize_step, start_frontend, start_backend, start_all, stop_frontend, stop_backend, stop_all, status, migrate_database, or build." },
                projectId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Target project id." },
                targetPath = new { type = "string", description = "Optional initialization target path." },
                step = new { type = "integer", description = "Step number 1-11 when operation=initialize_step." },
                workspacePath = new { type = "string", description = "Optional generated project directory. Used to resolve projectId from .codemaster/project-context.json." }
            },
            required = new[] { "operation" }
        })!
    };

    public async Task<object?> HandleAsync(object? input)
    {
        var args = (ProjectOperationInput?)input ?? throw new ArgumentException("Invalid input");
        args.ProjectId = await McpProjectContextHelper.ResolveProjectIdAsync(_contextResolver, args.ProjectId, args.WorkspacePath);
        if (args.ProjectId <= 0)
            return new { success = false, message = "projectId is required." };

        return args.Operation switch
        {
            "initialize" => await InitializeAsync(args),
            "initialize_step" => await InitializeStepAsync(args),
            "start_frontend" => await _projectService.StartFrontendAsync(new ProjectActionDto { ProjectId = args.ProjectId }),
            "start_backend" => await _projectService.StartBackendAsync(new ProjectActionDto { ProjectId = args.ProjectId }),
            "start_all" => await StartAllAsync(args.ProjectId),
            "stop_frontend" => await _projectService.StopFrontendAsync(new ProjectActionDto { ProjectId = args.ProjectId }),
            "stop_backend" => await _projectService.StopBackendAsync(new ProjectActionDto { ProjectId = args.ProjectId }),
            "stop_all" => await StopAllAsync(args.ProjectId),
            "status" => await _projectService.GetStatusAsync(new ProjectActionDto { ProjectId = args.ProjectId }),
            "migrate_database" => await _projectService.MigrateDatabaseAsync(new ProjectActionDto { ProjectId = args.ProjectId }),
            "build" => await _projectService.BuildAsync(new ProjectActionDto { ProjectId = args.ProjectId }),
            _ => new { success = false, message = "operation must be one of: initialize, initialize_step, start_frontend, start_backend, start_all, stop_frontend, stop_backend, stop_all, status, migrate_database, build." }
        };
    }

    private async Task<object> InitializeAsync(ProjectOperationInput args)
    {
        var ok = await _projectService.InitializeAsync(new InitializeProjectDto
        {
            Id = args.ProjectId,
            TargetPath = args.TargetPath
        });

        return new { success = ok, projectId = args.ProjectId.ToString(), message = ok ? "Project initialized." : "Project initialization failed." };
    }

    private async Task<object> InitializeStepAsync(ProjectOperationInput args)
    {
        var dto = new InitializeStepDto { ProjectId = args.ProjectId, TargetPath = args.TargetPath };
        return args.Step switch
        {
            1 => await _projectService.Step1_ExtractTemplateAsync(dto),
            2 => await _projectService.Step2_GenerateSolutionAsync(dto),
            3 => await _projectService.Step3_UpdateDatabaseConfigAsync(dto),
            4 => await _projectService.Step4_UpdatePortConfigAsync(dto),
            5 => await _projectService.Step5_CreateMigrationAsync(dto),
            6 => await _projectService.Step6_ApplyMigrationAsync(dto),
            7 => await _projectService.Step7_DotnetRestoreAsync(dto),
            8 => await _projectService.Step8_WriteTranslationsAsync(dto),
            9 => await _projectService.Step9_NpmInstallAsync(dto),
            10 => await _projectService.Step10_StartBackendAsync(dto),
            11 => await _projectService.Step11_StartFrontendAsync(dto),
            _ => new InitializeStepResultDto
            {
                Success = false,
                Message = "step must be between 1 and 11.",
                Step = "invalid_step",
                Progress = 0
            }
        };
    }

    private async Task<object> StartAllAsync(long projectId)
    {
        var backend = await _projectService.StartBackendAsync(new ProjectActionDto { ProjectId = projectId });
        var frontend = await _projectService.StartFrontendAsync(new ProjectActionDto { ProjectId = projectId });
        return new
        {
            success = backend.Success && frontend.Success,
            projectId = projectId.ToString(),
            backend,
            frontend
        };
    }

    private async Task<object> StopAllAsync(long projectId)
    {
        var frontend = await _projectService.StopFrontendAsync(new ProjectActionDto { ProjectId = projectId });
        var backend = await _projectService.StopBackendAsync(new ProjectActionDto { ProjectId = projectId });
        return new
        {
            success = backend.Success && frontend.Success,
            projectId = projectId.ToString(),
            backend,
            frontend
        };
    }
}

public class ProjectOperationInput
{
    public string Operation { get; set; } = string.Empty;
    public long ProjectId { get; set; }
    public string? TargetPath { get; set; }
    public int Step { get; set; }
    public string? WorkspacePath { get; set; }
}
