using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.McpServer.Services;

namespace CodeMaster.McpServer.Tools;

/// <summary>
/// MCP tool for reading CodeMaster project metadata.
/// </summary>
public class QueryTool
{
    private readonly CodeMasterApiClient _apiClient;
    private readonly ProjectContextResolver _contextResolver;

    public QueryTool(
        CodeMasterApiClient apiClient,
        ProjectContextResolver contextResolver)
    {
        _apiClient = apiClient;
        _contextResolver = contextResolver;
    }

    public static McpTool Definition => new()
    {
        Name = "query_project",
        Description = "Read CodeMaster project metadata. Use this before changing code generation metadata to discover projects, modules, entities, fields, and ids.",
        InputType = typeof(QueryInput),
        InputSchema = JsonSerializer.SerializeToNode(new
        {
            type = "object",
            properties = new
            {
                level = new { type = "string", description = "Query level: projects, modules, entities, or fields." },
                projectId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Project id for modules/entities queries." },
                moduleId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Optional module id for entities queries." },
                entityId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Entity id for fields queries." },
                workspacePath = new { type = "string", description = "Optional generated project directory. Used to resolve projectId from .codemaster/project-context.json." }
            },
            required = new[] { "level" }
        })!
    };

    public async Task<object?> HandleAsync(object? input)
    {
        var args = (QueryInput?)input ?? throw new ArgumentException("Invalid input");
        args.ProjectId = await McpProjectContextHelper.ResolveProjectIdAsync(_contextResolver, args.ProjectId, args.WorkspacePath);
        return args.Level switch
        {
            "projects" => await ListProjectsAsync(args.WorkspacePath),
            "modules" => await ListModulesAsync(args.ProjectId ?? 0, args.WorkspacePath),
            "entities" => await ListEntitiesAsync(args.ProjectId ?? 0, args.ModuleId, args.WorkspacePath),
            "fields" => await ListFieldsAsync(args.EntityId, args.WorkspacePath),
            _ => new { success = false, error = "level must be one of: projects, modules, entities, fields" }
        };
    }

    private async Task<object> ListProjectsAsync(string? workspacePath)
    {
        var projects = await _apiClient.GetAsync<List<ProjectDto>>(
            "/api/codegen/project/getlist?pageSize=1000",
            workspacePath);
        var list = projects
            .Select(p => new { Id = p.Id.ToString(), Name = p.ProjectName, p.DisplayName, p.ProjectPath, p.ProjectType, p.Status })
            .ToList();

        return new { success = true, projects = list, count = list.Count };
    }

    private async Task<object> ListModulesAsync(long projectId, string? workspacePath)
    {
        if (projectId <= 0)
        {
            return new { success = false, error = "projectId is required for level=modules" };
        }

        var modules = await _apiClient.GetAsync<List<ProjectModuleDto>>(
            $"/api/codegen/projectmodule/getbyprojectid/{projectId}",
            workspacePath);
        var list = modules.Select(m => new { Id = m.Id.ToString(), m.ModuleName, m.ModuleDescription, m.Icon, m.OrderNum }).ToList();
        return new { success = true, projectId = projectId.ToString(), modules = list, count = list.Count };
    }

    private async Task<object> ListEntitiesAsync(long projectId, long? moduleId, string? workspacePath)
    {
        if (projectId <= 0 && !moduleId.HasValue)
        {
            return new { success = false, error = "projectId or moduleId is required for level=entities" };
        }

        var entities = moduleId.HasValue
            ? await _apiClient.GetAsync<List<ModuleEntityDto>>(
                $"/api/codegen/moduleentity/getbymoduleid/{moduleId.Value}",
                workspacePath)
            : await _apiClient.GetAsync<List<ModuleEntityDto>>(
                $"/api/codegen/moduleentity/getlist?projectId={projectId}&pageSize=10000",
                workspacePath);

        var list = entities.Select(e => new
        {
            Id = e.Id.ToString(),
            e.Name,
            e.Description,
            e.TableName,
            e.HasPrimaryKey,
            e.IsTree,
            e.IsReadOnly,
            e.HasTenant,
            e.HasDataPermission,
            e.HasAudit,
            e.HasSoftDelete,
            e.GenerateFrontend,
            e.IsChildTable,
            ModuleId = e.ModuleId.ToString(),
            ProjectId = e.ProjectId.ToString()
        }).ToList();

        return new { success = true, projectId = projectId.ToString(), moduleId = moduleId?.ToString(), entities = list, count = list.Count };
    }

    private async Task<object> ListFieldsAsync(long? entityId, string? workspacePath)
    {
        if (!entityId.HasValue || entityId <= 0)
        {
            return new { success = false, error = "entityId is required for level=fields" };
        }

        var fields = await _apiClient.GetAsync<List<EntityFieldDto>>(
            $"/api/codegen/entityfield/getbyentityid/{entityId.Value}",
            workspacePath);
        var list = fields.Select(f => new
        {
            Id = f.Id.ToString(),
            f.Name,
            f.DataType,
            f.Description,
            f.IsSystemField,
            f.IsNullable,
            f.IsPrimaryKey,
            f.FormControlType,
            f.IsRequired,
            f.ShowInList,
            f.ShowInSearch,
            f.ShowInAddForm,
            f.ShowInEditForm,
            f.ShowInDetail,
            f.IsMultiple,
            f.SelectDataSource,
            f.SelectOptions,
            f.RelatedEntityName,
            f.RelatedEntityIdField,
            f.RelatedEntityDisplayFields,
            f.ResultMappings,
            f.FieldCategory,
            f.Formula,
            f.AggregateType,
            AggregateChildEntityId = f.AggregateChildEntityId?.ToString(),
            f.AggregateChildFieldName,
            f.AggregateSeparator,
            f.OrderNum
        }).ToList();

        return new { success = true, entityId = entityId.Value.ToString(), fields = list, count = list.Count };
    }
}

public class QueryInput
{
    public string Level { get; set; } = "projects";
    public long? ProjectId { get; set; }
    public long? ModuleId { get; set; }
    public long? EntityId { get; set; }
    public string? WorkspacePath { get; set; }
}
