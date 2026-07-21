using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.McpServer.Services;

namespace CodeMaster.McpServer.Tools;

/// <summary>
/// MCP tool for creating or updating CodeMaster project modules.
/// </summary>
public class ModuleTool
{
    private readonly CodeMasterApiClient _apiClient;
    private readonly ProjectContextResolver _contextResolver;

    public ModuleTool(CodeMasterApiClient apiClient, ProjectContextResolver contextResolver)
    {
        _apiClient = apiClient;
        _contextResolver = contextResolver;
    }

    public static McpTool Definition => new()
    {
        Name = "save_module",
        Description = "Create or update a CodeMaster project module through the application service. Use this for natural-language module changes before creating entities.",
        InputType = typeof(ModuleToolInput),
        InputSchema = JsonSerializer.SerializeToNode(new
        {
            type = "object",
            properties = new
            {
                projectId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Target project id." },
                moduleId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Existing module id. If omitted, moduleName is used to upsert in the project." },
                moduleName = new { type = "string", description = "Module technical name, PascalCase recommended." },
                moduleDescription = new { type = "string", description = "Module display title." },
                icon = new { type = "string", description = "Element Plus icon name." },
                orderNum = new { type = "integer" },
                routePath = new { type = "string" },
                remark = new { type = "string" },
                workspacePath = new { type = "string", description = "Optional generated project directory. Used to resolve projectId from .codemaster/project-context.json." }
            },
            required = new[] { "moduleName" }
        })!
    };

    public async Task<object?> HandleAsync(object? input)
    {
        var args = (ModuleToolInput?)input ?? throw new ArgumentException("Invalid input");
        args.ProjectId = await McpProjectContextHelper.ResolveProjectIdAsync(_contextResolver, args.ProjectId, args.WorkspacePath);
        if (args.ProjectId <= 0)
            return new { success = false, message = "projectId is required." };

        if (string.IsNullOrWhiteSpace(args.ModuleName))
            return new { success = false, message = "moduleName is required." };

        var module = await ResolveExistingAsync(args);
        if (module == null)
        {
            var id = await _apiClient.PostAsync<long>("/api/codegen/projectmodule/create", new CreateProjectModuleDto
            {
                ProjectId = args.ProjectId,
                ModuleName = args.ModuleName,
                ModuleDescription = args.ModuleDescription ?? args.ModuleName,
                Icon = args.Icon ?? "Document",
                OrderNum = args.OrderNum ?? 1,
                RoutePath = args.RoutePath,
                Remark = args.Remark
            }, args.WorkspacePath);

            return new { success = true, isNew = true, projectId = args.ProjectId.ToString(), moduleId = id.ToString(), moduleName = args.ModuleName };
        }

        await _apiClient.PutAsync<int>($"/api/codegen/projectmodule/update/{module.Id}", new UpdateProjectModuleDto
        {
            ModuleName = args.ModuleName ?? module.ModuleName,
            ModuleDescription = args.ModuleDescription ?? module.ModuleDescription,
            Icon = args.Icon ?? module.Icon,
            OrderNum = args.OrderNum ?? module.OrderNum,
            RoutePath = args.RoutePath ?? module.RoutePath,
            Remark = args.Remark ?? module.Remark
        }, args.WorkspacePath);

        return new
        {
            success = true,
            isNew = false,
            projectId = args.ProjectId.ToString(),
            moduleId = module.Id.ToString(),
            moduleName = args.ModuleName ?? module.ModuleName,
            message = $"Module {args.ModuleName ?? module.ModuleName} updated."
        };
    }

    private async Task<ProjectModuleDto?> ResolveExistingAsync(ModuleToolInput args)
    {
        if (args.ModuleId > 0)
            return await _apiClient.GetAsync<ProjectModuleDto>(
                $"/api/codegen/projectmodule/getbyid/{args.ModuleId}",
                args.WorkspacePath);

        var modules = await _apiClient.GetAsync<List<ProjectModuleDto>>(
            $"/api/codegen/projectmodule/getbyprojectid/{args.ProjectId}",
            args.WorkspacePath);
        return modules.FirstOrDefault(m => string.Equals(m.ModuleName, args.ModuleName, StringComparison.OrdinalIgnoreCase));
    }
}

public class ModuleToolInput
{
    public long ProjectId { get; set; }
    public long ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? ModuleDescription { get; set; }
    public string? Icon { get; set; }
    public int? OrderNum { get; set; }
    public string? RoutePath { get; set; }
    public string? Remark { get; set; }
    public string? WorkspacePath { get; set; }
}
