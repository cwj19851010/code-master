using CodeMaster.Agent.Contracts;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Agent.Services;

public interface ICodeMasterProjectChangeSetService
{
    Task<ProjectChangeSetValidationResult> ValidateAsync(long projectId, ProjectChangeSetProposal proposal);
    Task<ProjectChangeSetExecutionResult> ExecuteAsync(long projectId, ProjectChangeSetProposal proposal);
}

internal sealed class CodeMasterProjectChangeSetService : ICodeMasterProjectChangeSetService
{
    private readonly ICodeMasterProjectBlueprintService _blueprintService;
    private readonly IProjectModuleService _moduleService;
    private readonly IModuleEntityService _entityService;

    public CodeMasterProjectChangeSetService(
        ICodeMasterProjectBlueprintService blueprintService,
        IProjectModuleService moduleService,
        IModuleEntityService entityService)
    {
        _blueprintService = blueprintService;
        _moduleService = moduleService;
        _entityService = entityService;
    }

    public async Task<ProjectChangeSetValidationResult> ValidateAsync(long projectId, ProjectChangeSetProposal proposal)
    {
        var blueprint = await _blueprintService.GetProjectAsync(projectId);
        return CodeMasterProjectChangeSetRules.Validate(blueprint, proposal);
    }

    public async Task<ProjectChangeSetExecutionResult> ExecuteAsync(long projectId, ProjectChangeSetProposal proposal)
    {
        var validation = await ValidateAsync(projectId, proposal);
        if (!validation.IsValid)
            throw new InvalidOperationException("The project change set is invalid: " + string.Join(" ", validation.Errors));

        var result = new ProjectChangeSetExecutionResult
        {
            ProjectId = projectId,
            Summary = proposal.Summary.Trim()
        };

        var modules = await _moduleService.GetByProjectIdAsync(projectId);
        var moduleIdsByName = modules.ToDictionary(x => x.ModuleName, x => x.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var module in proposal.Modules)
        {
            var name = module.ModuleName.Trim();
            var id = await _moduleService.CreateAsync(new CreateProjectModuleDto
            {
                ProjectId = projectId,
                ModuleName = name,
                ModuleDescription = module.ModuleDescription.Trim(),
                Icon = NormalizeOptional(module.Icon),
                OrderNum = module.OrderNum,
                RoutePath = NormalizeOptional(module.RoutePath),
                Remark = NormalizeOptional(module.Remark)
            });
            moduleIdsByName[name] = id;
            result.CreatedModules.Add(new ProjectChangeCreatedItem { Id = id, Name = name });
        }

        foreach (var update in proposal.ModuleUpdates)
        {
            var current = await _moduleService.GetByIdAsync(update.ModuleId)
                ?? throw new KeyNotFoundException($"Module '{update.ModuleId}' could not be reloaded.");
            var name = MergeRequired(update.ModuleName, current.ModuleName);
            await _moduleService.UpdateAsync(update.ModuleId, new UpdateProjectModuleDto
            {
                ModuleName = name,
                ModuleDescription = MergeRequired(update.ModuleDescription, current.ModuleDescription),
                Icon = MergeOptional(update.Icon, current.Icon),
                OrderNum = update.OrderNum ?? current.OrderNum,
                RoutePath = MergeOptional(update.RoutePath, current.RoutePath),
                Remark = MergeOptional(update.Remark, current.Remark)
            });

            moduleIdsByName.Remove(current.ModuleName);
            moduleIdsByName[name] = current.Id;
            result.UpdatedModules.Add(new ProjectChangeCreatedItem { Id = current.Id, Name = name });
        }

        var existingEntities = await _entityService.GetListAsync(new ModuleEntityQueryDto
        {
            ProjectId = projectId,
            PageNum = 1,
            PageSize = 10000
        });
        var entityIdsByName = existingEntities.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var entity in proposal.Entities)
        {
            var name = entity.Name.Trim();
            var id = await _entityService.CreateAsync(new CreateModuleEntityDto
            {
                ProjectId = projectId,
                ModuleId = ResolveModuleId(entity, moduleIdsByName),
                Name = name,
                Description = entity.Description.Trim(),
                HasPrimaryKey = entity.HasPrimaryKey,
                TableName = NormalizeOptional(entity.TableName),
                IsTree = entity.IsTree,
                IsReadOnly = entity.IsReadOnly,
                HasTenant = entity.HasTenant,
                HasDataPermission = entity.HasDataPermission,
                HasAudit = entity.HasAudit,
                HasSoftDelete = entity.HasSoftDelete,
                GenerateFrontend = entity.GenerateFrontend,
                IsChildTable = entity.IsChildTable,
                FrontendRoute = NormalizeOptional(entity.FrontendRoute),
                MenuIcon = NormalizeOptional(entity.MenuIcon),
                OrderNum = entity.OrderNum,
                Remark = NormalizeOptional(entity.Remark),
                Fields = MapFields(entity)
            });
            entityIdsByName[name] = id;
            result.CreatedEntities.Add(new ProjectChangeCreatedItem { Id = id, Name = name });
        }

        foreach (var update in proposal.EntityUpdates.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            var current = existingEntities.First(x => x.Id == update.EntityId);
            entityIdsByName.Remove(current.Name);
            entityIdsByName[update.Name!.Trim()] = current.Id;
        }

        foreach (var update in proposal.EntityUpdates)
        {
            var current = await _entityService.GetByIdAsync(update.EntityId)
                ?? throw new KeyNotFoundException($"Entity '{update.EntityId}' could not be reloaded.");
            var dto = CreateUpdateDto(current, update, entityIdsByName);
            await _entityService.UpdateAsync(update.EntityId, dto);
            result.UpdatedEntities.Add(new ProjectChangeCreatedItem
            {
                Id = update.EntityId,
                Name = dto.Name
            });
            result.CreatedRelationCount += dto.NewRelations.Count + dto.NewEntityRelations.Count;
        }

        foreach (var entity in proposal.Entities.Where(x => x.Relations.Count > 0))
        {
            var sourceEntityId = entityIdsByName[entity.Name.Trim()];
            var sourceEntity = await _entityService.GetByIdAsync(sourceEntityId)
                ?? throw new KeyNotFoundException($"Entity '{sourceEntityId}' could not be reloaded.");
            var oneToManyRelations = entity.Relations
                .Where(IsLegacyOneToMany)
                .Select(relation => MapOneToManyRelation(relation, entityIdsByName))
                .ToList();
            var entityRelations = entity.Relations
                .Where(relation => !IsLegacyOneToMany(relation))
                .Select(relation => MapRelation(relation, entityIdsByName))
                .ToList();
            await _entityService.UpdateAsync(
                sourceEntityId,
                CreateRelationOnlyUpdateDto(sourceEntity, oneToManyRelations, entityRelations));
            result.CreatedRelationCount += oneToManyRelations.Count + entityRelations.Count;
        }

        foreach (var entityId in proposal.DeleteEntityIds.Distinct())
        {
            await _entityService.DeleteAsync(entityId);
            result.DeletedEntityIds.Add(entityId);
        }

        foreach (var moduleId in proposal.DeleteModuleIds.Distinct())
        {
            await _moduleService.DeleteAsync(moduleId);
            result.DeletedModuleIds.Add(moduleId);
        }

        AddClientActions(projectId, proposal, existingEntities, result);
        return result;
    }

    internal static void AddClientActions(
        long projectId,
        ProjectChangeSetProposal proposal,
        IReadOnlyCollection<ModuleEntityDto> existingEntities,
        ProjectChangeSetExecutionResult result)
    {
        if (string.Equals(proposal.GenerationMode, "None", StringComparison.OrdinalIgnoreCase))
            return;

        var deletedIds = proposal.DeleteEntityIds.ToHashSet();
        var targetIds = new HashSet<long>(proposal.GenerationEntityIds.Where(x => !deletedIds.Contains(x)));
        foreach (var entity in result.CreatedEntities)
            targetIds.Add(entity.Id);
        foreach (var entity in result.UpdatedEntities)
            targetIds.Add(entity.Id);

        var isFull = string.Equals(proposal.GenerationMode, "Full", StringComparison.OrdinalIgnoreCase);
        if (isFull && proposal.GenerationEntityIds.Count == 0)
        {
            foreach (var entity in existingEntities.Where(x => !deletedIds.Contains(x.Id)))
                targetIds.Add(entity.Id);
        }

        var generationQueued = isFull || targetIds.Count > 0;
        if (generationQueued)
        {
            result.ClientActions.Add(new AiClientActionDto
            {
                Action = isFull ? "generateProjectCode" : "generateProjectIncrementalCode",
                ProjectId = projectId,
                EntityIds = isFull && proposal.GenerationEntityIds.Count == 0
                    ? new List<long>()
                    : targetIds.OrderBy(x => x).ToList()
            });
        }

        if (generationQueued || proposal.BuildAfterGeneration)
        {
            result.ClientActions.Add(new AiClientActionDto
            {
                Action = "buildProject",
                ProjectId = projectId
            });
        }
    }

    private static long ResolveModuleId(CreateEntityProposal entity, IReadOnlyDictionary<string, long> moduleIdsByName)
    {
        if (entity.ModuleId > 0)
            return entity.ModuleId;

        if (!string.IsNullOrWhiteSpace(entity.ModuleName) && moduleIdsByName.TryGetValue(entity.ModuleName.Trim(), out var moduleId))
            return moduleId;

        throw new InvalidOperationException($"Unable to resolve module for entity '{entity.Name}'.");
    }

    private static CreateEntityFieldDto MapField(CreateEntityFieldProposal field)
    {
        return new CreateEntityFieldDto
        {
            Name = field.Name.Trim(),
            Description = field.Description.Trim(),
            DataType = field.DataType.Trim(),
            IsNullable = field.IsNullable,
            MaxLength = field.MaxLength,
            Precision = field.Precision,
            Scale = field.Scale,
            DefaultValue = NormalizeOptional(field.DefaultValue),
            IsIgnore = field.IsIgnore,
            IsPrimaryKey = field.IsPrimaryKey,
            IsRequired = field.IsRequired,
            MinValue = NormalizeOptional(field.MinValue),
            MaxValue = NormalizeOptional(field.MaxValue),
            RegexPattern = NormalizeOptional(field.RegexPattern),
            IsEmail = field.IsEmail,
            IsPhone = field.IsPhone,
            ShowInList = field.ShowInList,
            ShowInDetail = field.ShowInDetail,
            ShowInAddForm = field.ShowInAddForm,
            ShowInEditForm = field.ShowInEditForm,
            ShowInSearch = field.ShowInSearch,
            FormControlType = field.FormControlType.Trim(),
            ListWidth = field.ListWidth,
            OrderNum = field.OrderNum,
            SelectDataSource = NormalizeOptional(field.SelectDataSource),
            SelectOptions = NormalizeOptional(field.SelectOptions),
            IsMultiple = field.IsMultiple,
            RelatedEntityName = NormalizeOptional(field.RelatedEntityName),
            RelatedEntityIdField = NormalizeOptional(field.RelatedEntityIdField),
            RelatedEntityDisplayFields = NormalizeOptional(field.RelatedEntityDisplayFields),
            ResultMappings = NormalizeOptional(field.ResultMappings),
            FieldCategory = string.IsNullOrWhiteSpace(field.FieldCategory) ? "Normal" : field.FieldCategory.Trim(),
            Formula = NormalizeOptional(field.Formula),
            AggregateType = NormalizeOptional(field.AggregateType),
            AggregateChildEntityId = field.AggregateChildEntityId,
            AggregateChildFieldName = NormalizeOptional(field.AggregateChildFieldName),
            AggregateSeparator = NormalizeOptional(field.AggregateSeparator),
            Remark = NormalizeOptional(field.Remark)
        };
    }

    internal static List<CreateEntityFieldDto> MapFields(CreateEntityProposal entity)
    {
        var fields = entity.Fields.Select(MapField).ToList();
        if (entity.HasPrimaryKey && fields.All(field => !field.IsPrimaryKey))
        {
            fields.Insert(0, new CreateEntityFieldDto
            {
                Name = "Id",
                Description = "主键",
                IsSystemField = true,
                DataType = "long",
                IsNullable = false,
                IsPrimaryKey = true,
                IsRequired = true,
                ShowInList = false,
                ShowInDetail = false,
                ShowInAddForm = false,
                ShowInEditForm = false,
                ShowInSearch = false,
                FormControlType = "input",
                OrderNum = 0
            });
        }
        return fields;
    }

    private static UpdateEntityFieldWithIdDto MapUpdatedField(
        EntityFieldDto current,
        UpdateEntityFieldProposal update)
    {
        return new UpdateEntityFieldWithIdDto
        {
            Id = current.Id,
            Name = MergeRequired(update.Name, current.Name),
            Description = MergeRequired(update.Description, current.Description),
            DataType = MergeRequired(update.DataType, current.DataType),
            IsNullable = update.IsNullable ?? current.IsNullable,
            MaxLength = update.ClearMaxLength ? null : update.MaxLength ?? current.MaxLength,
            Precision = update.ClearPrecision ? null : update.Precision ?? current.Precision,
            Scale = update.ClearScale ? null : update.Scale ?? current.Scale,
            DefaultValue = MergeOptional(update.DefaultValue, current.DefaultValue),
            IsIgnore = update.IsIgnore ?? current.IsIgnore,
            IsPrimaryKey = update.IsPrimaryKey ?? current.IsPrimaryKey,
            IsRequired = update.IsRequired ?? current.IsRequired,
            MinValue = MergeOptional(update.MinValue, current.MinValue),
            MaxValue = MergeOptional(update.MaxValue, current.MaxValue),
            RegexPattern = MergeOptional(update.RegexPattern, current.RegexPattern),
            IsEmail = update.IsEmail ?? current.IsEmail,
            IsPhone = update.IsPhone ?? current.IsPhone,
            ShowInList = update.ShowInList ?? current.ShowInList,
            ShowInDetail = update.ShowInDetail ?? current.ShowInDetail,
            ShowInAddForm = update.ShowInAddForm ?? current.ShowInAddForm,
            ShowInEditForm = update.ShowInEditForm ?? current.ShowInEditForm,
            ShowInSearch = update.ShowInSearch ?? current.ShowInSearch,
            FormControlType = MergeRequired(update.FormControlType, current.FormControlType),
            ListWidth = update.ClearListWidth ? null : update.ListWidth ?? current.ListWidth,
            OrderNum = update.OrderNum ?? current.OrderNum,
            SelectDataSource = MergeOptional(update.SelectDataSource, current.SelectDataSource),
            SelectOptions = MergeOptional(update.SelectOptions, current.SelectOptions),
            IsMultiple = update.IsMultiple ?? current.IsMultiple,
            RelatedEntityName = MergeOptional(update.RelatedEntityName, current.RelatedEntityName),
            RelatedEntityIdField = MergeOptional(update.RelatedEntityIdField, current.RelatedEntityIdField),
            RelatedEntityDisplayFields = MergeOptional(update.RelatedEntityDisplayFields, current.RelatedEntityDisplayFields),
            ResultMappings = MergeOptional(update.ResultMappings, current.ResultMappings),
            FieldCategory = MergeRequired(update.FieldCategory, current.FieldCategory),
            Formula = MergeOptional(update.Formula, current.Formula),
            AggregateType = MergeOptional(update.AggregateType, current.AggregateType),
            AggregateChildEntityId = update.ClearAggregateChildEntityId
                ? null
                : update.AggregateChildEntityId ?? current.AggregateChildEntityId,
            AggregateChildFieldName = MergeOptional(update.AggregateChildFieldName, current.AggregateChildFieldName),
            AggregateSeparator = MergeOptional(update.AggregateSeparator, current.AggregateSeparator),
            Remark = MergeOptional(update.Remark, current.Remark)
        };
    }

    private static CreateEntityRelationDto MapRelation(
        CreateEntityRelationProposal relation,
        IReadOnlyDictionary<string, long> entityIdsByName)
    {
        return new CreateEntityRelationDto
        {
            TargetEntityId = ResolveTargetEntityId(relation.TargetEntityId, relation.TargetEntityName, null, entityIdsByName),
            RelationName = relation.RelationName.Trim(),
            SourceField = relation.SourceField.Trim(),
            TargetField = relation.TargetField.Trim(),
            Cardinality = Enum.Parse<EntityRelationCardinality>(relation.Cardinality, true),
            Ownership = Enum.Parse<EntityRelationOwnership>(relation.Ownership, true),
            IsRequired = relation.IsRequired,
            DeleteBehavior = Enum.Parse<EntityRelationDeleteBehavior>(relation.DeleteBehavior, true),
            OrderNum = relation.OrderNum
        };
    }

    internal static bool IsLegacyOneToMany(CreateEntityRelationProposal relation)
    {
        return Enum.TryParse<EntityRelationCardinality>(relation.Cardinality, true, out var cardinality) &&
               Enum.TryParse<EntityRelationOwnership>(relation.Ownership, true, out var ownership) &&
               cardinality == EntityRelationCardinality.OneToMany &&
               ownership == EntityRelationOwnership.Owned;
    }

    internal static CreateOneToManyRelationDto MapOneToManyRelation(
        CreateEntityRelationProposal relation,
        IReadOnlyDictionary<string, long> entityIdsByName)
    {
        var childEntityId = ResolveTargetEntityId(
            relation.TargetEntityId,
            relation.TargetEntityName,
            null,
            entityIdsByName);
        var childEntityName = !string.IsNullOrWhiteSpace(relation.TargetEntityName)
            ? relation.TargetEntityName.Trim()
            : entityIdsByName.First(item => item.Value == childEntityId).Key;
        return new CreateOneToManyRelationDto
        {
            MasterField = relation.SourceField.Trim(),
            ChildEntityId = childEntityId,
            ChildEntityName = childEntityName,
            ChildForeignKey = relation.TargetField.Trim(),
            OrderNum = relation.OrderNum
        };
    }

    private static UpdateEntityRelationWithIdDto MapUpdatedRelation(
        EntityRelationDto current,
        UpdateEntityRelationProposal update,
        IReadOnlyDictionary<string, long> entityIdsByName)
    {
        return new UpdateEntityRelationWithIdDto
        {
            Id = current.Id,
            TargetEntityId = ResolveTargetEntityId(update.TargetEntityId, update.TargetEntityName, current.TargetEntityId, entityIdsByName),
            RelationName = MergeRequired(update.RelationName, current.RelationName),
            SourceField = MergeRequired(update.SourceField, current.SourceField),
            TargetField = MergeRequired(update.TargetField, current.TargetField),
            Cardinality = update.Cardinality == null
                ? current.Cardinality
                : Enum.Parse<EntityRelationCardinality>(update.Cardinality, true),
            Ownership = update.Ownership == null
                ? current.Ownership
                : Enum.Parse<EntityRelationOwnership>(update.Ownership, true),
            IsRequired = update.IsRequired ?? current.IsRequired,
            DeleteBehavior = update.DeleteBehavior == null
                ? current.DeleteBehavior
                : Enum.Parse<EntityRelationDeleteBehavior>(update.DeleteBehavior, true),
            OrderNum = update.OrderNum ?? current.OrderNum
        };
    }

    private static UpdateModuleEntityDto CreateUpdateDto(
        ModuleEntityDto current,
        UpdateEntityProposal update,
        IReadOnlyDictionary<string, long> entityIdsByName)
    {
        var fieldsById = current.Fields.ToDictionary(x => x.Id);
        var relationsById = current.EntityRelations.ToDictionary(x => x.Id);
        return new UpdateModuleEntityDto
        {
            Name = MergeRequired(update.Name, current.Name),
            Description = MergeRequired(update.Description, current.Description),
            HasPrimaryKey = update.HasPrimaryKey ?? current.HasPrimaryKey,
            TableName = MergeOptional(update.TableName, current.TableName),
            IsTree = update.IsTree ?? current.IsTree,
            IsReadOnly = update.IsReadOnly ?? current.IsReadOnly,
            HasTenant = update.HasTenant ?? current.HasTenant,
            HasDataPermission = update.HasDataPermission ?? current.HasDataPermission,
            HasAudit = update.HasAudit ?? current.HasAudit,
            HasSoftDelete = update.HasSoftDelete ?? current.HasSoftDelete,
            GenerateFrontend = update.GenerateFrontend ?? current.GenerateFrontend,
            IsChildTable = update.IsChildTable ?? current.IsChildTable,
            FrontendRoute = MergeOptional(update.FrontendRoute, current.FrontendRoute),
            MenuIcon = MergeOptional(update.MenuIcon, current.MenuIcon),
            OrderNum = update.OrderNum ?? current.OrderNum,
            Remark = MergeOptional(update.Remark, current.Remark),
            NewFields = update.NewFields.Select(MapField).ToList(),
            UpdatedFields = update.UpdatedFields
                .Select(field => MapUpdatedField(fieldsById[field.FieldId], field))
                .ToList(),
            DeletedFieldIds = update.DeletedFieldIds.Distinct().ToList(),
            NewRelations = update.NewRelations
                .Where(IsLegacyOneToMany)
                .Select(relation => MapOneToManyRelation(relation, entityIdsByName))
                .ToList(),
            NewEntityRelations = update.NewRelations
                .Where(relation => !IsLegacyOneToMany(relation))
                .Select(relation => MapRelation(relation, entityIdsByName))
                .ToList(),
            UpdatedEntityRelations = update.UpdatedRelations
                .Select(relation => MapUpdatedRelation(relationsById[relation.RelationId], relation, entityIdsByName))
                .ToList(),
            DeletedEntityRelationIds = update.DeletedRelationIds.Distinct().ToList()
        };
    }

    private static UpdateModuleEntityDto CreateRelationOnlyUpdateDto(
        ModuleEntityDto entity,
        List<CreateOneToManyRelationDto> oneToManyRelations,
        List<CreateEntityRelationDto> entityRelations)
    {
        return new UpdateModuleEntityDto
        {
            Name = entity.Name,
            Description = entity.Description,
            HasPrimaryKey = entity.HasPrimaryKey,
            TableName = entity.TableName,
            IsTree = entity.IsTree,
            IsReadOnly = entity.IsReadOnly,
            HasTenant = entity.HasTenant,
            HasDataPermission = entity.HasDataPermission,
            HasAudit = entity.HasAudit,
            HasSoftDelete = entity.HasSoftDelete,
            GenerateFrontend = entity.GenerateFrontend,
            IsChildTable = entity.IsChildTable,
            FrontendRoute = entity.FrontendRoute,
            MenuIcon = entity.MenuIcon,
            OrderNum = entity.OrderNum,
            Remark = entity.Remark,
            NewRelations = oneToManyRelations,
            NewEntityRelations = entityRelations
        };
    }

    private static long ResolveTargetEntityId(
        long? targetEntityId,
        string? targetEntityName,
        long? currentTargetEntityId,
        IReadOnlyDictionary<string, long> entityIdsByName)
    {
        if (targetEntityId is > 0)
            return targetEntityId.Value;

        if (!string.IsNullOrWhiteSpace(targetEntityName)
            && entityIdsByName.TryGetValue(targetEntityName.Trim(), out var resolvedId))
        {
            return resolvedId;
        }

        if (currentTargetEntityId is > 0 && targetEntityId == null && targetEntityName == null)
            return currentTargetEntityId.Value;

        throw new InvalidOperationException($"Unable to resolve target entity '{targetEntityName ?? targetEntityId?.ToString()}'.");
    }

    private static string MergeRequired(string? value, string current)
        => value == null ? current : value.Trim();

    private static string? MergeOptional(string? value, string? current)
        => value == null ? current : NormalizeOptional(value);

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
