using CodeMaster.Agent.Contracts;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;

namespace CodeMaster.Agent.Services;

public interface ICodeMasterProjectBlueprintService
{
    Task<AiProjectBlueprintDto> GetProjectAsync(long projectId);
    Task<AiEntityBlueprintDto> GetEntityAsync(long projectId, long? entityId, string? entityName);
    Task<AiUiPageBlueprintDto> GetUiPageAsync(long projectId, long? entityId, string? entityName, string pageType);
}

internal sealed class CodeMasterProjectBlueprintService : ICodeMasterProjectBlueprintService
{
    private readonly IProjectService _projectService;
    private readonly IProjectModuleService _moduleService;
    private readonly IModuleEntityService _entityService;

    public CodeMasterProjectBlueprintService(
        IProjectService projectService,
        IProjectModuleService moduleService,
        IModuleEntityService entityService)
    {
        _projectService = projectService;
        _moduleService = moduleService;
        _entityService = entityService;
    }

    public async Task<AiProjectBlueprintDto> GetProjectAsync(long projectId)
    {
        var project = await _projectService.GetByIdAsync(projectId)
            ?? throw new KeyNotFoundException("The project was not found or is not accessible.");

        var modules = await _moduleService.GetByProjectIdAsync(projectId);
        var moduleBlueprints = new List<AiModuleBlueprintDto>();
        foreach (var module in modules.OrderBy(x => x.OrderNum).ThenBy(x => x.ModuleName))
        {
            var entities = await _entityService.GetByModuleIdAsync(module.Id);
            var entityBlueprints = new List<AiEntityBlueprintDto>();
            foreach (var entity in entities.OrderBy(x => x.OrderNum).ThenBy(x => x.Name))
            {
                var detail = await _entityService.GetByIdAsync(entity.Id)
                    ?? throw new KeyNotFoundException($"Entity '{entity.Id}' disappeared while building the project blueprint.");
                entityBlueprints.Add(MapEntity(detail));
            }

            moduleBlueprints.Add(new AiModuleBlueprintDto
            {
                Id = module.Id,
                ModuleName = module.ModuleName,
                ModuleDescription = module.ModuleDescription,
                RoutePath = module.RoutePath,
                Icon = module.Icon,
                OrderNum = module.OrderNum,
                IsSynced = module.IsSynced,
                Entities = entityBlueprints
            });
        }

        var controls = await _entityService.GetFieldControlTemplatesAsync();
        var pageTemplates = await _entityService.GetPageTemplatesAsync();
        var childTemplates = await _entityService.GetChildTemplatesAsync();

        return new AiProjectBlueprintDto
        {
            Project = new AiProjectSummaryDto
            {
                Id = project.Id,
                ProjectName = project.ProjectName,
                DisplayName = project.DisplayName,
                Description = project.Description,
                DatabaseType = project.DatabaseType.ToString(),
                ProjectType = project.ProjectType.ToString(),
                Status = project.Status.ToString(),
                FrontendPort = project.FrontendPort,
                BackendPort = project.BackendPort,
                InitializedAt = project.InitializedAt
            },
            Modules = moduleBlueprints,
            Controls = controls
                .GroupBy(x => x.ControlType, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key)
                .Select(group => new AiControlCatalogDto
                {
                    ControlType = group.Key,
                    PageSections = group.Select(x => x.PageSection)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x)
                        .ToList()
                })
                .ToList(),
            PageTemplates = pageTemplates
                .OrderBy(x => x.Sort)
                .Select(x => new AiPageTemplateCatalogDto
                {
                    PageType = x.PageType,
                    Name = x.Name,
                    IsSystem = x.IsSystem
                })
                .ToList(),
            ChildTemplates = childTemplates
                .OrderBy(x => x.Sort)
                .Select(x => new AiChildTemplateCatalogDto
                {
                    PageType = x.PageType,
                    ChildType = x.ChildType
                })
                .ToList()
        };
    }

    public async Task<AiEntityBlueprintDto> GetEntityAsync(long projectId, long? entityId, string? entityName)
    {
        ModuleEntityDto? entity = null;
        if (entityId is > 0)
        {
            entity = await _entityService.GetByIdAsync(entityId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(entityName))
        {
            var candidates = await _entityService.GetListAsync(new ModuleEntityQueryDto
            {
                ProjectId = projectId,
                Name = entityName.Trim(),
                PageNum = 1,
                PageSize = 10000
            });
            var match = candidates.FirstOrDefault(x => string.Equals(x.Name, entityName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                entity = await _entityService.GetByIdAsync(match.Id);
            }
        }

        if (entity == null || entity.ProjectId != projectId)
        {
            throw new KeyNotFoundException("The entity was not found in the project bound to this conversation.");
        }

        return MapEntity(entity);
    }

    public async Task<AiUiPageBlueprintDto> GetUiPageAsync(
        long projectId,
        long? entityId,
        string? entityName,
        string pageType)
    {
        var entity = await GetEntityAsync(projectId, entityId, entityName);
        var normalizedPageType = pageType.Trim().ToLowerInvariant();
        if (normalizedPageType is not ("index" or "add" or "edit" or "detail"))
            throw new ArgumentException("PageType must be index, add, edit, or detail.");

        var stableNodes = normalizedPageType switch
        {
            "index" => new List<AiUiStableNodeDto>
            {
                new() { GenId = "gen_search_area", Role = "Search container" },
                new() { GenId = "gen_list_area", Role = "List container" },
                new() { GenId = "gen_toolbar", Role = "List toolbar" },
                new() { GenId = "gen_operations", Role = "Operation column" },
                new() { GenId = "gen_pagination", Role = "Pagination" },
                new() { GenId = "gen_action_add", Role = "Add action" }
            },
            "detail" => new List<AiUiStableNodeDto>
            {
                new() { GenId = "gen_detail_area", Role = "Detail container" }
            },
            _ => new List<AiUiStableNodeDto>
            {
                new() { GenId = "gen_form_area", Role = "Form container" },
                new() { GenId = "gen_form_actions", Role = "Form actions" },
                new() { GenId = "gen_action_submit", Role = "Submit action" },
                new() { GenId = "gen_action_cancel", Role = "Cancel action" }
            }
        };

        return new AiUiPageBlueprintDto
        {
            EntityId = entity.Id,
            EntityName = entity.Name,
            EntityDescription = entity.Description,
            PageType = normalizedPageType,
            StableNodes = stableNodes,
            Fields = entity.Fields
                .OrderBy(field => field.OrderNum)
                .Select(field => new AiUiFieldNodeDto
                {
                    FieldId = field.Id,
                    FieldName = field.Name,
                    Description = field.Description,
                    ControlType = field.FormControlType,
                    GenIds = BuildFieldGenIds(field, normalizedPageType),
                    Visible = IsVisible(field, normalizedPageType),
                    OrderNum = field.OrderNum
                })
                .ToList()
        };
    }

    private static bool IsVisible(AiFieldBlueprintDto field, string pageType)
    {
        return pageType switch
        {
            "index" => field.ShowInList || field.ShowInSearch,
            "add" => field.ShowInAddForm,
            "edit" => field.ShowInEditForm,
            "detail" => field.ShowInDetail,
            _ => false
        };
    }

    private static List<string> BuildFieldGenIds(AiFieldBlueprintDto field, string pageType)
    {
        var prefix = pageType == "index" ? "gen_col_" : "gen_field_";
        var baseGenId = $"{prefix}{field.Id}";
        if (string.Equals(field.FormControlType, "select-table", StringComparison.OrdinalIgnoreCase) &&
            pageType is "index" or "detail" &&
            !string.IsNullOrWhiteSpace(field.RelatedEntityDisplayFields))
        {
            return field.RelatedEntityDisplayFields
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(name => $"{baseGenId}_{char.ToLowerInvariant(name[0])}{name[1..]}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return new List<string> { baseGenId };
    }

    private static AiEntityBlueprintDto MapEntity(ModuleEntityDto entity)
    {
        return new AiEntityBlueprintDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            Description = entity.Description,
            TableName = entity.TableName,
            HasPrimaryKey = entity.HasPrimaryKey,
            IsTree = entity.IsTree,
            IsReadOnly = entity.IsReadOnly,
            HasTenant = entity.HasTenant,
            HasDataPermission = entity.HasDataPermission,
            HasAudit = entity.HasAudit,
            HasSoftDelete = entity.HasSoftDelete,
            GenerateFrontend = entity.GenerateFrontend,
            IsChildTable = entity.IsChildTable,
            IsGenerated = entity.IsGenerated,
            LastGeneratedTime = entity.LastGeneratedTime,
            FrontendRoute = entity.FrontendRoute,
            MenuIcon = entity.MenuIcon,
            OrderNum = entity.OrderNum,
            Remark = entity.Remark,
            Fields = entity.Fields.OrderBy(x => x.OrderNum).Select(MapField).ToList(),
            Relations = entity.EntityRelations.OrderBy(x => x.OrderNum).Select(x => new AiRelationBlueprintDto
            {
                Id = x.Id,
                SourceEntityId = x.SourceEntityId,
                TargetEntityId = x.TargetEntityId,
                TargetEntityName = x.TargetEntityName,
                TargetEntityDescription = x.TargetEntityDescription,
                RelationName = x.RelationName,
                SourceField = x.SourceField,
                TargetField = x.TargetField,
                Cardinality = x.Cardinality.ToString(),
                Ownership = x.Ownership.ToString(),
                IsRequired = x.IsRequired,
                DeleteBehavior = x.DeleteBehavior.ToString(),
                OrderNum = x.OrderNum
            }).ToList(),
            LegacyOneToManyRelations = entity.OneToManyRelations.OrderBy(x => x.OrderNum).Select(x => new AiLegacyOneToManyBlueprintDto
            {
                Id = x.Id,
                MasterField = x.MasterField,
                ChildEntityId = x.ChildEntityId,
                ChildEntityName = x.ChildEntityName,
                ChildForeignKey = x.ChildForeignKey,
                OrderNum = x.OrderNum
            }).ToList()
        };
    }

    private static AiFieldBlueprintDto MapField(EntityFieldDto field)
    {
        return new AiFieldBlueprintDto
        {
            Id = field.Id,
            Name = field.Name,
            Description = field.Description,
            IsSystemField = field.IsSystemField,
            DataType = field.DataType,
            IsNullable = field.IsNullable,
            MaxLength = field.MaxLength,
            Precision = field.Precision,
            Scale = field.Scale,
            DefaultValue = field.DefaultValue,
            IsIgnore = field.IsIgnore,
            IsPrimaryKey = field.IsPrimaryKey,
            IsRequired = field.IsRequired,
            MinValue = field.MinValue,
            MaxValue = field.MaxValue,
            RegexPattern = field.RegexPattern,
            IsEmail = field.IsEmail,
            IsPhone = field.IsPhone,
            ShowInList = field.ShowInList,
            ShowInDetail = field.ShowInDetail,
            ShowInAddForm = field.ShowInAddForm,
            ShowInEditForm = field.ShowInEditForm,
            ShowInSearch = field.ShowInSearch,
            FormControlType = field.FormControlType,
            ListWidth = field.ListWidth,
            OrderNum = field.OrderNum,
            SelectDataSource = field.SelectDataSource,
            SelectOptions = field.SelectOptions,
            IsMultiple = field.IsMultiple,
            RelatedEntityName = field.RelatedEntityName,
            RelatedEntityIdField = field.RelatedEntityIdField,
            RelatedEntityDisplayFields = field.RelatedEntityDisplayFields,
            ResultMappings = field.ResultMappings,
            FieldCategory = field.FieldCategory,
            Formula = field.Formula,
            AggregateType = field.AggregateType,
            AggregateChildEntityId = field.AggregateChildEntityId,
            AggregateChildFieldName = field.AggregateChildFieldName,
            AggregateSeparator = field.AggregateSeparator,
            Remark = field.Remark
        };
    }
}
