using CodeMaster.Domain.Entities.CodeGen;
using Scriban;
using Scriban.Runtime;
using SqlSugar;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// </summary>
public class CodeGeneratorService
{
    private readonly ISqlSugarClient _db;
    private readonly string _templatePath;

    private static readonly Dictionary<string, HashSet<string>> InterfaceFields = new()
    {
        ["IEntity"] = new() { "Id" },
        ["ITree"] = new() { "ParentId" },
        ["ITenant"] = new() { "TenantId" },
        ["IDept"] = new() { "DeptId", "DeptAncestors", "CreateUserId" },
        ["IAuditEntity"] = new() { "CreateUserId", "CreateBy", "CreateTime", "UpdateUserId", "UpdateBy", "UpdateTime" },
        ["ISoftDelete"] = new() { "IsDeleted", "DeleteTime", "DeleteBy", "DeleteUserId" }
    };

    private static readonly Dictionary<string, HashSet<string>> BaseClassFields = new()
    {
        ["EntityBase"] = new() { "Id", "CreateUserId", "CreateBy", "CreateTime", "UpdateUserId", "UpdateBy", "UpdateTime", "IsDeleted", "DeleteTime", "DeleteBy", "DeleteUserId", "Remark" },
        ["EntityBaseWithTenant"] = new() { "Id", "CreateUserId", "CreateBy", "CreateTime", "UpdateUserId", "UpdateBy", "UpdateTime", "IsDeleted", "DeleteTime", "DeleteBy", "DeleteUserId", "Remark", "TenantId" },
        ["EntityBaseWithDept"] = new() { "Id", "CreateUserId", "CreateBy", "CreateTime", "UpdateUserId", "UpdateBy", "UpdateTime", "IsDeleted", "DeleteTime", "DeleteBy", "DeleteUserId", "Remark", "TenantId", "DeptId", "DeptAncestors" },
        ["TreeEntityBase"] = new() { "Id", "CreateUserId", "CreateBy", "CreateTime", "UpdateUserId", "UpdateBy", "UpdateTime", "IsDeleted", "DeleteTime", "DeleteBy", "DeleteUserId", "Remark", "ParentId", "Ancestors" },
        ["TreeEntityBaseWithTenant"] = new() { "Id", "CreateUserId", "CreateBy", "CreateTime", "UpdateUserId", "UpdateBy", "UpdateTime", "IsDeleted", "DeleteTime", "DeleteBy", "DeleteUserId", "Remark", "ParentId", "Ancestors", "TenantId" },
    };

    public CodeGeneratorService(ISqlSugarClient db)
    {
        _db = db;
        _templatePath = ResolveTemplatePath();
    }

    #region Public methods

    /// <summary>
    /// </summary>
    public void ValidateConstraints(ModuleEntity entity, List<EntityField> fields)
    {
        if (!entity.IsReadOnly && !entity.HasPrimaryKey)
        {
            throw new Exception($"实体 {entity.Name} 非只读但未设置主键（HasPrimaryKey 必须为 true）");
        }

        foreach (var field in fields.Where(f => f.IsMultiple))
        {
            if (IsNumericDataType(field.DataType))
            {
                if (field.FormControlType == "select-table" || field.FormControlType == "cascader")
                {
                    continue;
                }
                throw new Exception($"字段 {field.Name} 为多选字段，数据类型不能为数值类型（当前: {field.DataType}），必须使用 string");
            }
        }

        foreach (var field in fields.Where(f => f.FormControlType == "cascader" && !string.IsNullOrEmpty(f.RelatedEntityName)))
        {
            var relatedEntity = _db.Queryable<ModuleEntity>()
                .Where(e => e.Name == field.RelatedEntityName)
                .First();

            if (relatedEntity != null && !relatedEntity.IsTree)
            {
                throw new Exception($"字段 {field.Name} 使用 cascader 控件，但关联表 {field.RelatedEntityName} 不是树形结构（IsTree 必须为 true）");
            }

            if (relatedEntity == null &&
                SystemReferenceEntityCatalog.TryGet(field.RelatedEntityName, out var referenceEntity) &&
                !referenceEntity.IsTree)
            {
                throw new Exception($"字段 {field.Name} 使用 cascader 控件，但关联源 {field.RelatedEntityName} 不是树形结构（IsTree 必须为 true）");
            }
        }
    }

    /// <summary>
    /// </summary>
    public string CalculateInterfaceList(ModuleEntity entity, bool excludeFramework = false, bool excludeEntity = false)
    {
        var interfaces = new List<string>();

        if (entity.HasPrimaryKey && !excludeEntity)
            interfaces.Add("IEntity");
        if (!excludeEntity)
        {
            if (entity.IsTree) interfaces.Add("ITree");
            if (entity.HasTenant) interfaces.Add("ITenant");
            if (entity.HasDataPermission) interfaces.Add("IDept");
            if (!excludeFramework)
            {
                if (entity.HasAudit) interfaces.Add("IAuditEntity");
                if (entity.HasSoftDelete) interfaces.Add("ISoftDelete");
            }
        }

        return interfaces.Count > 0 ? ", " + string.Join(", ", interfaces) : "";
    }

    /// <summary>
    /// </summary>
    public string CalculateBaseClassName(ModuleEntity entity)
    {
        return "EntityBase";
    }

    /// <summary>
    /// </summary>
    public bool IsInterfaceField(string fieldName, ModuleEntity entity, string? baseClassName = null)
    {
        if (baseClassName != null && BaseClassFields.TryGetValue(baseClassName, out var baseFields) && baseFields.Contains(fieldName))
            return true;

        if (entity.HasPrimaryKey && InterfaceFields["IEntity"].Contains(fieldName))
            return true;
        if (entity.IsTree && InterfaceFields["ITree"].Contains(fieldName))
            return true;
        if (entity.HasTenant && InterfaceFields["ITenant"].Contains(fieldName))
            return true;
        if (entity.HasDataPermission && InterfaceFields["IDept"].Contains(fieldName))
            return true;
        if (entity.HasAudit && InterfaceFields["IAuditEntity"].Contains(fieldName))
            return true;
        if (entity.HasSoftDelete && InterfaceFields["ISoftDelete"].Contains(fieldName))
            return true;

        return false;
    }

    private static string GetGenerationDataType(EntityField field)
    {
        var dataType = field.DataType?.TrimEnd('?') ?? "string";
        if (field.IsMultiple &&
            (field.FormControlType == "select-table" || field.FormControlType == "cascader") &&
            IsNumericDataType(dataType))
        {
            return "string";
        }

        return dataType;
    }

    private static bool IsNumericDataType(string? dataType)
    {
        var normalized = dataType?.TrimEnd('?');
        return normalized is "int" or "long" or "decimal" or "double" or "float" or "short" or "byte";
    }

    /// <summary>
    /// </summary>
    public ScriptObject BuildTemplateContext(
        ModuleEntity entity,
        List<EntityField> fields,
        List<OneToManyRelation> relations,
        string projectName,
        string moduleName)
    {
        var baseClassName = CalculateBaseClassName(entity);
        var interfaceList = CalculateInterfaceList(entity);
        var dtoInterfaceList = CalculateInterfaceList(entity, excludeFramework: true, excludeEntity: true);
        var createUpdateDtoInterfaceList = CalculateInterfaceList(entity, excludeFramework: true, excludeEntity: true);

        fields = fields
            .GroupBy(f => f.Name)
            .Select(g => g.First())
            .ToList();

        var fieldList = fields.Select(f =>
        {
            var regexPattern = NormalizeRegexPattern(f.RegexPattern);
            return new Dictionary<string, object?>
            {
            ["name"] = f.Name,
            ["name_lower"] = ToCamelCase(f.Name),
            ["description"] = f.Description,
            ["data_type"] = GetGenerationDataType(f),
            ["is_nullable"] = f.IsNullable,
            ["is_primary_key"] = f.IsPrimaryKey,
            ["is_required"] = f.IsRequired,
            ["is_system_field"] = f.IsSystemField,
            ["is_interface_field"] = IsInterfaceField(f.Name, entity, baseClassName),
            ["is_ignore"] = f.IsIgnore,
            ["form_control_type"] = f.FormControlType,
            ["select_data_source"] = f.SelectDataSource,
            ["select_options"] = f.SelectOptions,
            ["is_multiple"] = f.IsMultiple,
            ["related_entity_name"] = f.RelatedEntityName,
            ["related_entity_name_lower"] = f.RelatedEntityName != null ? ToCamelCase(f.RelatedEntityName) : null,
            ["related_entity_id_field"] = GetRelatedEntityIdField(f),
            ["related_entity_id_field_lower"] = ToCamelCase(GetRelatedEntityIdField(f)),
            ["related_entity_display_fields"] = f.RelatedEntityDisplayFields,
            ["related_display_label"] = GetFirstDisplayField(f.RelatedEntityDisplayFields),
            ["related_display_fields_list"] = ParseDisplayFields(f.RelatedEntityDisplayFields),
            ["show_in_list"] = f.ShowInList,
            ["show_in_add_form"] = f.ShowInAddForm,
            ["show_in_edit_form"] = f.ShowInEditForm,
            ["show_in_detail"] = f.ShowInDetail,
            ["show_in_search"] = f.ShowInSearch,
            ["list_width"] = f.ListWidth,
            ["column_name"] = ToSnakeCase(f.Name),
            ["field_category"] = f.FieldCategory,
            ["formula"] = f.Formula,
            ["aggregate_type"] = f.AggregateType,
            ["aggregate_child_entity_id"] = f.AggregateChildEntityId,
            ["aggregate_child_field_name"] = f.AggregateChildFieldName,
            ["aggregate_separator"] = f.AggregateSeparator,
            ["min_value"] = string.IsNullOrEmpty(f.MinValue) ? null : f.MinValue,
            ["max_value"] = string.IsNullOrEmpty(f.MaxValue) ? null : f.MaxValue,
            ["regex_pattern"] = regexPattern,
            ["has_regex_pattern"] = regexPattern != null,
            ["regex_pattern_literal"] = ToCSharpStringLiteral(regexPattern),
            ["is_email"] = f.IsEmail,
            ["is_phone"] = f.IsPhone,
            ["default_value"] = f.DefaultValue,
            ["show_condition"] = f.ShowCondition,
            ["is_sortable"] = f.IsSortable,
            };
        }).ToList();

        var mainSelectTableEntityNames = new HashSet<string>(
            fields.Where(f => f.FormControlType == "select-table" && !string.IsNullOrEmpty(f.RelatedEntityName))
                  .Select(f => f.RelatedEntityName!));
        var mainDictFieldKeys = new Dictionary<string, string?>(
            fields.Where(f => f.SelectDataSource == "dict" && !string.IsNullOrEmpty(f.Name))
                  .Select(f => new KeyValuePair<string, string?>(f.Name, f.SelectOptions)));

        var relationList = new List<Dictionary<string, object?>>();
        var childSelectTableRefNames = new Dictionary<string, string>();
        var childDictRefNames = new Dictionary<string, string>();

        foreach (var r in relations)
        {
            var childEntity = _db.Queryable<ModuleEntity>().Where(e => e.Id == r.ChildEntityId).First();
            var childAllFields = _db.Queryable<EntityField>().Where(f => f.ModuleEntityId == r.ChildEntityId).OrderBy(f => f.OrderNum).ToList();
            var childFields = childAllFields
                .Where(f => !IsInterfaceField(f.Name, childEntity) && f.Name != r.ChildForeignKey)
                .Where(f => f.ShowInList || f.FormControlType == "select-table")
                .ToList();
            var childEditableFields = childAllFields
                .Where(f => !IsInterfaceField(f.Name, childEntity) && f.Name != r.ChildForeignKey)
                .Where(f => !f.IsPrimaryKey && f.ShowInAddForm)
                .ToList();

            var childFieldList = childFields.Select(f =>
            {
                var dict = new Dictionary<string, object?>
                {
                    ["name"] = f.Name,
                    ["name_lower"] = ToCamelCase(f.Name),
                    ["description"] = f.Description,
                    ["data_type"] = GetGenerationDataType(f),
                    ["is_nullable"] = f.IsNullable,
                    ["form_control_type"] = f.FormControlType,
                    ["select_data_source"] = f.SelectDataSource,
                    ["select_options"] = f.SelectOptions,
                    ["related_entity_name"] = f.RelatedEntityName,
                    ["related_entity_name_lower"] = f.RelatedEntityName != null ? ToCamelCase(f.RelatedEntityName) : null,
                    ["related_entity_id_field"] = GetRelatedEntityIdField(f),
                    ["related_entity_id_field_lower"] = ToCamelCase(GetRelatedEntityIdField(f)),
                    ["related_display_label"] = GetFirstDisplayField(f.RelatedEntityDisplayFields),
                    ["related_display_fields_list"] = ParseDisplayFields(f.RelatedEntityDisplayFields),
                    ["field_category"] = f.FieldCategory,
                    ["formula"] = f.Formula,
                    ["aggregate_type"] = f.AggregateType,
                    ["aggregate_child_entity_id"] = f.AggregateChildEntityId,
                    ["aggregate_child_field_name"] = f.AggregateChildFieldName,
                    ["aggregate_separator"] = f.AggregateSeparator,
                };

                if (f.FormControlType == "select-table" && !string.IsNullOrEmpty(f.RelatedEntityName))
                {
                    var defaultRefName = ToCamelCase(f.RelatedEntityName) + "Options";
                    if (mainSelectTableEntityNames.Contains(f.RelatedEntityName))
                    {
                        dict["ref_name"] = defaultRefName;
                    }
                    else if (childSelectTableRefNames.TryGetValue(f.RelatedEntityName, out var existingName))
                    {
                        dict["ref_name"] = existingName;
                    }
                    else
                    {
                        dict["ref_name"] = ToCamelCase(r.ChildEntityName) + f.RelatedEntityName + "Options";
                    }
                }

                return dict;
            }).ToList();

            var childEditableFieldList = childEditableFields.Select(f =>
            {
                var dict = new Dictionary<string, object?>
                {
                    ["name"] = f.Name,
                    ["name_lower"] = ToCamelCase(f.Name),
                    ["description"] = f.Description,
                    ["data_type"] = GetGenerationDataType(f),
                    ["is_nullable"] = f.IsNullable,
                    ["form_control_type"] = f.FormControlType,
                    ["select_data_source"] = f.SelectDataSource,
                    ["select_options"] = f.SelectOptions,
                    ["is_multiple"] = f.IsMultiple,
                    ["is_required"] = f.IsRequired,
                    ["related_entity_name"] = f.RelatedEntityName,
                    ["related_entity_name_lower"] = f.RelatedEntityName != null ? ToCamelCase(f.RelatedEntityName) : null,
                    ["related_entity_id_field"] = GetRelatedEntityIdField(f),
                    ["related_entity_id_field_lower"] = ToCamelCase(GetRelatedEntityIdField(f)),
                    ["related_entity_display_fields"] = f.RelatedEntityDisplayFields,
                    ["related_display_label"] = GetFirstDisplayField(f.RelatedEntityDisplayFields),
                    ["related_display_fields_list"] = ParseDisplayFields(f.RelatedEntityDisplayFields),
                    ["field_category"] = f.FieldCategory,
                    ["formula"] = f.Formula,
                    ["aggregate_type"] = f.AggregateType,
                    ["aggregate_child_entity_id"] = f.AggregateChildEntityId,
                    ["aggregate_child_field_name"] = f.AggregateChildFieldName,
                    ["aggregate_separator"] = f.AggregateSeparator,
                };

                if (f.FormControlType == "select-table" && !string.IsNullOrEmpty(f.RelatedEntityName))
                {
                    var defaultRefName = ToCamelCase(f.RelatedEntityName) + "Options";
                    if (mainSelectTableEntityNames.Contains(f.RelatedEntityName))
                    {
                        dict["ref_name"] = defaultRefName;
                        dict["is_shared"] = true;
                    }
                    else if (childSelectTableRefNames.TryGetValue(f.RelatedEntityName, out var existingName))
                    {
                        dict["ref_name"] = existingName;
                        dict["is_shared"] = true;
                    }
                    else
                    {
                        var refName = ToCamelCase(r.ChildEntityName) + f.RelatedEntityName + "Options";
                        dict["ref_name"] = refName;
                        dict["is_shared"] = false;
                        childSelectTableRefNames[f.RelatedEntityName] = refName;
                    }
                }
                else if (f.SelectDataSource == "dict" && !string.IsNullOrEmpty(f.Name))
                {
                    var defaultRefName = ToCamelCase(f.Name) + "Options";
                    var key = $"{f.Name}|{f.SelectOptions ?? ""}";
                    if (mainDictFieldKeys.TryGetValue(f.Name, out var mainOptions) && mainOptions == f.SelectOptions)
                    {
                        dict["ref_name"] = defaultRefName;
                        dict["is_shared"] = true;
                    }
                    else if (childDictRefNames.TryGetValue(key, out var existingName))
                    {
                        dict["ref_name"] = existingName;
                        dict["is_shared"] = true;
                    }
                    else
                    {
                        var refName = ToCamelCase(r.ChildEntityName) + f.Name + "Options";
                        dict["ref_name"] = refName;
                        dict["is_shared"] = false;
                        childDictRefNames[key] = refName;
                    }
                }

                return dict;
            }).ToList();

            relationList.Add(new Dictionary<string, object?>
            {
                ["master_field"] = r.MasterField,
                ["child_entity_id"] = r.ChildEntityId,
                ["child_entity_name"] = r.ChildEntityName,
                ["child_entity_name_lower"] = ToCamelCase(r.ChildEntityName),
                ["child_entity_name_all_lower"] = r.ChildEntityName.ToLower(),
                ["child_entity_description"] = childEntity?.Description ?? r.ChildEntityName,
                ["child_foreign_key"] = r.ChildForeignKey,
                ["child_fields"] = childFieldList,
                ["child_editable_fields"] = childEditableFieldList,
            });
        }

        var allRelatedEntityNames = new HashSet<string>(
            fields.Where(f => (f.FormControlType == "select-table" || f.FormControlType == "cascader")
                        && !string.IsNullOrEmpty(f.RelatedEntityName))
                  .Select(f => f.RelatedEntityName!));

        foreach (var r in relations)
        {
            var childSelectFields = _db.Queryable<EntityField>()
                .Where(f => f.ModuleEntityId == r.ChildEntityId
                            && f.FormControlType == "select-table"
                            && !string.IsNullOrEmpty(f.RelatedEntityName))
                .ToList();
            foreach (var cf in childSelectFields)
                allRelatedEntityNames.Add(cf.RelatedEntityName!);
        }

        var entityNameToModule = new Dictionary<string, string>();
        if (allRelatedEntityNames.Count > 0)
        {
            var relatedEntities = _db.Queryable<ModuleEntity>()
                .Where(e => allRelatedEntityNames.Contains(e.Name) && e.ProjectId == entity.ProjectId)
                .ToList();

            if (relatedEntities.Count > 0)
            {
                var moduleIds = relatedEntities.Select(e => e.ModuleId).Distinct().ToList();
                var modules = _db.Queryable<ProjectModule>()
                    .Where(m => moduleIds.Contains(m.Id))
                    .ToList();

                foreach (var re in relatedEntities)
                {
                    var mod = modules.FirstOrDefault(m => m.Id == re.ModuleId);
                    entityNameToModule[re.Name] = mod?.ModuleName?.ToLower() ?? moduleName.ToLower();
                }
            }
        }

        foreach (var name in allRelatedEntityNames)
        {
            if (SystemReferenceEntityCatalog.TryGet(name, out var referenceEntity))
            {
                entityNameToModule[name] = referenceEntity.ApiModuleName;
            }
        }

        var relatedImports = allRelatedEntityNames.Select(name => new Dictionary<string, object?>
        {
            ["entity_name"] = name,
            ["entity_name_lower"] = SystemReferenceEntityCatalog.TryGet(name, out var referenceEntity)
                ? referenceEntity.ApiEntityName
                : ToCamelCase(name),
            ["module_name_lower"] = entityNameToModule.TryGetValue(name, out var mn) ? mn : moduleName.ToLower(),
        }).ToList();

        var searchFields = fieldList.Where(f => (bool)(f["show_in_search"] ?? false)).ToList();
        var listFields = fieldList.Where(f => (bool)(f["show_in_list"] ?? false) || (string?)(f["form_control_type"]) == "select-table").ToList();
        var addFields = fieldList.Where(f => (bool)(f["show_in_add_form"] ?? false) && !(bool)(f["is_primary_key"] ?? false)).ToList();
        var editFields = fieldList.Where(f => (bool)(f["show_in_edit_form"] ?? false) && !(bool)(f["is_primary_key"] ?? false)).ToList();
        var detailFields = fieldList.Where(f => (bool)(f["show_in_detail"] ?? false) || (string?)(f["form_control_type"]) == "select-table").ToList();
        var multipleFields = fieldList.Where(f => (bool)(f["is_multiple"] ?? false)).ToList();
        var dictFields = fieldList.Where(f => (string?)(f["select_data_source"]) == "dict").ToList();
        var selectTableFields = fieldList.Where(f => (string?)(f["form_control_type"]) == "select-table").ToList();
        var cascaderFields = fieldList.Where(f => (string?)(f["form_control_type"]) == "cascader").ToList();
        var enumFields = fieldList.Where(f => (string?)(f["select_data_source"]) == "enum").ToList();
        var computedFields = fieldList.Where(f => (string?)(f["field_category"]) == "Computed").ToList();
        foreach (var cf in computedFields)
        {
            var formula = (string?)cf["formula"];
            if (!string.IsNullOrEmpty(formula))
            {
                cf["computed_js_expr"] = global::System.Text.RegularExpressions.Regex.Replace(
                    formula, @"\[(\w+)\]", m => $"form.{ToCamelCase(m.Groups[1].Value)}");
            }
        }
        var aggregateFields = fieldList.Where(f => (string?)(f["field_category"]) == "Aggregate").ToList();
        foreach (var af in aggregateFields)
        {
            var childEntityId = (long?)af["aggregate_child_entity_id"];
            var childFieldName = (string?)af["aggregate_child_field_name"];
            if (childEntityId != null && !string.IsNullOrEmpty(childFieldName))
            {
                var relation = relations.FirstOrDefault(r => r.ChildEntityId == childEntityId.Value);
                if (relation != null)
                {
                    af["agg_child_entity_lower"] = ToCamelCase(relation.ChildEntityName);
                    af["agg_child_field_lower"] = ToCamelCase(childFieldName);
                }
            }
        }

        var computedDependencyMap = new Dictionary<string, List<Dictionary<string, string>>>();
        foreach (var cf in computedFields)
        {
            var formula = (string?)cf["formula"];
            if (string.IsNullOrEmpty(formula)) continue;
            var matches = global::System.Text.RegularExpressions.Regex.Matches(formula, @"\[(\w+)\]");
            var deps = new List<Dictionary<string, string>>();
            foreach (global::System.Text.RegularExpressions.Match m in matches)
            {
                var depName = m.Groups[1].Value;
                deps.Add(new Dictionary<string, string>
                {
                    ["name"] = depName,
                    ["name_lower"] = ToCamelCase(depName)
                });
            }
            computedDependencyMap[(string)cf["name"]!] = deps;
        }
        var fieldChangeTriggers = new Dictionary<string, List<string>>();
        foreach (var kv in computedDependencyMap)
        {
            foreach (var dep in kv.Value)
            {
                var depName = dep["name"];
                if (!fieldChangeTriggers.ContainsKey(depName))
                    fieldChangeTriggers[depName] = new List<string>();
                fieldChangeTriggers[depName].Add("calc" + kv.Key);
            }
        }

        foreach (var f in fieldList)
        {
            var fieldName = (string)f["name"]!;
            if (fieldChangeTriggers.TryGetValue(fieldName, out var triggers))
                f["change_triggers"] = triggers.Select(t => new Dictionary<string, object?> { ["func"] = t }).ToList();
            else
                f["change_triggers"] = new List<object>();
        }

        var permissionPrefix = $"{moduleName.ToLower()}:{entity.Name.ToLower()}";

        var scriptObj = new ScriptObject();
        scriptObj.Import(new Dictionary<string, object?>
        {
            ["entity"] = new Dictionary<string, object?>
            {
                ["name"] = entity.Name,
                ["name_lower"] = entity.Name.ToLower(),
                ["description"] = entity.Description,
                ["project_name"] = projectName,
                ["module_name"] = moduleName,
                ["module_name_lower"] = moduleName.ToLower(),
                ["table_name"] = entity.TableName,
                ["base_class_name"] = baseClassName,
                ["has_primary_key"] = entity.HasPrimaryKey,
                ["is_tree"] = entity.IsTree,
                ["is_read_only"] = entity.IsReadOnly,
                ["has_tenant"] = entity.HasTenant,
                ["has_data_permission"] = entity.HasDataPermission,
                ["has_audit"] = entity.HasAudit,
                ["has_soft_delete"] = entity.HasSoftDelete,
                ["generate_frontend"] = entity.GenerateFrontend,
                ["frontend_route"] = entity.FrontendRoute,
                ["interface_list"] = interfaceList,
                ["dto_interface_list"] = dtoInterfaceList,
                ["create_update_dto_interface_list"] = createUpdateDtoInterfaceList,
                ["fields"] = fieldList,
                ["one_to_many_relations"] = relationList,
                ["has_one_to_many"] = relations.Count > 0,
                ["search_fields"] = searchFields,
                ["list_fields"] = listFields,
                ["add_fields"] = addFields,
                ["edit_fields"] = editFields,
                ["detail_fields"] = detailFields,
                ["multiple_fields"] = multipleFields,
                ["dict_fields"] = dictFields,
                ["select_table_fields"] = selectTableFields,
                ["cascader_fields"] = cascaderFields,
                ["enum_fields"] = enumFields,
                ["has_cascader"] = cascaderFields.Count > 0,
                ["computed_fields"] = computedFields,
                ["aggregate_fields"] = aggregateFields,
                ["computed_dependency_map"] = computedDependencyMap,
                ["field_change_triggers"] = fieldChangeTriggers,
                ["related_imports"] = relatedImports,
                ["permission_prefix"] = permissionPrefix,
                ["split_mode"] = UseSplitScript,
            }
        });

        return scriptObj;
    }

    #endregion

    #region Code generation methods

    /// <summary>
    /// </summary>
    public async Task<string> GenerateEntityAutoAsync(long entityId, string projectName, string moduleName)
    {
        var (entity, fields, relations) = await LoadMetadataAsync(entityId);
        ValidateConstraints(entity, fields);

        var context = BuildTemplateContext(entity, fields, relations, projectName, moduleName);
        return await RenderTemplateAsync("EntityAutoTemplate.scriban", context);
    }

    /// <summary>
    /// </summary>
    public async Task<string> GenerateEntityAsync(long entityId, string projectName, string moduleName)
    {
        var (entity, fields, relations) = await LoadMetadataAsync(entityId);
        var context = BuildTemplateContext(entity, fields, relations, projectName, moduleName);
        return await RenderTemplateAsync("EntityTemplate.scriban", context);
    }

    /// <summary>
    /// </summary>
    public async Task<string> GenerateDtoAsync(long entityId, string projectName, string moduleName)
    {
        var (entity, fields, relations) = await LoadMetadataAsync(entityId);
        var context = BuildTemplateContext(entity, fields, relations, projectName, moduleName);
        return await RenderTemplateAsync("DtoTemplate.scriban", context);
    }

    /// <summary>
    /// </summary>
    public async Task<string> GenerateServiceInterfaceAsync(long entityId, string projectName, string moduleName)
    {
        var (entity, fields, relations) = await LoadMetadataAsync(entityId);
        var context = BuildTemplateContext(entity, fields, relations, projectName, moduleName);
        return await RenderTemplateAsync("ServiceInterfaceTemplate.scriban", context);
    }

    /// <summary>
    /// </summary>
    public async Task<string> GenerateServiceImplementationAsync(long entityId, string projectName, string moduleName)
    {
        var (entity, fields, relations) = await LoadMetadataAsync(entityId);
        var context = BuildTemplateContext(entity, fields, relations, projectName, moduleName);
        return await RenderTemplateAsync("ServiceTemplate.scriban", context);
    }

    /// <summary>
    /// </summary>
    public async Task<string> GenerateFrontendApiAsync(long entityId, string projectName, string moduleName)
    {
        var (entity, fields, relations) = await LoadMetadataAsync(entityId);
        var context = BuildTemplateContext(entity, fields, relations, projectName, moduleName);
        return await RenderTemplateAsync(Path.Combine("Frontend", "ApiTemplate.scriban"), context);
    }

    public static bool UseTemplateGenerator { get; set; } = true;
    public static bool UseSplitScript { get; set; } = true;
    public static bool UseJsonTemplate { get; set; } = true;

    #endregion

    #region Private methods

    /// <summary>
    /// </summary>
    private async Task<(ModuleEntity entity, List<EntityField> fields, List<OneToManyRelation> relations)> LoadMetadataAsync(long entityId)
    {
        var entity = await _db.Queryable<ModuleEntity>()
            .Where(e => e.Id == entityId)
            .FirstAsync();

        if (entity == null)
            throw new Exception($"实体不存在: {entityId}");

        var fields = await _db.Queryable<EntityField>()
            .Where(f => f.ModuleEntityId == entityId)
            .OrderBy(f => f.OrderNum)
            .ToListAsync();

        var relations = await _db.Queryable<OneToManyRelation>()
            .Where(r => r.ModuleEntityId == entityId)
            .OrderBy(r => r.OrderNum)
            .ToListAsync();

        return (entity, fields, relations);
    }

    /// <summary>
    /// </summary>
    private async Task<string> RenderTemplateAsync(string templateName, ScriptObject context)
    {
        var templateFile = Path.Combine(_templatePath, templateName);

        if (!File.Exists(templateFile))
        {
            throw new Exception($"模板文件不存在: {templateFile}");
        }

        var templateContent = await File.ReadAllTextAsync(templateFile);
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            var errors = string.Join("\n", template.Messages.Select(m => m.Message));
            throw new Exception($"模板 {templateName} 解析错误:\n{errors}");
        }

        var templateContext = new TemplateContext();
        templateContext.PushGlobal(context);

        return await template.RenderAsync(templateContext);
    }

    private static string ResolveTemplatePath()
    {
        var searchedPaths = new List<string>();
        foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() }.Distinct())
        {
            foreach (var candidate in GetTemplatePathCandidates(startPath))
            {
                if (Directory.Exists(candidate))
                    return candidate;

                searchedPaths.Add(candidate);
            }
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "CodeMaster.CodeGenerator", "Templates");
            if (Directory.Exists(candidate))
                return candidate;
            searchedPaths.Add(candidate);

            dir = dir.Parent;
        }

        var defaultPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "CodeMaster.CodeGenerator", "Templates"));
        searchedPaths.Add(defaultPath);

        throw new DirectoryNotFoundException(
            "Code generator templates directory not found. Searched: " +
            string.Join("; ", searchedPaths.Distinct()));
    }

    private static IEnumerable<string> GetTemplatePathCandidates(string startPath)
    {
        return new[]
        {
            Path.Combine(startPath, "CodeMaster.CodeGenerator", "Templates"),
            Path.Combine(startPath, "Templates", "CodeMaster.CodeGenerator", "Templates"),
            Path.Combine(startPath, "resources", "CodeMaster.CodeGenerator", "Templates"),
            Path.Combine(startPath, "resources", "Templates", "CodeMaster.CodeGenerator", "Templates")
        }.Select(Path.GetFullPath);
    }

    /// <summary>
    /// </summary>
    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        return char.ToLowerInvariant(str[0]) + str[1..];
    }

    private static string GetRelatedEntityIdField(EntityField field)
    {
        return string.IsNullOrWhiteSpace(field.RelatedEntityIdField)
            ? "Id"
            : field.RelatedEntityIdField.Trim();
    }

    private static string? NormalizeRegexPattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return null;

        pattern = pattern.Trim();
        try
        {
            _ = new global::System.Text.RegularExpressions.Regex(pattern);
            return pattern;
        }
        catch (ArgumentException ex)
        {
            throw new Exception($"Invalid regex pattern: {pattern}. {ex.Message}", ex);
        }
    }

    private static string? ToCSharpStringLiteral(string? value)
    {
        if (value == null)
            return null;

        return "@\"" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// </summary>
    private static string ToSnakeCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var result = new global::System.Text.StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            if (char.IsUpper(str[i]))
            {
                if (i > 0 && !char.IsUpper(str[i - 1]))
                    result.Append('_');
                result.Append(char.ToLowerInvariant(str[i]));
            }
            else
            {
                result.Append(str[i]);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// </summary>
    private static List<Dictionary<string, object?>> ParseDisplayFields(string? displayFieldsJson)
    {
        var result = new List<Dictionary<string, object?>>();
        if (string.IsNullOrEmpty(displayFieldsJson)) return result;

        try
        {
            var names = displayFieldsJson.Trim('[', ']').Split(',')
                .Select(s => s.Trim('"', ' '))
                .Where(s => !string.IsNullOrEmpty(s));
            foreach (var name in names)
            {
                result.Add(new Dictionary<string, object?>
                {
                    ["name"] = name,
                    ["name_lower"] = ToCamelCase(name)
                });
            }
        }
        catch { }
        return result;
    }

    /// <summary>
    /// </summary>
    private static string? GetFirstDisplayField(string? displayFieldsJson)
    {
        if (string.IsNullOrEmpty(displayFieldsJson))
            return null;

        try
        {
            var trimmed = displayFieldsJson.Trim('[', ']');
            var first = trimmed.Split(',').FirstOrDefault()?.Trim('"', ' ');
            return first != null ? ToCamelCase(first) : null;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}

