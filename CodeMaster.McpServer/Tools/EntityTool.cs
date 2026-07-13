using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;

namespace CodeMaster.McpServer.Tools;

/// <summary>
/// MCP tool for creating or updating CodeMaster entity metadata.
/// </summary>
public class EntityTool
{
    private readonly IProjectModuleService _moduleService;
    private readonly IModuleEntityService _entityService;
    private readonly IEntityFieldService _fieldService;

    public EntityTool(
        IProjectModuleService moduleService,
        IModuleEntityService entityService,
        IEntityFieldService fieldService)
    {
        _moduleService = moduleService;
        _entityService = entityService;
        _fieldService = fieldService;
    }

    public static McpTool Definition => new()
    {
        Name = "create_or_update_entity",
        Description = "Create or update CodeMaster entity metadata through shared services. Supports field control configuration updates. Does not delete fields or relations unless future tools explicitly support deletion.",
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
                tableName = new { type = "string", description = "Optional table name. Leave empty for generated snake_case." },
                isTree = new { type = "boolean", description = "Generate a tree entity." },
                isMultiTenant = new { type = "boolean", description = "Enable tenant fields and filters." },
                hasDataPermission = new { type = "boolean", description = "Enable department data permission." },
                hasAudit = new { type = "boolean", description = "Enable audit fields. Defaults to true for new entities." },
                hasSoftDelete = new { type = "boolean", description = "Enable soft delete. Defaults to true for new entities." },
                generateFrontend = new { type = "boolean", description = "Generate Vue pages. Defaults to true for new entities." },
                isChildTable = new { type = "boolean", description = "Mark this entity as a child table." },
                frontendRoute = new { type = "string" },
                menuIcon = new { type = "string" },
                orderNum = new { type = "integer" },
                remark = new { type = "string" },
                fields = new
                {
                    type = "array",
                    description = "Business fields. Do not include Id, audit, soft-delete, or tenant fields. Existing fields are matched by id first, then name.",
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
                            formControlType = new { type = "string", description = "input, textarea, number, select, select-table, date, datetime, switch, file, image, or table-column." },
                            relatedEntityName = new { type = "string", description = "Related entity name for select-table." },
                            relatedEntityIdField = new { type = "string", description = "Related entity id field for select-table. Defaults to Id." },
                            relatedDisplayFields = new { type = "array", items = new { type = "string" }, description = "Related display field names, for example [\"Name\", \"Type\"]." },
                            relatedEntityDisplayFields = new { type = "string", description = "Raw JSON array for related display fields." },
                            isMultiple = new { type = "boolean", description = "Allow multiple values for select or select-table." },
                            showInList = new { type = "boolean", description = "Show in list page." },
                            showInAddForm = new { type = "boolean", description = "Show in add form." },
                            showInEditForm = new { type = "boolean", description = "Show in edit form." },
                            showInSearch = new { type = "boolean", description = "Show as search condition." },
                            showInDetail = new { type = "boolean", description = "Show in detail page." },
                            listWidth = new { type = "integer", description = "List column width in pixels." },
                            orderNum = new { type = "integer", description = "Field sort order." },
                            fieldCategory = new { type = "string", description = "Normal, Formula, Aggregate, etc." },
                            formula = new { type = "string" },
                            aggregateType = new { type = "string" },
                            aggregateChildEntityId = new { oneOf = new object[] { new { type = "integer" }, new { type = "string" } } },
                            aggregateChildFieldName = new { type = "string" },
                            aggregateSeparator = new { type = "string" },
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
                }
            },
            required = new[] { "projectId" }
        })!
    };

    public async Task<object?> HandleAsync(object? input)
    {
        var args = (EntityToolInput?)input ?? throw new ArgumentException("Invalid input");
        if (args.ProjectId <= 0)
            return new { success = false, message = "projectId is required." };

        var module = await ResolveOrCreateModuleAsync(args);
        if (module == null)
            return new { success = false, message = "moduleId or moduleName is required." };

        var entities = await _entityService.GetByModuleIdAsync(module.Id);
        var entity = await ResolveEntityAsync(args, entities);

        return entity == null
            ? await CreateEntityAsync(args, module.Id, entities)
            : await UpdateEntityAsync(args, entity.Id, module.Id, entities);
    }

    private async Task<ProjectModuleDto?> ResolveOrCreateModuleAsync(EntityToolInput args)
    {
        if (args.ModuleId > 0)
            return await _moduleService.GetByIdAsync(args.ModuleId);

        if (string.IsNullOrWhiteSpace(args.ModuleName))
            return null;

        var modules = await _moduleService.GetByProjectIdAsync(args.ProjectId);
        var module = modules.FirstOrDefault(m => string.Equals(m.ModuleName, args.ModuleName, StringComparison.OrdinalIgnoreCase));
        if (module != null)
            return module;

        var moduleId = await _moduleService.CreateAsync(new CreateProjectModuleDto
        {
            ProjectId = args.ProjectId,
            ModuleName = args.ModuleName,
            ModuleDescription = args.ModuleDescription ?? args.ModuleName,
            Icon = args.ModuleIcon ?? "Document",
            OrderNum = args.ModuleOrderNum ?? 1,
            Remark = args.Description
        });

        return (await _moduleService.GetByProjectIdAsync(args.ProjectId)).First(m => m.Id == moduleId);
    }

    private async Task<ModuleEntityDto?> ResolveEntityAsync(EntityToolInput args, IReadOnlyCollection<ModuleEntityDto> moduleEntities)
    {
        if (args.EntityId > 0)
            return await _entityService.GetByIdAsync(args.EntityId);

        if (string.IsNullOrWhiteSpace(args.EntityName))
            return null;

        return moduleEntities.FirstOrDefault(e => string.Equals(e.Name, args.EntityName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<object> CreateEntityAsync(EntityToolInput args, long moduleId, IReadOnlyCollection<ModuleEntityDto> moduleEntities)
    {
        if (string.IsNullOrWhiteSpace(args.EntityName))
            return new { success = false, message = "entityName is required when creating an entity." };

        var fields = BuildCreateFieldDtos(args.Fields);
        var relationResult = BuildCreateRelations(args.EntityName, args.Relations, moduleEntities);
        if (relationResult.Error != null)
            return new { success = false, message = relationResult.Error };

        var entityId = await _entityService.CreateAsync(new CreateModuleEntityDto
        {
            ProjectId = args.ProjectId,
            ModuleId = moduleId,
            Name = args.EntityName,
            TableName = args.TableName,
            Description = args.Description ?? args.EntityName,
            IsTree = args.IsTree ?? false,
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
            OneToManyRelations = relationResult.Relations
        });

        return new
        {
            success = true,
            isNew = true,
            projectId = args.ProjectId.ToString(),
            moduleId = moduleId.ToString(),
            entityId = entityId.ToString(),
            entityName = args.EntityName,
            fieldsCreated = fields.Select(f => f.Name).ToList(),
            relationsCreated = relationResult.Relations.Select(r => new { childEntityId = r.ChildEntityId.ToString(), r.ChildEntityName, r.ChildForeignKey }).ToList()
        };
    }

    private async Task<object> UpdateEntityAsync(EntityToolInput args, long entityId, long moduleId, IReadOnlyCollection<ModuleEntityDto> moduleEntities)
    {
        var entity = await _entityService.GetByIdAsync(entityId);
        if (entity == null)
            return new { success = false, message = $"Entity not found: {entityId}", entityId = entityId.ToString() };

        var existingFields = await _fieldService.GetByEntityIdAsync(entityId);
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

        await _entityService.UpdateAsync(entityId, new UpdateModuleEntityDto
        {
            Name = args.EntityName ?? entity.Name,
            Description = args.Description ?? entity.Description,
            HasPrimaryKey = entity.HasPrimaryKey,
            TableName = args.TableName ?? entity.TableName,
            IsTree = args.IsTree ?? entity.IsTree,
            IsReadOnly = args.IsReadOnly ?? entity.IsReadOnly,
            HasTenant = args.IsMultiTenant ?? entity.HasTenant,
            HasDataPermission = args.HasDataPermission ?? entity.HasDataPermission,
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
            DeletedRelationIds = new()
        });

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
            relationsUpdated = relationResult.UpdatedRelations.Select(r => new { id = r.Id.ToString(), childEntityId = r.ChildEntityId.ToString(), r.ChildEntityName, r.ChildForeignKey }).ToList()
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

    private static string? BuildRelatedDisplayFields(FieldInput field)
    {
        if (field.RelatedDisplayFields is { Count: > 0 })
            return JsonSerializer.Serialize(field.RelatedDisplayFields);

        return string.IsNullOrWhiteSpace(field.RelatedEntityDisplayFields)
            ? null
            : field.RelatedEntityDisplayFields;
    }

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
    public List<FieldInput>? Fields { get; set; }
    public List<RelationInput>? Relations { get; set; }
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
