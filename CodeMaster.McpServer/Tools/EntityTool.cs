using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;
using CodeMaster.McpServer.Services;

namespace CodeMaster.McpServer.Tools;

/// <summary>
/// MCP tool for creating or updating CodeMaster entity metadata.
/// </summary>
public class EntityTool
{
    private readonly CodeMasterApiClient _apiClient;
    private readonly ProjectContextResolver _contextResolver;

    public EntityTool(
        CodeMasterApiClient apiClient,
        ProjectContextResolver contextResolver)
    {
        _apiClient = apiClient;
        _contextResolver = contextResolver;
    }

    public static McpTool Definition => new()
    {
        Name = "create_or_update_entity",
        Description = "Create or update CodeMaster entity metadata through the authenticated WebApi. Supports field controls, select-table result mappings, legacy one-to-many relations, and owned one-to-one relations. Does not delete fields or relations unless future tools explicitly support deletion.",
        InputType = typeof(EntityToolInput),
        InputSchema = JsonSerializer.SerializeToNode(new
        {
            type = "object",
            properties = new
            {
                projectId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Target project id." },
                moduleId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Optional existing module id." },
                moduleName = new { type = "string", description = "Module name in PascalCase, for example OrderManagement." },
                moduleDescription = new { type = "string", description = "Human-readable module title." },
                moduleIcon = new { type = "string", description = "Element Plus icon name. Defaults to Document." },
                moduleOrderNum = new { type = "integer", description = "Module sort order." },
                entityId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Optional existing entity id." },
                entityName = new { type = "string", description = "Entity name in PascalCase, for example Order or OrderItem." },
                description = new { type = "string", description = "Human-readable entity title." },
                tableName = new { type = "string", description = "Optional custom physical table name. Leave empty for CodeMaster's default plural snake_case name without a prefix. Never invent a prefix unless the user explicitly requests one." },
                hasPrimaryKey = new { type = "boolean", description = "Generate the standard long Id primary key and enable keyed services/migration. Defaults to true for new entities." },
                isTree = new { type = "boolean", description = "Generate a tree entity." },
                isReadOnly = new { type = "boolean", description = "Disable create/update/delete. With a primary key, GetById remains available; without a primary key, only list/query/export are generated." },
                isMultiTenant = new { type = "boolean", description = "Enable tenant fields and filters." },
                hasDataPermission = new { type = "boolean", description = "Enable department data permission." },
                hasAudit = new { type = "boolean", description = "Enable audit fields. Defaults to true for new entities." },
                hasSoftDelete = new { type = "boolean", description = "Enable soft delete. Defaults to true for new entities." },
                generateFrontend = new { type = "boolean", description = "Generate Vue pages. Defaults to true for new entities." },
                isChildTable = new { type = "boolean", description = "Mark this entity as a child table and suppress standalone Vue pages. Its API can still be generated for aggregate pages." },
                frontendRoute = new { type = "string" },
                menuIcon = new { type = "string" },
                orderNum = new { type = "integer" },
                remark = new { type = "string" },
                workspacePath = new { type = "string", description = "Optional generated project directory. Used to resolve projectId from .codemaster/project-context.json." },
                fields = new
                {
                    type = "array",
                    description = "Business fields. Do not include Id, tree, audit, soft-delete, tenant, or data-permission system fields. Existing fields are matched by id first, then name.",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Existing field id for updates." },
                            name = new { type = "string", description = "Field name in PascalCase." },
                            dataType = new { type = "string", description = "C# data type: string, long, int, decimal, DateTime, bool, Guid, enum, or text." },
                            description = new { type = "string", description = "Human-readable field title." },
                            maxLength = new { type = "integer", description = "String max length." },
                            minLength = new { type = "integer", description = "Alias for minValue." },
                            minValue = new { type = "string", description = "Minimum validation value." },
                            maxValue = new { type = "string", description = "Maximum validation value." },
                            precision = new { type = "integer", description = "Decimal precision." },
                            scale = new { type = "integer", description = "Decimal scale." },
                            isRequired = new { type = "boolean", description = "Required validation." },
                            isNullable = new { type = "boolean", description = "Nullable database field." },
                            defaultValue = new { type = "string", description = "Default value." },
                            regexPattern = new { type = "string", description = "Validation regex." },
                            isEmail = new { type = "boolean", description = "Email validation." },
                            isPhone = new { type = "boolean", description = "Phone validation." },
                            enumValues = new { type = "string", description = "Comma-separated options, written to selectOptions." },
                            selectDataSource = new { type = "string", description = "Select data source, for example dict or static." },
                            selectOptions = new { type = "string", description = "Raw select options/dictionary config." },
                            formControlType = new { type = "string", description = "input, textarea, number, select, select-table, date, datetime, switch, checkbox, checkbox-group, radio-group, file, image, editor, cascader, or table-column." },
                            relatedEntityName = new { type = "string", description = "Related entity name for select-table or cascader." },
                            relatedEntityIdField = new { type = "string", description = "Related entity value field for select-table or cascader. Defaults to Id." },
                            relatedDisplayFields = new { type = "array", items = new { type = "string" }, description = "Related display field names, for example [\"Name\", \"Type\"]." },
                            relatedEntityDisplayFields = new { type = "string", description = "Raw JSON array for related display fields." },
                            resultMappings = new
                            {
                                type = "array",
                                description = "For select-table, copy fields from the selected related row into local entity fields.",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        sourceField = new { type = "string", description = "Field on the selected related entity." },
                                        targetField = new { type = "string", description = "Local entity field that receives the selected value." }
                                    },
                                    required = new[] { "sourceField", "targetField" }
                                }
                            },
                            isMultiple = new { type = "boolean", description = "Allow multiple values for select or select-table." },
                            showInList = new { type = "boolean", description = "Show in list page." },
                            showInAddForm = new { type = "boolean", description = "Show in add form." },
                            showInEditForm = new { type = "boolean", description = "Show in edit form." },
                            showInSearch = new { type = "boolean", description = "Show as search condition." },
                            showInDetail = new { type = "boolean", description = "Show in detail page." },
                            listWidth = new { type = "integer", description = "List column width in pixels." },
                            orderNum = new { type = "integer", description = "Field sort order." },
                            fieldCategory = new { type = "string", @enum = new[] { "Normal", "Computed", "Aggregate" }, description = "Normal business field, Computed formula field, or Aggregate child-statistics field." },
                            formula = new { type = "string", description = "Computed formula using [FieldName] references and arithmetic operators, for example [Price]*[Quantity]." },
                            aggregateType = new { type = "string", @enum = new[] { "Sum", "Avg", "Concat" }, description = "Aggregate calculation applied to a one-to-many child field." },
                            aggregateChildEntityId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Existing child entity id from an owned one-to-many relation." },
                            aggregateChildFieldName = new { type = "string", description = "Child source field. Sum/Avg require numeric data; Concat accepts text." },
                            aggregateSeparator = new { type = "string", description = "Optional separator used by Concat." },
                            remark = new { type = "string" }
                        }
                    }
                },
                relations = new
                {
                    type = "array",
                    description = "One-to-many child table relations on the parent entity. Existing relations are matched by id or childEntityName+foreignKey.",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } } },
                            childEntityId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } } },
                            childEntityName = new { type = "string", description = "Child entity name in PascalCase." },
                            foreignKey = new { type = "string", description = "Child foreign key field. Defaults to {parentEntityName}Id." },
                            masterField = new { type = "string", description = "Parent key field. Defaults to Id." },
                            orderNum = new { type = "integer", description = "Relation sort order." }
                        }
                    }
                },
                ownedOneRelations = new
                {
                    type = "array",
                    description = "Owned one-to-one composition relations. The target entity is saved as part of the source aggregate. Existing relations are matched by id or target entity plus field mapping.",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } }, description = "Existing relation id for updates." },
                            targetEntityId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } } },
                            targetEntityName = new { type = "string", description = "Owned target entity name. It may be in another module of the same project." },
                            relationName = new { type = "string", description = "Generated navigation/property name. Defaults to targetEntityName." },
                            sourceField = new { type = "string", description = "Source entity key field. Defaults to Id." },
                            targetField = new { type = "string", description = "Target foreign-key field. Defaults to {sourceEntityName}Id." },
                            isRequired = new { type = "boolean", description = "Require the owned object in create/update DTOs." },
                            deleteBehavior = new { type = "string", @enum = new[] { "delete", "keep", "restrict" }, description = "Behavior when an optional owned object is cleared. Defaults to delete." },
                            orderNum = new { type = "integer", description = "Relation display/generation order." }
                        }
                    }
                }
            },
            required = Array.Empty<string>()
        })!
    };

    public async Task<object?> HandleAsync(object? input)
    {
        var args = (EntityToolInput?)input ?? throw new ArgumentException("Invalid input");
        args.ProjectId = await McpProjectContextHelper.ResolveProjectIdAsync(_contextResolver, args.ProjectId, args.WorkspacePath);
        if (args.ProjectId <= 0)
            return new { success = false, message = "projectId is required." };

        var module = await ResolveOrCreateModuleAsync(args);
        if (module == null)
            return new { success = false, message = "moduleId or moduleName is required." };

        var entities = await _apiClient.GetAsync<List<ModuleEntityDto>>(
            $"/api/codegen/moduleentity/getbymoduleid/{module.Id}",
            args.WorkspacePath);
        var projectEntities = await _apiClient.GetAsync<List<ModuleEntityDto>>(
            $"/api/codegen/moduleentity/getlist?projectId={args.ProjectId}",
            args.WorkspacePath);
        var entity = await ResolveEntityAsync(args, entities);

        return entity == null
            ? await CreateEntityAsync(args, module.Id, entities, projectEntities)
            : await UpdateEntityAsync(args, entity.Id, module.Id, entities, projectEntities);
    }

    private async Task<ProjectModuleDto?> ResolveOrCreateModuleAsync(EntityToolInput args)
    {
        if (args.ModuleId > 0)
            return await _apiClient.GetAsync<ProjectModuleDto>(
                $"/api/codegen/projectmodule/getbyid/{args.ModuleId}",
                args.WorkspacePath);

        if (string.IsNullOrWhiteSpace(args.ModuleName))
            return null;

        args.ModuleName = CSharpModuleNameValidator.RequireValid(args.ModuleName);

        var modules = await _apiClient.GetAsync<List<ProjectModuleDto>>(
            $"/api/codegen/projectmodule/getbyprojectid/{args.ProjectId}",
            args.WorkspacePath);
        var module = modules.FirstOrDefault(m => string.Equals(m.ModuleName, args.ModuleName, StringComparison.OrdinalIgnoreCase));
        if (module != null)
            return module;

        var moduleId = await _apiClient.PostAsync<long>("/api/codegen/projectmodule/create", new CreateProjectModuleDto
        {
            ProjectId = args.ProjectId,
            ModuleName = args.ModuleName,
            ModuleDescription = args.ModuleDescription ?? args.ModuleName,
            Icon = args.ModuleIcon ?? "Document",
            OrderNum = args.ModuleOrderNum ?? 1,
            Remark = args.Description
        }, args.WorkspacePath);

        return (await _apiClient.GetAsync<List<ProjectModuleDto>>(
            $"/api/codegen/projectmodule/getbyprojectid/{args.ProjectId}",
            args.WorkspacePath)).First(m => m.Id == moduleId);
    }

    private async Task<ModuleEntityDto?> ResolveEntityAsync(EntityToolInput args, IReadOnlyCollection<ModuleEntityDto> moduleEntities)
    {
        if (args.EntityId > 0)
            return await _apiClient.GetAsync<ModuleEntityDto>(
                $"/api/codegen/moduleentity/getbyid/{args.EntityId}",
                args.WorkspacePath);

        if (string.IsNullOrWhiteSpace(args.EntityName))
            return null;

        return moduleEntities.FirstOrDefault(e => string.Equals(e.Name, args.EntityName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<object> CreateEntityAsync(
        EntityToolInput args,
        long moduleId,
        IReadOnlyCollection<ModuleEntityDto> moduleEntities,
        IReadOnlyCollection<ModuleEntityDto> projectEntities)
    {
        if (string.IsNullOrWhiteSpace(args.EntityName))
            return new { success = false, message = "entityName is required when creating an entity." };

        var fields = BuildCreateFieldDtos(args.Fields);
        var relationResult = BuildCreateRelations(args.EntityName, args.Relations, moduleEntities);
        if (relationResult.Error != null)
            return new { success = false, message = relationResult.Error };
        var ownedOneResult = BuildCreateOwnedOneRelations(args.EntityName, args.OwnedOneRelations, projectEntities);
        if (ownedOneResult.Error != null)
            return new { success = false, message = ownedOneResult.Error };

        var entityId = await _apiClient.PostAsync<long>("/api/codegen/moduleentity/create", new CreateModuleEntityDto
        {
            ProjectId = args.ProjectId,
            ModuleId = moduleId,
            Name = args.EntityName,
            TableName = args.TableName,
            Description = args.Description ?? args.EntityName,
            IsTree = args.IsTree ?? false,
            HasPrimaryKey = args.HasPrimaryKey ?? true,
            IsReadOnly = args.IsReadOnly ?? false,
            HasTenant = args.IsMultiTenant ?? false,
            HasDataPermission = args.HasDataPermission ?? false,
            HasAudit = args.HasAudit ?? true,
            HasSoftDelete = args.HasSoftDelete ?? true,
            GenerateFrontend = args.GenerateFrontend ?? true,
            IsChildTable = args.IsChildTable ?? false,
            FrontendRoute = args.FrontendRoute,
            MenuIcon = args.MenuIcon,
            OrderNum = args.OrderNum ?? 0,
            Remark = args.Remark,
            Fields = fields,
            OneToManyRelations = relationResult.Relations,
            EntityRelations = ownedOneResult.Relations
        }, args.WorkspacePath);

        return new
        {
            success = true,
            isNew = true,
            projectId = args.ProjectId.ToString(),
            moduleId = moduleId.ToString(),
            entityId = entityId.ToString(),
            entityName = args.EntityName,
            fieldsCreated = fields.Select(f => f.Name).ToList(),
            relationsCreated = relationResult.Relations.Select(r => new { childEntityId = r.ChildEntityId.ToString(), r.ChildEntityName, r.ChildForeignKey }).ToList(),
            ownedOneRelationsCreated = ownedOneResult.Relations.Select(r => new { targetEntityId = r.TargetEntityId.ToString(), r.RelationName, r.SourceField, r.TargetField }).ToList()
        };
    }

    private async Task<object> UpdateEntityAsync(
        EntityToolInput args,
        long entityId,
        long moduleId,
        IReadOnlyCollection<ModuleEntityDto> moduleEntities,
        IReadOnlyCollection<ModuleEntityDto> projectEntities)
    {
        var entity = await _apiClient.GetAsync<ModuleEntityDto>(
            $"/api/codegen/moduleentity/getbyid/{entityId}",
            args.WorkspacePath);
        if (entity == null)
            return new { success = false, message = $"Entity not found: {entityId}", entityId = entityId.ToString() };

        var existingFields = await _apiClient.GetAsync<List<EntityFieldDto>>(
            $"/api/codegen/entityfield/getbyentityid/{entityId}",
            args.WorkspacePath);
        var newFields = new List<CreateEntityFieldDto>();
        var updatedFields = new List<UpdateEntityFieldWithIdDto>();
        var fieldsSkipped = new List<string>();

        foreach (var field in args.Fields ?? new())
        {
            var existing = ResolveExistingField(field, existingFields);
            if (existing == null)
            {
                if (string.IsNullOrWhiteSpace(field.Name))
                {
                    fieldsSkipped.Add(field.Id > 0 ? field.Id.ToString() : "(missing name)");
                    continue;
                }

                newFields.Add(BuildCreateFieldDto(field));
            }
            else
            {
                updatedFields.Add(BuildUpdateFieldDto(field, existing));
            }
        }

        var relationResult = BuildRelationsForUpdate(entity, args.Relations, moduleEntities);
        if (relationResult.Error != null)
            return new { success = false, message = relationResult.Error, entityId = entityId.ToString() };
        var ownedOneResult = BuildOwnedOneRelationsForUpdate(entity, args.OwnedOneRelations, projectEntities);
        if (ownedOneResult.Error != null)
            return new { success = false, message = ownedOneResult.Error, entityId = entityId.ToString() };

        await _apiClient.PutAsync<int>($"/api/codegen/moduleentity/update/{entityId}", new UpdateModuleEntityDto
        {
            Name = args.EntityName ?? entity.Name,
            Description = args.Description ?? entity.Description,
            HasPrimaryKey = args.HasPrimaryKey ?? entity.HasPrimaryKey,
            TableName = args.TableName ?? entity.TableName,
            IsTree = args.IsTree ?? entity.IsTree,
            IsReadOnly = args.IsReadOnly ?? entity.IsReadOnly,
            HasTenant = args.IsMultiTenant ?? entity.HasTenant,
            HasDataPermission = args.HasDataPermission ?? entity.HasDataPermission,
            HasAudit = args.HasAudit ?? entity.HasAudit,
            HasSoftDelete = args.HasSoftDelete ?? entity.HasSoftDelete,
            GenerateFrontend = args.GenerateFrontend ?? entity.GenerateFrontend,
            IsChildTable = args.IsChildTable ?? entity.IsChildTable,
            FrontendRoute = args.FrontendRoute ?? entity.FrontendRoute,
            MenuIcon = args.MenuIcon ?? entity.MenuIcon,
            OrderNum = args.OrderNum ?? entity.OrderNum,
            Remark = args.Remark ?? entity.Remark,
            NewFields = newFields,
            UpdatedFields = updatedFields,
            DeletedFieldIds = new(),
            NewRelations = relationResult.NewRelations,
            UpdatedRelations = relationResult.UpdatedRelations,
            DeletedRelationIds = new(),
            NewEntityRelations = ownedOneResult.NewRelations,
            UpdatedEntityRelations = ownedOneResult.UpdatedRelations,
            DeletedEntityRelationIds = new()
        }, args.WorkspacePath);

        return new
        {
            success = true,
            isNew = false,
            projectId = args.ProjectId.ToString(),
            moduleId = moduleId.ToString(),
            entityId = entityId.ToString(),
            entityName = args.EntityName ?? entity.Name,
            fieldsCreated = newFields.Select(f => f.Name).ToList(),
            fieldsUpdated = updatedFields.Select(f => new { id = f.Id.ToString(), f.Name }).ToList(),
            fieldsSkipped,
            relationsCreated = relationResult.NewRelations.Select(r => new { childEntityId = r.ChildEntityId.ToString(), r.ChildEntityName, r.ChildForeignKey }).ToList(),
            relationsUpdated = relationResult.UpdatedRelations.Select(r => new { id = r.Id.ToString(), childEntityId = r.ChildEntityId.ToString(), r.ChildEntityName, r.ChildForeignKey }).ToList(),
            ownedOneRelationsCreated = ownedOneResult.NewRelations.Select(r => new { targetEntityId = r.TargetEntityId.ToString(), r.RelationName, r.SourceField, r.TargetField }).ToList(),
            ownedOneRelationsUpdated = ownedOneResult.UpdatedRelations.Select(r => new { id = r.Id.ToString(), targetEntityId = r.TargetEntityId.ToString(), r.RelationName, r.SourceField, r.TargetField }).ToList()
        };
    }

    private static EntityFieldDto? ResolveExistingField(FieldInput field, IReadOnlyCollection<EntityFieldDto> existingFields)
    {
        if (field.Id > 0)
            return existingFields.FirstOrDefault(f => f.Id == field.Id);

        return string.IsNullOrWhiteSpace(field.Name)
            ? null
            : existingFields.FirstOrDefault(f => string.Equals(f.Name, field.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static List<CreateEntityFieldDto> BuildCreateFieldDtos(List<FieldInput>? fields) =>
        fields?.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Select(BuildCreateFieldDto).ToList() ?? new();

    private static CreateEntityFieldDto BuildCreateFieldDto(FieldInput field)
    {
        var normalized = NormalizeDataType(field.DataType, field.MaxLength, field.Precision, field.Scale);
        var controlType = field.FormControlType ?? DefaultControlType(normalized.DataType, field.EnumValues ?? field.SelectOptions);

        return new CreateEntityFieldDto
        {
            Name = field.Name ?? string.Empty,
            Description = field.Description ?? field.Name ?? string.Empty,
            DataType = normalized.DataType,
            MaxLength = normalized.MaxLength,
            Precision = normalized.Precision,
            Scale = normalized.Scale,
            IsRequired = field.IsRequired ?? false,
            IsNullable = field.IsNullable ?? !(field.IsRequired ?? false),
            DefaultValue = field.DefaultValue,
            MinValue = field.MinValue ?? field.MinLength?.ToString(),
            MaxValue = field.MaxValue,
            RegexPattern = field.RegexPattern,
            IsEmail = field.IsEmail ?? false,
            IsPhone = field.IsPhone ?? false,
            ShowInList = field.ShowInList ?? false,
            ShowInAddForm = field.ShowInAddForm ?? true,
            ShowInEditForm = field.ShowInEditForm ?? true,
            ShowInSearch = field.ShowInSearch ?? false,
            ShowInDetail = field.ShowInDetail ?? true,
            FormControlType = controlType,
            SelectDataSource = field.SelectDataSource,
            SelectOptions = field.SelectOptions ?? field.EnumValues,
            IsMultiple = field.IsMultiple ?? false,
            RelatedEntityName = field.RelatedEntityName,
            RelatedEntityIdField = string.IsNullOrWhiteSpace(field.RelatedEntityName) ? null : field.RelatedEntityIdField ?? "Id",
            RelatedEntityDisplayFields = BuildRelatedDisplayFields(field),
            ResultMappings = BuildResultMappings(field),
            OrderNum = field.OrderNum ?? 0,
            ListWidth = field.ListWidth,
            FieldCategory = field.FieldCategory ?? "Normal",
            Formula = field.Formula,
            AggregateType = field.AggregateType,
            AggregateChildEntityId = field.AggregateChildEntityId,
            AggregateChildFieldName = field.AggregateChildFieldName,
            AggregateSeparator = field.AggregateSeparator,
            Remark = field.Remark ?? field.Description
        };
    }

    private static UpdateEntityFieldWithIdDto BuildUpdateFieldDto(FieldInput field, EntityFieldDto existing)
    {
        var normalized = NormalizeDataType(field.DataType ?? existing.DataType, field.MaxLength ?? existing.MaxLength, field.Precision ?? existing.Precision, field.Scale ?? existing.Scale);

        return new UpdateEntityFieldWithIdDto
        {
            Id = existing.Id,
            Name = string.IsNullOrWhiteSpace(field.Name) ? existing.Name : field.Name!,
            Description = field.Description ?? existing.Description,
            DataType = normalized.DataType,
            MaxLength = normalized.MaxLength,
            Precision = normalized.Precision,
            Scale = normalized.Scale,
            IsNullable = field.IsNullable ?? existing.IsNullable,
            DefaultValue = field.DefaultValue ?? existing.DefaultValue,
            IsIgnore = existing.IsIgnore,
            IsPrimaryKey = existing.IsPrimaryKey,
            IsRequired = field.IsRequired ?? existing.IsRequired,
            MinValue = field.MinValue ?? field.MinLength?.ToString() ?? existing.MinValue,
            MaxValue = field.MaxValue ?? existing.MaxValue,
            RegexPattern = field.RegexPattern ?? existing.RegexPattern,
            IsEmail = field.IsEmail ?? existing.IsEmail,
            IsPhone = field.IsPhone ?? existing.IsPhone,
            ShowInList = field.ShowInList ?? existing.ShowInList,
            ShowInAddForm = field.ShowInAddForm ?? existing.ShowInAddForm,
            ShowInEditForm = field.ShowInEditForm ?? existing.ShowInEditForm,
            ShowInSearch = field.ShowInSearch ?? existing.ShowInSearch,
            ShowInDetail = field.ShowInDetail ?? existing.ShowInDetail,
            FormControlType = field.FormControlType ?? existing.FormControlType,
            ListWidth = field.ListWidth ?? existing.ListWidth,
            OrderNum = field.OrderNum ?? existing.OrderNum,
            SelectDataSource = field.SelectDataSource ?? existing.SelectDataSource,
            SelectOptions = field.SelectOptions ?? field.EnumValues ?? existing.SelectOptions,
            IsMultiple = field.IsMultiple ?? existing.IsMultiple,
            RelatedEntityName = field.RelatedEntityName ?? existing.RelatedEntityName,
            RelatedEntityIdField = field.RelatedEntityIdField ?? existing.RelatedEntityIdField,
            RelatedEntityDisplayFields = BuildRelatedDisplayFields(field) ?? existing.RelatedEntityDisplayFields,
            ResultMappings = field.ResultMappings == null ? existing.ResultMappings : BuildResultMappings(field),
            FieldCategory = field.FieldCategory ?? existing.FieldCategory,
            Formula = field.Formula ?? existing.Formula,
            AggregateType = field.AggregateType ?? existing.AggregateType,
            AggregateChildEntityId = field.AggregateChildEntityId ?? existing.AggregateChildEntityId,
            AggregateChildFieldName = field.AggregateChildFieldName ?? existing.AggregateChildFieldName,
            AggregateSeparator = field.AggregateSeparator ?? existing.AggregateSeparator,
            Remark = field.Remark ?? existing.Remark
        };
    }

    private static (List<CreateOneToManyRelationDto> Relations, string? Error) BuildCreateRelations(
        string parentEntityName,
        List<RelationInput>? relations,
        IReadOnlyCollection<ModuleEntityDto> moduleEntities)
    {
        var result = new List<CreateOneToManyRelationDto>();
        if (relations == null || relations.Count == 0)
            return (result, null);

        for (var index = 0; index < relations.Count; index++)
        {
            var relation = relations[index];
            var resolved = ResolveRelationChild(relation, moduleEntities);
            if (resolved.Error != null)
                return (new List<CreateOneToManyRelationDto>(), resolved.Error);

            result.Add(new CreateOneToManyRelationDto
            {
                MasterField = relation.MasterField ?? "Id",
                ChildEntityId = resolved.ChildEntity!.Id,
                ChildEntityName = resolved.ChildEntity.Name,
                ChildForeignKey = relation.ForeignKey ?? $"{parentEntityName}Id",
                OrderNum = relation.OrderNum ?? index + 1
            });
        }

        return (result, null);
    }

    private static (List<CreateOneToManyRelationDto> NewRelations, List<UpdateOneToManyRelationWithIdDto> UpdatedRelations, string? Error) BuildRelationsForUpdate(
        ModuleEntityDto entity,
        List<RelationInput>? relations,
        IReadOnlyCollection<ModuleEntityDto> moduleEntities)
    {
        var newRelations = new List<CreateOneToManyRelationDto>();
        var updatedRelations = new List<UpdateOneToManyRelationWithIdDto>();
        if (relations == null || relations.Count == 0)
            return (newRelations, updatedRelations, null);

        var existingRelations = entity.OneToManyRelations ?? new();
        for (var index = 0; index < relations.Count; index++)
        {
            var relation = relations[index];
            var resolved = ResolveRelationChild(relation, moduleEntities);
            if (resolved.Error != null)
                return (newRelations, updatedRelations, resolved.Error);

            var child = resolved.ChildEntity!;
            var foreignKey = relation.ForeignKey ?? $"{entity.Name}Id";
            var existing = relation.Id > 0
                ? existingRelations.FirstOrDefault(r => r.Id == relation.Id)
                : existingRelations.FirstOrDefault(r =>
                    string.Equals(r.ChildEntityName, child.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.ChildForeignKey, foreignKey, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                newRelations.Add(new CreateOneToManyRelationDto
                {
                    MasterField = relation.MasterField ?? "Id",
                    ChildEntityId = child.Id,
                    ChildEntityName = child.Name,
                    ChildForeignKey = foreignKey,
                    OrderNum = relation.OrderNum ?? index + 1
                });
            }
            else
            {
                updatedRelations.Add(new UpdateOneToManyRelationWithIdDto
                {
                    Id = existing.Id,
                    MasterField = relation.MasterField ?? existing.MasterField,
                    ChildEntityId = child.Id,
                    ChildEntityName = child.Name,
                    ChildForeignKey = foreignKey,
                    OrderNum = relation.OrderNum ?? existing.OrderNum
                });
            }
        }

        return (newRelations, updatedRelations, null);
    }

    private static (ModuleEntityDto? ChildEntity, string? Error) ResolveRelationChild(RelationInput relation, IReadOnlyCollection<ModuleEntityDto> moduleEntities)
    {
        var child = relation.ChildEntityId > 0
            ? moduleEntities.FirstOrDefault(e => e.Id == relation.ChildEntityId)
            : moduleEntities.FirstOrDefault(e => string.Equals(e.Name, relation.ChildEntityName, StringComparison.OrdinalIgnoreCase));

        if (child == null)
            return (null, $"Child entity '{relation.ChildEntityName}' was not found in the target module. Create the child entity first, then add the one-to-many relation.");

        return (child, null);
    }

    private static (List<CreateEntityRelationDto> Relations, string? Error) BuildCreateOwnedOneRelations(
        string sourceEntityName,
        List<OwnedOneRelationInput>? relations,
        IReadOnlyCollection<ModuleEntityDto> projectEntities)
    {
        var result = new List<CreateEntityRelationDto>();
        if (relations == null || relations.Count == 0)
            return (result, null);

        for (var index = 0; index < relations.Count; index++)
        {
            var resolved = ResolveOwnedOneTarget(relations[index], projectEntities);
            if (resolved.Error != null)
                return (new List<CreateEntityRelationDto>(), resolved.Error);

            result.Add(BuildCreateOwnedOneRelation(sourceEntityName, relations[index], resolved.TargetEntity!, index));
        }

        return (result, null);
    }

    private static (List<CreateEntityRelationDto> NewRelations, List<UpdateEntityRelationWithIdDto> UpdatedRelations, string? Error) BuildOwnedOneRelationsForUpdate(
        ModuleEntityDto entity,
        List<OwnedOneRelationInput>? relations,
        IReadOnlyCollection<ModuleEntityDto> projectEntities)
    {
        var newRelations = new List<CreateEntityRelationDto>();
        var updatedRelations = new List<UpdateEntityRelationWithIdDto>();
        if (relations == null || relations.Count == 0)
            return (newRelations, updatedRelations, null);

        var existingRelations = entity.EntityRelations ?? new();
        for (var index = 0; index < relations.Count; index++)
        {
            var input = relations[index];
            var resolved = ResolveOwnedOneTarget(input, projectEntities);
            if (resolved.Error != null)
                return (newRelations, updatedRelations, resolved.Error);

            var target = resolved.TargetEntity!;
            var sourceField = input.SourceField ?? "Id";
            var targetField = input.TargetField ?? $"{entity.Name}Id";
            var existing = input.Id > 0
                ? existingRelations.FirstOrDefault(item => item.Id == input.Id)
                : existingRelations.FirstOrDefault(item =>
                    item.TargetEntityId == target.Id &&
                    string.Equals(item.SourceField, sourceField, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.TargetField, targetField, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                newRelations.Add(BuildCreateOwnedOneRelation(entity.Name, input, target, index));
                continue;
            }

            updatedRelations.Add(new UpdateEntityRelationWithIdDto
            {
                Id = existing.Id,
                TargetEntityId = target.Id,
                RelationName = input.RelationName ?? existing.RelationName,
                SourceField = sourceField,
                TargetField = targetField,
                Cardinality = EntityRelationCardinality.OneToOne,
                Ownership = EntityRelationOwnership.Owned,
                IsRequired = input.IsRequired ?? existing.IsRequired,
                DeleteBehavior = ParseDeleteBehavior(input.DeleteBehavior, existing.DeleteBehavior),
                OrderNum = input.OrderNum ?? existing.OrderNum
            });
        }

        return (newRelations, updatedRelations, null);
    }

    private static CreateEntityRelationDto BuildCreateOwnedOneRelation(
        string sourceEntityName,
        OwnedOneRelationInput input,
        ModuleEntityDto target,
        int index)
    {
        return new CreateEntityRelationDto
        {
            TargetEntityId = target.Id,
            RelationName = input.RelationName ?? target.Name,
            SourceField = input.SourceField ?? "Id",
            TargetField = input.TargetField ?? $"{sourceEntityName}Id",
            Cardinality = EntityRelationCardinality.OneToOne,
            Ownership = EntityRelationOwnership.Owned,
            IsRequired = input.IsRequired ?? false,
            DeleteBehavior = ParseDeleteBehavior(input.DeleteBehavior, EntityRelationDeleteBehavior.Delete),
            OrderNum = input.OrderNum ?? index + 1
        };
    }

    private static (ModuleEntityDto? TargetEntity, string? Error) ResolveOwnedOneTarget(
        OwnedOneRelationInput relation,
        IReadOnlyCollection<ModuleEntityDto> projectEntities)
    {
        var target = relation.TargetEntityId > 0
            ? projectEntities.FirstOrDefault(entity => entity.Id == relation.TargetEntityId)
            : projectEntities.FirstOrDefault(entity => string.Equals(entity.Name, relation.TargetEntityName, StringComparison.OrdinalIgnoreCase));

        return target == null
            ? (null, $"Owned target entity '{relation.TargetEntityName}' was not found in the project. Create the target entity first, then add the owned one-to-one relation.")
            : (target, null);
    }

    private static EntityRelationDeleteBehavior ParseDeleteBehavior(
        string? value,
        EntityRelationDeleteBehavior fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Trim().ToLowerInvariant() switch
        {
            "delete" => EntityRelationDeleteBehavior.Delete,
            "keep" => EntityRelationDeleteBehavior.Keep,
            "restrict" => EntityRelationDeleteBehavior.Restrict,
            _ => throw new ArgumentException($"Unsupported owned relation deleteBehavior: {value}")
        };
    }

    private static string? BuildRelatedDisplayFields(FieldInput field)
    {
        if (field.RelatedDisplayFields is { Count: > 0 })
            return JsonSerializer.Serialize(field.RelatedDisplayFields);

        return string.IsNullOrWhiteSpace(field.RelatedEntityDisplayFields)
            ? null
            : field.RelatedEntityDisplayFields;
    }

    private static string? BuildResultMappings(FieldInput field) =>
        field.ResultMappings == null ? null : JsonSerializer.Serialize(field.ResultMappings);

    private static (string DataType, int? MaxLength, int? Precision, int? Scale) NormalizeDataType(string? type, int? maxLength, int? precision, int? scale)
    {
        return (type ?? "string").ToLowerInvariant() switch
        {
            "string" => ("string", maxLength is > 0 ? maxLength : 256, null, null),
            "long" => ("long", null, null, null),
            "int" or "integer" => ("int", null, null, null),
            "decimal" => ("decimal", null, precision is > 0 ? precision : 18, scale is > 0 ? scale : 2),
            "datetime" or "date" => ("DateTime", null, null, null),
            "bool" or "boolean" => ("bool", null, null, null),
            "enum" => ("string", maxLength is > 0 ? maxLength : 128, null, null),
            "text" => ("string", 4000, null, null),
            "guid" => ("Guid", null, null, null),
            _ => ("string", maxLength is > 0 ? maxLength : 256, precision, scale)
        };
    }

    private static string DefaultControlType(string dataType, string? selectOptions)
    {
        if (!string.IsNullOrWhiteSpace(selectOptions))
            return "select";

        return dataType.ToLowerInvariant() switch
        {
            "long" or "int" or "decimal" => "number",
            "datetime" => "datetime",
            "bool" or "boolean" => "switch",
            _ => "input"
        };
    }
}

public class EntityToolInput
{
    public long ProjectId { get; set; }
    public long ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public string? ModuleDescription { get; set; }
    public string? ModuleIcon { get; set; }
    public int? ModuleOrderNum { get; set; }
    public long EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Description { get; set; }
    public string? TableName { get; set; }
    public bool? HasPrimaryKey { get; set; }
    public bool? IsTree { get; set; }
    public bool? IsReadOnly { get; set; }
    public bool? IsMultiTenant { get; set; }
    public bool? HasDataPermission { get; set; }
    public bool? HasAudit { get; set; }
    public bool? HasSoftDelete { get; set; }
    public bool? GenerateFrontend { get; set; }
    public bool? IsChildTable { get; set; }
    public string? FrontendRoute { get; set; }
    public string? MenuIcon { get; set; }
    public int? OrderNum { get; set; }
    public string? Remark { get; set; }
    public string? WorkspacePath { get; set; }
    public List<FieldInput>? Fields { get; set; }
    public List<RelationInput>? Relations { get; set; }
    public List<OwnedOneRelationInput>? OwnedOneRelations { get; set; }
}

public class FieldInput
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? DataType { get; set; }
    public string? Description { get; set; }
    public int? MaxLength { get; set; }
    public int? MinLength { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsNullable { get; set; }
    public string? DefaultValue { get; set; }
    public string? RegexPattern { get; set; }
    public bool? IsEmail { get; set; }
    public bool? IsPhone { get; set; }
    public string? EnumValues { get; set; }
    public string? SelectDataSource { get; set; }
    public string? SelectOptions { get; set; }
    public string? FormControlType { get; set; }
    public string? RelatedEntityName { get; set; }
    public string? RelatedEntityIdField { get; set; }
    public List<string>? RelatedDisplayFields { get; set; }
    public string? RelatedEntityDisplayFields { get; set; }
    public List<ResultMappingInput>? ResultMappings { get; set; }
    public bool? IsMultiple { get; set; }
    public bool? ShowInList { get; set; }
    public bool? ShowInAddForm { get; set; }
    public bool? ShowInEditForm { get; set; }
    public bool? ShowInSearch { get; set; }
    public bool? ShowInDetail { get; set; }
    public int? ListWidth { get; set; }
    public int? OrderNum { get; set; }
    public string? FieldCategory { get; set; }
    public string? Formula { get; set; }
    public string? AggregateType { get; set; }
    public long? AggregateChildEntityId { get; set; }
    public string? AggregateChildFieldName { get; set; }
    public string? AggregateSeparator { get; set; }
    public string? Remark { get; set; }
}

public class RelationInput
{
    public long Id { get; set; }
    public long ChildEntityId { get; set; }
    public string? ChildEntityName { get; set; }
    public string? ForeignKey { get; set; }
    public string? MasterField { get; set; }
    public int? OrderNum { get; set; }
}

public class OwnedOneRelationInput
{
    public long Id { get; set; }
    public long TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }
    public string? RelationName { get; set; }
    public string? SourceField { get; set; }
    public string? TargetField { get; set; }
    public bool? IsRequired { get; set; }
    public string? DeleteBehavior { get; set; }
    public int? OrderNum { get; set; }
}

public class ResultMappingInput
{
    public string? SourceField { get; set; }
    public string? TargetField { get; set; }
}
