using System.Text.Json;
using CodeMaster.Application.Services.CodeGen;

namespace CodeMaster.McpServer.Tools;

/// <summary>
/// MCP tool for reading the complete project structure and generated file state.
/// </summary>
public class ProjectStructureTool
{
    private readonly IProjectService _projectService;

    public ProjectStructureTool(IProjectService projectService)
    {
        _projectService = projectService;
    }

    public static McpTool Definition => new()
    {
        Name = "get_project_structure",
        Description = "Read the full CodeMaster project entity structure and generated project directory state. Use this before generation and after generation to understand modules, entities, fields, relations, and generated files.",
        InputType = typeof(ProjectStructureInput),
        InputSchema = JsonSerializer.SerializeToNode(new
        {
            type = "object",
            properties = new
            {
                projectId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Target project id." },
                includeGeneratedFiles = new { type = "boolean", description = "Include generated Vue/API file presence. Defaults to true." }
            },
            required = new[] { "projectId" }
        })!
    };

    public async Task<object?> HandleAsync(object? input)
    {
        var args = (ProjectStructureInput?)input ?? throw new ArgumentException("Invalid input");
        if (args.ProjectId <= 0)
            return new { success = false, message = "projectId is required." };

        var bundle = await _projectService.GetGenerationBundleAsync(args.ProjectId);
        var projectPath = GetFullProjectPath(bundle.Project.ProjectName, bundle.Project.ProjectPath);
        var includeFiles = args.IncludeGeneratedFiles ?? true;

        var modules = bundle.Modules
            .OrderBy(m => m.OrderNum)
            .ThenBy(m => m.ModuleName)
            .Select(module =>
            {
                var entities = bundle.Entities
                    .Where(e => e.ModuleId == module.Id)
                    .OrderBy(e => e.OrderNum)
                    .ThenBy(e => e.Name)
                    .Select(entity =>
                    {
                        var fields = bundle.Fields
                            .Where(f => f.ModuleEntityId == entity.Id)
                            .OrderBy(f => f.OrderNum)
                            .ThenBy(f => f.Name)
                            .Select(f => new
                            {
                                id = f.Id.ToString(),
                                f.Name,
                                f.Description,
                                f.DataType,
                                f.IsNullable,
                                f.IsRequired,
                                f.FormControlType,
                                f.SelectDataSource,
                                f.SelectOptions,
                                f.IsMultiple,
                                f.RelatedEntityName,
                                f.RelatedEntityIdField,
                                f.RelatedEntityDisplayFields,
                                f.ShowInList,
                                f.ShowInSearch,
                                f.ShowInAddForm,
                                f.ShowInEditForm,
                                f.ShowInDetail,
                                f.ListWidth,
                                f.OrderNum
                            })
                            .ToList();

                        var relations = bundle.Relations
                            .Where(r => r.ModuleEntityId == entity.Id)
                            .OrderBy(r => r.OrderNum)
                            .Select(r => new
                            {
                                id = r.Id.ToString(),
                                moduleEntityId = r.ModuleEntityId.ToString(),
                                childEntityId = r.ChildEntityId.ToString(),
                                r.ChildEntityName,
                                r.MasterField,
                                r.ChildForeignKey,
                                r.OrderNum
                            })
                            .ToList();

                        return new
                        {
                            id = entity.Id.ToString(),
                            entity.ProjectId,
                            moduleId = entity.ModuleId.ToString(),
                            entity.Name,
                            entity.Description,
                            entity.TableName,
                            entity.IsTree,
                            entity.HasTenant,
                            entity.HasDataPermission,
                            entity.GenerateFrontend,
                            entity.IsChildTable,
                            entity.IsGenerated,
                            entity.LastGeneratedTime,
                            fields,
                            relations,
                            generatedFiles = includeFiles ? GetGeneratedFiles(projectPath, bundle.Project.ProjectName, module.ModuleName, entity.Name) : null
                        };
                    })
                    .ToList();

                return new
                {
                    id = module.Id.ToString(),
                    module.ProjectId,
                    module.ModuleName,
                    module.ModuleDescription,
                    module.Icon,
                    module.RoutePath,
                    module.OrderNum,
                    entities
                };
            })
            .ToList();

        return new
        {
            success = true,
            source = "CodeMaster metadata plus generated project files",
            generatedAt = DateTimeOffset.UtcNow,
            project = new
            {
                id = bundle.Project.Id.ToString(),
                bundle.Project.ProjectName,
                bundle.Project.DisplayName,
                bundle.Project.DatabaseType,
                bundle.Project.ProjectType,
                bundle.Project.Status,
                bundle.Project.FrontendPort,
                bundle.Project.BackendPort,
                projectPath
            },
            modules
        };
    }

    private static object GetGeneratedFiles(string projectPath, string projectName, string moduleName, string entityName)
    {
        var entityLower = entityName.ToLowerInvariant();
        var viewPath = Path.Combine(projectPath, $"{projectName}.Vue", "src", "views", moduleName.ToLowerInvariant(), entityLower);
        var apiPath = Path.Combine(projectPath, $"{projectName}.Vue", "src", "api", moduleName.ToLowerInvariant(), $"{entityLower}.js");
        var backendEntityPath = Path.Combine(projectPath, $"{projectName}.Domain", "Entities", moduleName, $"{entityName}.cs");

        string[] pageTypes = ["index", "add", "edit", "detail"];
        var pages = pageTypes.Select(page => new
        {
            page,
            vue = File.Exists(Path.Combine(viewPath, $"{page}.vue")),
            autoJs = File.Exists(Path.Combine(viewPath, $"{entityLower}.{page}.auto.js")),
            scriptJson = File.Exists(Path.Combine(viewPath, $"{entityLower}.{page}.script.json")),
            fieldsJson = File.Exists(Path.Combine(viewPath, $"{entityLower}.{page}.fields.json")),
            treeJson = File.Exists(Path.Combine(viewPath, $"{entityLower}.{page}.tree.json"))
        }).ToList();

        return new
        {
            viewPath,
            viewPathExists = Directory.Exists(viewPath),
            apiPath,
            apiExists = File.Exists(apiPath),
            backendEntityPath,
            backendEntityExists = File.Exists(backendEntityPath),
            pages
        };
    }

    private static string GetFullProjectPath(string projectName, string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            return string.Empty;

        return targetPath.EndsWith(projectName, StringComparison.OrdinalIgnoreCase)
            ? targetPath
            : Path.Combine(targetPath, projectName);
    }
}

public class ProjectStructureInput
{
    public long ProjectId { get; set; }
    public bool? IncludeGeneratedFiles { get; set; }
}
