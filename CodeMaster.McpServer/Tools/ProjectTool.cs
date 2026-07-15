using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Core.Enums;
using CodeMaster.McpServer.Services;

namespace CodeMaster.McpServer.Tools;

/// <summary>
/// MCP tool for creating or updating CodeMaster projects.
/// </summary>
public class ProjectTool
{
    private readonly IProjectService _projectService;
    private readonly ProjectContextResolver _contextResolver;

    public ProjectTool(IProjectService projectService, ProjectContextResolver contextResolver)
    {
        _projectService = projectService;
        _contextResolver = contextResolver;
    }

    public static McpTool Definition => new()
    {
        Name = "save_project",
        Description = "Create or update a CodeMaster project through the application service. Use this for natural-language project metadata changes instead of directly editing database rows or generated files.",
        InputType = typeof(ProjectToolInput),
        InputSchema = JsonSerializer.SerializeToNode(new
        {
            type = "object",
            properties = new
            {
                projectId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Existing project id. If omitted, projectName is used to upsert." },
                projectName = new { type = "string", description = "Project technical name, for example OrderSystem." },
                displayName = new { type = "string", description = "Project display name." },
                displayNameEn = new { type = "string" },
                description = new { type = "string" },
                descriptionEn = new { type = "string" },
                databaseType = new { type = "string", description = "SqlServer, MySQL, PostgreSQL, SQLite, or Oracle. Defaults to MySQL for new projects." },
                connectionString = new { type = "string" },
                projectPath = new { type = "string", description = "Base path or full project path." },
                logoPath = new { type = "string" },
                projectType = new { type = "string", description = "Server or WpfClient. Defaults to Server." },
                frontendPort = new { type = "integer" },
                backendPort = new { type = "integer" },
                workspacePath = new { type = "string", description = "Optional generated project directory. Used to resolve projectId from .codemaster/project-context.json." }
            }
        })!
    };

    public async Task<object?> HandleAsync(object? input)
    {
        var args = (ProjectToolInput?)input ?? throw new ArgumentException("Invalid input");
        args.ProjectId = await McpProjectContextHelper.ResolveProjectIdAsync(_contextResolver, args.ProjectId, args.WorkspacePath);
        var existing = await ResolveExistingAsync(args);

        if (existing == null)
        {
            if (string.IsNullOrWhiteSpace(args.ProjectName))
                return new { success = false, message = "projectName is required when creating a project." };

            var id = await _projectService.CreateAsync(new CreateProjectDto
            {
                ProjectName = args.ProjectName,
                DisplayName = args.DisplayName ?? args.ProjectName,
                DisplayNameEn = args.DisplayNameEn,
                Description = args.Description,
                DescriptionEn = args.DescriptionEn,
                DatabaseType = ParseDatabaseType(args.DatabaseType, DatabaseType.MySQL),
                ConnectionString = args.ConnectionString ?? string.Empty,
                ProjectPath = args.ProjectPath ?? string.Empty,
                LogoPath = args.LogoPath,
                ProjectType = ParseProjectType(args.ProjectType, ProjectType.Server),
                FrontendPort = args.FrontendPort,
                BackendPort = args.BackendPort
            });

            return new { success = true, isNew = true, projectId = id.ToString(), projectName = args.ProjectName };
        }

        var update = new UpdateProjectDto
        {
            Id = existing.Id,
            ProjectName = args.ProjectName ?? existing.ProjectName,
            DisplayName = args.DisplayName ?? existing.DisplayName,
            DisplayNameEn = args.DisplayNameEn ?? existing.DisplayNameEn,
            Description = args.Description ?? existing.Description,
            DescriptionEn = args.DescriptionEn ?? existing.DescriptionEn,
            DatabaseType = ParseDatabaseType(args.DatabaseType, existing.DatabaseType),
            ConnectionString = args.ConnectionString ?? existing.ConnectionString,
            ProjectPath = args.ProjectPath ?? existing.ProjectPath,
            LogoPath = args.LogoPath ?? existing.LogoPath,
            ProjectType = ParseProjectType(args.ProjectType, existing.ProjectType),
            FrontendPort = args.FrontendPort ?? existing.FrontendPort,
            BackendPort = args.BackendPort ?? existing.BackendPort
        };

        await _projectService.UpdateAsync(existing.Id, update);
        return new
        {
            success = true,
            isNew = false,
            projectId = existing.Id.ToString(),
            projectName = update.ProjectName,
            message = $"Project {update.ProjectName} updated."
        };
    }

    private async Task<ProjectDto?> ResolveExistingAsync(ProjectToolInput args)
    {
        if (args.ProjectId > 0)
            return await _projectService.GetByIdAsync(args.ProjectId);

        if (string.IsNullOrWhiteSpace(args.ProjectName))
            return null;

        var projects = await _projectService.GetListAsync(new ProjectQueryDto { ProjectName = args.ProjectName, PageSize = 1000 });
        return projects.FirstOrDefault(p => string.Equals(p.ProjectName, args.ProjectName, StringComparison.OrdinalIgnoreCase));
    }

    private static DatabaseType ParseDatabaseType(string? value, DatabaseType fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(DatabaseType), numeric))
            return (DatabaseType)numeric;

        return Enum.TryParse<DatabaseType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }

    private static ProjectType ParseProjectType(string? value, ProjectType fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(ProjectType), numeric))
            return (ProjectType)numeric;

        return Enum.TryParse<ProjectType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }
}

public class ProjectToolInput
{
    public long ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? DisplayName { get; set; }
    public string? DisplayNameEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DatabaseType { get; set; }
    public string? ConnectionString { get; set; }
    public string? ProjectPath { get; set; }
    public string? LogoPath { get; set; }
    public string? ProjectType { get; set; }
    public int? FrontendPort { get; set; }
    public int? BackendPort { get; set; }
    public string? WorkspacePath { get; set; }
}
