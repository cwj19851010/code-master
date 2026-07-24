using CodeMaster.Agent.Contracts;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Agent.Services;

public static class CodeMasterProjectChangeSetRules
{
    private static readonly HashSet<string> GenerationModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "None", "Incremental", "Full"
    };

    public static ProjectChangeSetValidationResult Validate(
        AiProjectBlueprintDto blueprint,
        ProjectChangeSetProposal proposal)
    {
        var result = new ProjectChangeSetValidationResult
        {
            ModuleCount = proposal.Modules.Count,
            EntityCount = proposal.Entities.Count,
            FieldCount = proposal.Entities.Sum(x => x.Fields.Count) + proposal.EntityUpdates.Sum(x => x.NewFields.Count),
            RelationCount = proposal.Entities.Sum(x => x.Relations.Count) + proposal.EntityUpdates.Sum(x => x.NewRelations.Count),
            UpdatedModuleCount = proposal.ModuleUpdates.Count,
            UpdatedEntityCount = proposal.EntityUpdates.Count,
            DeletedModuleCount = proposal.DeleteModuleIds.Count,
            DeletedEntityCount = proposal.DeleteEntityIds.Count
        };

        if (string.IsNullOrWhiteSpace(proposal.Summary))
            result.Errors.Add("A change set summary is required.");

        if (!HasChanges(proposal))
        {
            result.Errors.Add("The change set does not contain any metadata or generation changes.");
            return result;
        }

        if (!GenerationModes.Contains(proposal.GenerationMode))
            result.Errors.Add("GenerationMode must be None, Incremental, or Full.");
        if (proposal.BuildAfterGeneration && string.Equals(proposal.GenerationMode, "None", StringComparison.OrdinalIgnoreCase))
            result.Errors.Add("BuildAfterGeneration requires Incremental or Full generation mode.");

        var existingModulesById = blueprint.Modules.ToDictionary(x => x.Id);
        var existingModulesByName = blueprint.Modules.ToDictionary(x => x.ModuleName, StringComparer.OrdinalIgnoreCase);
        var existingEntities = blueprint.Modules.SelectMany(x => x.Entities).ToList();
        var existingEntitiesById = existingEntities.ToDictionary(x => x.Id);
        var existingEntitiesByName = existingEntities
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var controls = blueprint.Controls.Select(x => x.ControlType).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deletedModuleIds = proposal.DeleteModuleIds.ToHashSet();
        var deletedEntityIds = proposal.DeleteEntityIds.ToHashSet();

        AddDuplicateErrors(proposal.Modules.Select(x => x.ModuleName), "module", result.Errors);
        AddDuplicateErrors(proposal.Entities.Select(x => x.Name), "entity", result.Errors);
        AddDuplicateIds(proposal.ModuleUpdates.Select(x => x.ModuleId), "module update", result.Errors);
        AddDuplicateIds(proposal.EntityUpdates.Select(x => x.EntityId), "entity update", result.Errors);
        AddDuplicateIds(proposal.DeleteModuleIds, "deleted module", result.Errors);
        AddDuplicateIds(proposal.DeleteEntityIds, "deleted entity", result.Errors);

        var proposedModules = proposal.Modules
            .Where(x => !string.IsNullOrWhiteSpace(x.ModuleName))
            .GroupBy(x => x.ModuleName.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var proposedEntities = proposal.Entities
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        ValidateCreatedModules(proposal, existingModulesByName, result);
        ValidateModuleUpdates(proposal, existingModulesById, existingModulesByName, proposedModules, deletedModuleIds, result);

        foreach (var entity in proposal.Entities)
        {
            ValidateCreatedEntity(
                entity,
                existingModulesById,
                existingModulesByName,
                proposedModules,
                existingEntitiesById,
                existingEntitiesByName,
                proposedEntities,
                deletedModuleIds,
                deletedEntityIds,
                controls,
                result);
        }

        foreach (var update in proposal.EntityUpdates)
        {
            ValidateEntityUpdate(
                update,
                existingEntitiesById,
                existingEntitiesByName,
                proposedEntities,
                deletedEntityIds,
                controls,
                result);
        }

        ValidateFinalNames(blueprint, proposal, deletedModuleIds, deletedEntityIds, result);
        ValidateDeletes(blueprint, proposal, existingModulesById, existingEntitiesById, deletedModuleIds, deletedEntityIds, result);
        ValidateFinalRelationDependencies(blueprint, proposal, deletedEntityIds, result);
        ValidateGenerationTargets(proposal, existingEntitiesById, deletedEntityIds, result);
        return result;
    }

    private static bool HasChanges(ProjectChangeSetProposal proposal)
    {
        return proposal.Modules.Count > 0
            || proposal.Entities.Count > 0
            || proposal.ModuleUpdates.Count > 0
            || proposal.EntityUpdates.Count > 0
            || proposal.DeleteModuleIds.Count > 0
            || proposal.DeleteEntityIds.Count > 0
            || !string.Equals(proposal.GenerationMode, "None", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateCreatedModules(
        ProjectChangeSetProposal proposal,
        IReadOnlyDictionary<string, AiModuleBlueprintDto> existingModulesByName,
        ProjectChangeSetValidationResult result)
    {
        foreach (var module in proposal.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.ModuleName) || string.IsNullOrWhiteSpace(module.ModuleDescription))
            {
                result.Errors.Add("Every new module requires a name and description.");
                continue;
            }

            if (!CSharpModuleNameValidator.IsValid(module.ModuleName))
                result.Errors.Add(CSharpModuleNameValidator.Requirement);

            if (existingModulesByName.ContainsKey(module.ModuleName.Trim()))
                result.Errors.Add($"Module '{module.ModuleName}' already exists in this project.");
        }
    }

    private static void ValidateModuleUpdates(
        ProjectChangeSetProposal proposal,
        IReadOnlyDictionary<long, AiModuleBlueprintDto> existingModulesById,
        IReadOnlyDictionary<string, AiModuleBlueprintDto> existingModulesByName,
        IReadOnlyDictionary<string, CreateModuleProposal> proposedModules,
        IReadOnlySet<long> deletedModuleIds,
        ProjectChangeSetValidationResult result)
    {
        foreach (var update in proposal.ModuleUpdates)
        {
            if (!existingModulesById.TryGetValue(update.ModuleId, out var current))
            {
                result.Errors.Add($"Module update target '{update.ModuleId}' does not exist in this project.");
                continue;
            }

            if (deletedModuleIds.Contains(update.ModuleId))
                result.Errors.Add($"Module '{current.ModuleName}' cannot be updated and deleted in the same change set.");

            if (update.ModuleName != null)
            {
                if (string.IsNullOrWhiteSpace(update.ModuleName))
                    result.Errors.Add($"Module '{current.ModuleName}' cannot be renamed to an empty name.");
                else
                {
                    var name = update.ModuleName.Trim();
                    if (!CSharpModuleNameValidator.IsValid(name))
                        result.Errors.Add(CSharpModuleNameValidator.Requirement);
                    if (existingModulesByName.TryGetValue(name, out var conflict) && conflict.Id != current.Id)
                        result.Errors.Add($"Module name '{name}' already belongs to another module.");
                    if (proposedModules.ContainsKey(name))
                        result.Errors.Add($"Module name '{name}' is also used by a newly proposed module.");
                }
            }

            if (update.ModuleDescription != null && string.IsNullOrWhiteSpace(update.ModuleDescription))
                result.Errors.Add($"Module '{current.ModuleName}' cannot have an empty description.");
        }
    }

    private static void ValidateCreatedEntity(
        CreateEntityProposal entity,
        IReadOnlyDictionary<long, AiModuleBlueprintDto> existingModulesById,
        IReadOnlyDictionary<string, AiModuleBlueprintDto> existingModulesByName,
        IReadOnlyDictionary<string, CreateModuleProposal> proposedModules,
        IReadOnlyDictionary<long, AiEntityBlueprintDto> existingEntitiesById,
        IReadOnlyDictionary<string, AiEntityBlueprintDto> existingEntitiesByName,
        IReadOnlyDictionary<string, CreateEntityProposal> proposedEntities,
        IReadOnlySet<long> deletedModuleIds,
        IReadOnlySet<long> deletedEntityIds,
        IReadOnlySet<string> controls,
        ProjectChangeSetValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(entity.Name) || string.IsNullOrWhiteSpace(entity.Description))
        {
            result.Errors.Add("Every new entity requires a name and description.");
            return;
        }

        if (existingEntitiesByName.ContainsKey(entity.Name.Trim()))
            result.Errors.Add($"Entity '{entity.Name}' already exists in this project.");

        var moduleFound = entity.ModuleId > 0
            ? existingModulesById.ContainsKey(entity.ModuleId)
            : !string.IsNullOrWhiteSpace(entity.ModuleName)
              && (existingModulesByName.ContainsKey(entity.ModuleName.Trim()) || proposedModules.ContainsKey(entity.ModuleName.Trim()));
        if (!moduleFound)
            result.Errors.Add($"Entity '{entity.Name}' must reference an existing or proposed module by ModuleId or ModuleName.");
        else if (entity.ModuleId > 0 && deletedModuleIds.Contains(entity.ModuleId))
            result.Errors.Add($"Entity '{entity.Name}' cannot be created inside a module that is deleted by the same change set.");
        else if (entity.ModuleId <= 0
                 && !string.IsNullOrWhiteSpace(entity.ModuleName)
                 && existingModulesByName.TryGetValue(entity.ModuleName.Trim(), out var existingModule)
                 && deletedModuleIds.Contains(existingModule.Id))
            result.Errors.Add($"Entity '{entity.Name}' cannot be created inside a module that is deleted by the same change set.");

        if (entity.Fields.Count == 0)
            result.Errors.Add($"Entity '{entity.Name}' requires at least one business field.");

        AddDuplicateErrors(entity.Fields.Select(x => x.Name), $"field in entity '{entity.Name}'", result.Errors);
        foreach (var field in entity.Fields)
            ValidateNewField(entity.Name, field, controls, result);
        ValidateCreatedPrimaryKey(entity, result);

        AddDuplicateErrors(entity.Relations.Select(x => x.RelationName), $"relation in entity '{entity.Name}'", result.Errors);
        var sourceFields = GetCreatedFieldMap(entity);
        foreach (var relation in entity.Relations)
        {
            ValidateRelation(
                entity.Name,
                relation,
                sourceFields,
                existingEntitiesById,
                existingEntitiesByName,
                proposedEntities,
                deletedEntityIds,
                result);
        }
    }

    private static void ValidateEntityUpdate(
        UpdateEntityProposal update,
        IReadOnlyDictionary<long, AiEntityBlueprintDto> existingEntitiesById,
        IReadOnlyDictionary<string, AiEntityBlueprintDto> existingEntitiesByName,
        IReadOnlyDictionary<string, CreateEntityProposal> proposedEntities,
        IReadOnlySet<long> deletedEntityIds,
        IReadOnlySet<string> controls,
        ProjectChangeSetValidationResult result)
    {
        if (!existingEntitiesById.TryGetValue(update.EntityId, out var current))
        {
            result.Errors.Add($"Entity update target '{update.EntityId}' does not exist in this project.");
            return;
        }

        if (deletedEntityIds.Contains(update.EntityId))
            result.Errors.Add($"Entity '{current.Name}' cannot be updated and deleted in the same change set.");

        if (update.Name != null)
        {
            if (string.IsNullOrWhiteSpace(update.Name))
                result.Errors.Add($"Entity '{current.Name}' cannot be renamed to an empty name.");
            else
            {
                var name = update.Name.Trim();
                if (existingEntitiesByName.TryGetValue(name, out var conflict) && conflict.Id != current.Id)
                    result.Errors.Add($"Entity name '{name}' already belongs to another entity.");
                if (proposedEntities.ContainsKey(name))
                    result.Errors.Add($"Entity name '{name}' is also used by a newly proposed entity.");
            }
        }

        if (update.Description != null && string.IsNullOrWhiteSpace(update.Description))
            result.Errors.Add($"Entity '{current.Name}' cannot have an empty description.");

        var fieldsById = current.Fields.ToDictionary(x => x.Id);
        var deletedFieldIds = update.DeletedFieldIds.ToHashSet();
        AddDuplicateIds(update.UpdatedFields.Select(x => x.FieldId), $"updated field in entity '{current.Name}'", result.Errors);
        AddDuplicateIds(update.DeletedFieldIds, $"deleted field in entity '{current.Name}'", result.Errors);

        foreach (var fieldId in deletedFieldIds)
        {
            if (!fieldsById.TryGetValue(fieldId, out var field))
                result.Errors.Add($"Deleted field '{fieldId}' does not belong to entity '{current.Name}'.");
            else if (field.IsSystemField)
                result.Errors.Add($"System field '{current.Name}.{field.Name}' cannot be deleted by Agent.");
        }

        foreach (var field in update.UpdatedFields)
        {
            if (!fieldsById.TryGetValue(field.FieldId, out var currentField))
            {
                result.Errors.Add($"Updated field '{field.FieldId}' does not belong to entity '{current.Name}'.");
                continue;
            }

            if (deletedFieldIds.Contains(field.FieldId))
                result.Errors.Add($"Field '{current.Name}.{currentField.Name}' cannot be updated and deleted in the same change set.");
            ValidateUpdatedField(current.Name, currentField, field, controls, result);
        }

        foreach (var field in update.NewFields)
            ValidateNewField(current.Name, field, controls, result);

        var finalFieldNames = current.Fields
            .Where(x => !deletedFieldIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.Name);
        foreach (var field in update.UpdatedFields.Where(x => finalFieldNames.ContainsKey(x.FieldId) && !string.IsNullOrWhiteSpace(x.Name)))
            finalFieldNames[field.FieldId] = field.Name!.Trim();
        AddDuplicateErrors(
            finalFieldNames.Values.Concat(update.NewFields.Select(x => x.Name)),
            $"final field in entity '{current.Name}'",
            result.Errors);

        var relationsById = current.Relations.ToDictionary(x => x.Id);
        var deletedRelationIds = update.DeletedRelationIds.ToHashSet();
        AddDuplicateIds(update.UpdatedRelations.Select(x => x.RelationId), $"updated relation in entity '{current.Name}'", result.Errors);
        AddDuplicateIds(update.DeletedRelationIds, $"deleted relation in entity '{current.Name}'", result.Errors);
        foreach (var relationId in deletedRelationIds)
        {
            if (!relationsById.ContainsKey(relationId))
                result.Errors.Add($"Deleted relation '{relationId}' does not belong to entity '{current.Name}'.");
        }

        var sourceFields = BuildUpdatedFieldMap(current, update);
        ValidateUpdatedPrimaryKey(current, update, sourceFields, result);
        foreach (var relation in update.NewRelations)
        {
            ValidateRelation(
                current.Name,
                relation,
                sourceFields,
                existingEntitiesById,
                existingEntitiesByName,
                proposedEntities,
                deletedEntityIds,
                result);
        }

        foreach (var relation in update.UpdatedRelations)
        {
            if (!relationsById.TryGetValue(relation.RelationId, out var currentRelation))
            {
                result.Errors.Add($"Updated relation '{relation.RelationId}' does not belong to entity '{current.Name}'.");
                continue;
            }

            if (deletedRelationIds.Contains(relation.RelationId))
                result.Errors.Add($"Relation '{current.Name}.{currentRelation.RelationName}' cannot be updated and deleted in the same change set.");

            ValidateRelation(
                current.Name,
                MergeRelation(currentRelation, relation),
                sourceFields,
                existingEntitiesById,
                existingEntitiesByName,
                proposedEntities,
                deletedEntityIds,
                result);
        }

        var updatedRelationIds = update.UpdatedRelations.Select(x => x.RelationId).ToHashSet();
        foreach (var relation in current.Relations.Where(x => !deletedRelationIds.Contains(x.Id) && !updatedRelationIds.Contains(x.Id)))
        {
            if (!sourceFields.ContainsKey(relation.SourceField))
                result.Errors.Add($"Relation '{current.Name}.{relation.RelationName}' still uses removed or renamed source field '{relation.SourceField}'.");
        }
    }

    private static void ValidateNewField(
        string entityName,
        CreateEntityFieldProposal field,
        IReadOnlySet<string> controls,
        ProjectChangeSetValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.Description) || string.IsNullOrWhiteSpace(field.DataType))
        {
            result.Errors.Add($"Every field in entity '{entityName}' requires a name, description, and data type.");
            return;
        }

        ValidateControl(entityName, field.Name, field.FormControlType, controls, result);
        ValidatePrecision(entityName, field.Name, field.Precision, field.Scale, result);
        ValidateCalculatedField(
            entityName,
            field.Name,
            field.DataType,
            field.FieldCategory,
            field.Formula,
            field.AggregateType,
            field.AggregateChildEntityId,
            field.AggregateChildFieldName,
            result);
    }

    private static void ValidateUpdatedField(
        string entityName,
        AiFieldBlueprintDto current,
        UpdateEntityFieldProposal field,
        IReadOnlySet<string> controls,
        ProjectChangeSetValidationResult result)
    {
        var name = string.IsNullOrWhiteSpace(field.Name) ? current.Name : field.Name.Trim();
        if (field.Name != null && string.IsNullOrWhiteSpace(field.Name))
            result.Errors.Add($"Field '{entityName}.{current.Name}' cannot be renamed to an empty name.");
        if (field.Description != null && string.IsNullOrWhiteSpace(field.Description))
            result.Errors.Add($"Field '{entityName}.{current.Name}' cannot have an empty description.");
        if (field.DataType != null && string.IsNullOrWhiteSpace(field.DataType))
            result.Errors.Add($"Field '{entityName}.{current.Name}' cannot have an empty data type.");
        if (field.FormControlType != null && string.IsNullOrWhiteSpace(field.FormControlType))
            result.Errors.Add($"Field '{entityName}.{current.Name}' cannot have an empty form control type.");
        if (field.FieldCategory != null && string.IsNullOrWhiteSpace(field.FieldCategory))
            result.Errors.Add($"Field '{entityName}.{current.Name}' cannot have an empty field category.");
        var control = field.FormControlType ?? current.FormControlType;
        ValidateControl(entityName, name, control, controls, result);
        var precision = field.ClearPrecision ? null : field.Precision ?? current.Precision;
        var scale = field.ClearScale ? null : field.Scale ?? current.Scale;
        ValidatePrecision(entityName, name, precision, scale, result);
        ValidateCalculatedField(
            entityName,
            name,
            field.DataType ?? current.DataType,
            field.FieldCategory ?? current.FieldCategory,
            field.Formula ?? current.Formula,
            field.AggregateType ?? current.AggregateType,
            field.ClearAggregateChildEntityId ? null : field.AggregateChildEntityId ?? current.AggregateChildEntityId,
            field.AggregateChildFieldName ?? current.AggregateChildFieldName,
            result);
    }

    private static void ValidateControl(
        string entityName,
        string fieldName,
        string? control,
        IReadOnlySet<string> controls,
        ProjectChangeSetValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(control))
            result.Errors.Add($"Field '{entityName}.{fieldName}' requires a form control type.");
        else if (controls.Count > 0 && !controls.Contains(control))
            result.Errors.Add($"Field '{entityName}.{fieldName}' uses unknown control '{control}'. Read the control catalog from get_project_structure first.");
    }

    private static void ValidatePrecision(
        string entityName,
        string fieldName,
        int? precision,
        int? scale,
        ProjectChangeSetValidationResult result)
    {
        if (scale.HasValue && precision.HasValue && scale > precision)
            result.Errors.Add($"Field '{entityName}.{fieldName}' has Scale greater than Precision.");
    }

    private static void ValidateCalculatedField(
        string entityName,
        string fieldName,
        string? dataType,
        string? category,
        string? formula,
        string? aggregateType,
        long? aggregateChildEntityId,
        string? aggregateChildFieldName,
        ProjectChangeSetValidationResult result)
    {
        var normalizedCategory = string.IsNullOrWhiteSpace(category) ? "Normal" : category.Trim();
        if (!new[] { "Normal", "Computed", "Aggregate" }.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase))
        {
            result.Errors.Add($"Field '{entityName}.{fieldName}' has unsupported FieldCategory '{category}'. Use Normal, Computed, or Aggregate.");
            return;
        }

        if (normalizedCategory.Equals("Computed", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsNumericDataType(dataType))
                result.Errors.Add($"Computed field '{entityName}.{fieldName}' must use a numeric data type.");
            if (string.IsNullOrWhiteSpace(formula) || !formula.Contains('[') || !formula.Contains(']'))
                result.Errors.Add($"Computed field '{entityName}.{fieldName}' requires an arithmetic Formula with [FieldName] references.");
            return;
        }

        if (!normalizedCategory.Equals("Aggregate", StringComparison.OrdinalIgnoreCase))
            return;

        var normalizedAggregateType = string.IsNullOrWhiteSpace(aggregateType) ? "Sum" : aggregateType.Trim();
        if (!new[] { "Sum", "Avg", "Concat" }.Contains(normalizedAggregateType, StringComparer.OrdinalIgnoreCase))
            result.Errors.Add($"Aggregate field '{entityName}.{fieldName}' must use AggregateType Sum, Avg, or Concat.");
        if (!aggregateChildEntityId.HasValue || string.IsNullOrWhiteSpace(aggregateChildFieldName))
            result.Errors.Add($"Aggregate field '{entityName}.{fieldName}' requires AggregateChildEntityId and AggregateChildFieldName.");
        if (normalizedAggregateType.Equals("Concat", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsTextDataType(dataType))
                result.Errors.Add($"Concat aggregate field '{entityName}.{fieldName}' must use string or text data type.");
        }
        else if (!IsNumericDataType(dataType))
        {
            result.Errors.Add($"{normalizedAggregateType} aggregate field '{entityName}.{fieldName}' must use a numeric data type.");
        }
    }

    private static bool IsNumericDataType(string? dataType) =>
        NormalizeDataType(dataType) is "byte" or "short" or "int" or "long" or "float" or "double" or "decimal";

    private static bool IsTextDataType(string? dataType) =>
        NormalizeDataType(dataType) is "string" or "text";

    private static string NormalizeDataType(string? dataType) =>
        (dataType ?? string.Empty).Trim().TrimEnd('?').ToLowerInvariant();

    private static void ValidateRelation(
        string entityName,
        CreateEntityRelationProposal relation,
        IReadOnlyDictionary<string, RelationFieldInfo> sourceFields,
        IReadOnlyDictionary<long, AiEntityBlueprintDto> existingEntitiesById,
        IReadOnlyDictionary<string, AiEntityBlueprintDto> existingEntitiesByName,
        IReadOnlyDictionary<string, CreateEntityProposal> proposedEntities,
        IReadOnlySet<long> deletedEntityIds,
        ProjectChangeSetValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(relation.RelationName))
            result.Errors.Add($"Every relation in entity '{entityName}' requires a relation name.");

        sourceFields.TryGetValue(relation.SourceField, out var sourceField);
        if (sourceField == null)
            result.Errors.Add($"Relation '{entityName}.{relation.RelationName}' references missing source field '{relation.SourceField}'.");

        var target = ResolveTarget(relation, existingEntitiesById, existingEntitiesByName, proposedEntities);
        if (target == null)
            result.Errors.Add($"Relation '{entityName}.{relation.RelationName}' must reference an existing or proposed target entity.");
        else
        {
            if (target.Value.Id > 0 && deletedEntityIds.Contains(target.Value.Id))
                result.Errors.Add($"Relation '{entityName}.{relation.RelationName}' cannot target an entity deleted by the same change set.");
            if (!target.Value.Fields.TryGetValue(relation.TargetField, out var targetField))
                result.Errors.Add($"Relation '{entityName}.{relation.RelationName}' references missing target field '{relation.TargetField}'.");
            else if (sourceField != null)
                ValidateRelationSemantics(entityName, relation, sourceField, targetField, result);
        }

        if (target == null || sourceField == null)
        {
            ValidateEnum<EntityRelationCardinality>(relation.Cardinality, $"relation cardinality for '{entityName}.{relation.RelationName}'", result.Errors);
            ValidateEnum<EntityRelationOwnership>(relation.Ownership, $"relation ownership for '{entityName}.{relation.RelationName}'", result.Errors);
            ValidateEnum<EntityRelationDeleteBehavior>(relation.DeleteBehavior, $"delete behavior for '{entityName}.{relation.RelationName}'", result.Errors);
        }
    }

    private static void ValidateCreatedPrimaryKey(
        CreateEntityProposal entity,
        ProjectChangeSetValidationResult result)
    {
        if (!entity.HasPrimaryKey && !entity.IsReadOnly)
            result.Errors.Add($"Entity '{entity.Name}' must be read-only when HasPrimaryKey is false.");
        if (entity.IsTree && !entity.HasPrimaryKey)
            result.Errors.Add($"Tree entity '{entity.Name}' requires a primary key.");

        var primaryFields = entity.Fields.Where(field => field.IsPrimaryKey).ToList();
        if (!entity.HasPrimaryKey && primaryFields.Count > 0)
        {
            result.Errors.Add($"Entity '{entity.Name}' disables HasPrimaryKey but defines a primary-key field.");
            return;
        }

        if (!entity.HasPrimaryKey) return;
        if (primaryFields.Count > 1)
            result.Errors.Add($"Entity '{entity.Name}' can define only one primary-key field.");
        if (primaryFields.Count == 0 && entity.Fields.Any(field => string.Equals(field.Name, "Id", StringComparison.OrdinalIgnoreCase)))
            result.Errors.Add($"Entity '{entity.Name}' contains an Id field that must be marked as the primary key.");
    }

    private static void ValidateUpdatedPrimaryKey(
        AiEntityBlueprintDto current,
        UpdateEntityProposal update,
        IReadOnlyDictionary<string, RelationFieldInfo> fields,
        ProjectChangeSetValidationResult result)
    {
        var hasPrimaryKey = update.HasPrimaryKey ?? current.HasPrimaryKey;
        var isReadOnly = update.IsReadOnly ?? current.IsReadOnly;
        var isTree = update.IsTree ?? current.IsTree;
        if (!hasPrimaryKey && !isReadOnly)
            result.Errors.Add($"Entity '{current.Name}' must be read-only when HasPrimaryKey is false.");
        if (isTree && !hasPrimaryKey)
            result.Errors.Add($"Tree entity '{current.Name}' requires a primary key.");
        var primaryCount = fields.Values.Count(field => field.IsPrimaryKey);
        if (hasPrimaryKey && primaryCount != 1)
            result.Errors.Add($"Entity '{current.Name}' must have exactly one primary-key field after the update.");
        if (!hasPrimaryKey && primaryCount > 0)
            result.Errors.Add($"Entity '{current.Name}' disables HasPrimaryKey but still contains a primary-key field.");
    }

    private static IReadOnlyDictionary<string, RelationFieldInfo> GetCreatedFieldMap(CreateEntityProposal entity)
    {
        var fields = entity.Fields.Select(field => new RelationFieldInfo(field.Name, field.DataType, field.IsPrimaryKey)).ToList();
        if (entity.HasPrimaryKey && fields.All(field => !field.IsPrimaryKey))
            fields.Add(new RelationFieldInfo("Id", "long", true));
        return ToFieldMap(fields);
    }

    private static IReadOnlyDictionary<string, RelationFieldInfo> GetExistingFieldMap(AiEntityBlueprintDto entity)
    {
        var fields = entity.Fields
            .Select(field => new RelationFieldInfo(field.Name, field.DataType, field.IsPrimaryKey))
            .ToList();
        AddImplicitPrimaryKey(fields, entity.HasPrimaryKey);
        return ToFieldMap(fields);
    }

    private static IReadOnlyDictionary<string, RelationFieldInfo> BuildUpdatedFieldMap(
        AiEntityBlueprintDto current,
        UpdateEntityProposal update)
    {
        var deletedIds = update.DeletedFieldIds.ToHashSet();
        var updates = update.UpdatedFields
            .GroupBy(field => field.FieldId)
            .ToDictionary(group => group.Key, group => group.First());
        var fields = new List<RelationFieldInfo>();
        foreach (var field in current.Fields.Where(field => !deletedIds.Contains(field.Id)))
        {
            updates.TryGetValue(field.Id, out var fieldUpdate);
            fields.Add(new RelationFieldInfo(
                string.IsNullOrWhiteSpace(fieldUpdate?.Name) ? field.Name : fieldUpdate.Name.Trim(),
                string.IsNullOrWhiteSpace(fieldUpdate?.DataType) ? field.DataType : fieldUpdate.DataType.Trim(),
                fieldUpdate?.IsPrimaryKey ?? field.IsPrimaryKey));
        }
        fields.AddRange(update.NewFields.Select(field => new RelationFieldInfo(field.Name, field.DataType, field.IsPrimaryKey)));
        AddImplicitPrimaryKey(fields, update.HasPrimaryKey ?? current.HasPrimaryKey);
        return ToFieldMap(fields);
    }

    private static void AddImplicitPrimaryKey(List<RelationFieldInfo> fields, bool hasPrimaryKey)
    {
        if (!hasPrimaryKey || fields.Any(field => field.IsPrimaryKey)) return;

        var idIndex = fields.FindIndex(field => string.Equals(field.Name, "Id", StringComparison.OrdinalIgnoreCase));
        if (idIndex >= 0)
        {
            var id = fields[idIndex];
            fields[idIndex] = id with { IsPrimaryKey = true, DataType = "long" };
            return;
        }

        fields.Add(new RelationFieldInfo("Id", "long", true));
    }

    private static IReadOnlyDictionary<string, RelationFieldInfo> ToFieldMap(IEnumerable<RelationFieldInfo> fields)
    {
        return fields
            .Where(field => !string.IsNullOrWhiteSpace(field.Name))
            .GroupBy(field => field.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateRelationSemantics(
        string entityName,
        CreateEntityRelationProposal relation,
        RelationFieldInfo sourceField,
        RelationFieldInfo targetField,
        ProjectChangeSetValidationResult result)
    {
        var cardinalityValid = Enum.TryParse<EntityRelationCardinality>(relation.Cardinality, true, out var cardinality);
        var ownershipValid = Enum.TryParse<EntityRelationOwnership>(relation.Ownership, true, out var ownership);
        var deleteBehaviorValid = Enum.TryParse<EntityRelationDeleteBehavior>(relation.DeleteBehavior, true, out var deleteBehavior);
        if (!cardinalityValid)
            ValidateEnum<EntityRelationCardinality>(relation.Cardinality, $"relation cardinality for '{entityName}.{relation.RelationName}'", result.Errors);
        if (!ownershipValid)
            ValidateEnum<EntityRelationOwnership>(relation.Ownership, $"relation ownership for '{entityName}.{relation.RelationName}'", result.Errors);
        if (!deleteBehaviorValid)
            ValidateEnum<EntityRelationDeleteBehavior>(relation.DeleteBehavior, $"delete behavior for '{entityName}.{relation.RelationName}'", result.Errors);
        if (!cardinalityValid || !ownershipValid || !deleteBehaviorValid) return;

        var relationLabel = $"Relation '{entityName}.{relation.RelationName}'";
        if (cardinality == EntityRelationCardinality.ManyToMany)
        {
            result.Errors.Add($"{relationLabel} uses ManyToMany, which requires an explicit junction entity.");
            return;
        }

        if (ownership == EntityRelationOwnership.Owned &&
            cardinality is not EntityRelationCardinality.OneToOne and not EntityRelationCardinality.OneToMany)
        {
            result.Errors.Add($"{relationLabel} uses unsupported owned cardinality '{cardinality}'.");
        }

        if (cardinality == EntityRelationCardinality.OneToMany)
        {
            if (ownership != EntityRelationOwnership.Owned)
                result.Errors.Add($"{relationLabel} must use Owned ownership so it can be generated as a child-table relation.");
            if (!sourceField.IsPrimaryKey)
                result.Errors.Add($"{relationLabel} must use the source entity primary key as SourceField.");
            if (deleteBehavior != EntityRelationDeleteBehavior.Delete)
                result.Errors.Add($"{relationLabel} must use Delete behavior because generated one-to-many aggregates own their child rows.");
        }
        else if (cardinality == EntityRelationCardinality.ManyToOne)
        {
            if (ownership == EntityRelationOwnership.Owned)
                result.Errors.Add($"{relationLabel} must use Reference, ReadOnly, Snapshot, or Independent ownership.");
            if (!targetField.IsPrimaryKey)
                result.Errors.Add($"{relationLabel} must target the target entity primary key.");
        }
        else if (cardinality == EntityRelationCardinality.OneToOne)
        {
            if (ownership == EntityRelationOwnership.Owned && !sourceField.IsPrimaryKey)
                result.Errors.Add($"{relationLabel} must use the source entity primary key for an owned one-to-one relation.");
            if (ownership != EntityRelationOwnership.Owned && !targetField.IsPrimaryKey)
                result.Errors.Add($"{relationLabel} must target the target entity primary key for a reference one-to-one relation.");
        }

        if (!string.Equals(NormalizeDataType(sourceField.DataType), NormalizeDataType(targetField.DataType), StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add(
                $"{relationLabel} field types do not match: {sourceField.Name} ({sourceField.DataType}) and {targetField.Name} ({targetField.DataType}).");
        }
    }

    private static CreateEntityRelationProposal MergeRelation(
        AiRelationBlueprintDto current,
        UpdateEntityRelationProposal update)
    {
        return new CreateEntityRelationProposal
        {
            TargetEntityId = update.TargetEntityId
                ?? (update.TargetEntityName == null ? current.TargetEntityId : 0),
            TargetEntityName = update.TargetEntityName,
            RelationName = update.RelationName ?? current.RelationName,
            SourceField = update.SourceField ?? current.SourceField,
            TargetField = update.TargetField ?? current.TargetField,
            Cardinality = update.Cardinality ?? current.Cardinality,
            Ownership = update.Ownership ?? current.Ownership,
            IsRequired = update.IsRequired ?? current.IsRequired,
            DeleteBehavior = update.DeleteBehavior ?? current.DeleteBehavior,
            OrderNum = update.OrderNum ?? current.OrderNum
        };
    }

    private static (IReadOnlyDictionary<string, RelationFieldInfo> Fields, long Id)? ResolveTarget(
        CreateEntityRelationProposal relation,
        IReadOnlyDictionary<long, AiEntityBlueprintDto> existingEntitiesById,
        IReadOnlyDictionary<string, AiEntityBlueprintDto> existingEntitiesByName,
        IReadOnlyDictionary<string, CreateEntityProposal> proposedEntities)
    {
        if (relation.TargetEntityId > 0 && existingEntitiesById.TryGetValue(relation.TargetEntityId, out var existingById))
            return (GetExistingFieldMap(existingById), existingById.Id);

        if (string.IsNullOrWhiteSpace(relation.TargetEntityName))
            return null;

        var name = relation.TargetEntityName.Trim();
        if (existingEntitiesByName.TryGetValue(name, out var existingByName))
            return (GetExistingFieldMap(existingByName), existingByName.Id);

        return proposedEntities.TryGetValue(name, out var proposed)
            ? (GetCreatedFieldMap(proposed), 0)
            : null;
    }

    private static void ValidateFinalNames(
        AiProjectBlueprintDto blueprint,
        ProjectChangeSetProposal proposal,
        IReadOnlySet<long> deletedModuleIds,
        IReadOnlySet<long> deletedEntityIds,
        ProjectChangeSetValidationResult result)
    {
        var moduleUpdates = proposal.ModuleUpdates
            .GroupBy(x => x.ModuleId)
            .ToDictionary(x => x.Key, x => x.First());
        var finalModuleNames = blueprint.Modules
            .Where(x => !deletedModuleIds.Contains(x.Id))
            .Select(x => moduleUpdates.TryGetValue(x.Id, out var update) && !string.IsNullOrWhiteSpace(update.ModuleName)
                ? update.ModuleName.Trim()
                : x.ModuleName)
            .Concat(proposal.Modules.Select(x => x.ModuleName));
        AddDuplicateErrors(finalModuleNames, "final module", result.Errors);

        var entityUpdates = proposal.EntityUpdates
            .GroupBy(x => x.EntityId)
            .ToDictionary(x => x.Key, x => x.First());
        var finalEntityNames = blueprint.Modules.SelectMany(x => x.Entities)
            .Where(x => !deletedEntityIds.Contains(x.Id))
            .Select(x => entityUpdates.TryGetValue(x.Id, out var update) && !string.IsNullOrWhiteSpace(update.Name)
                ? update.Name.Trim()
                : x.Name)
            .Concat(proposal.Entities.Select(x => x.Name));
        AddDuplicateErrors(finalEntityNames, "final entity", result.Errors);
    }

    private static void ValidateFinalRelationDependencies(
        AiProjectBlueprintDto blueprint,
        ProjectChangeSetProposal proposal,
        IReadOnlySet<long> deletedEntityIds,
        ProjectChangeSetValidationResult result)
    {
        var existingEntities = blueprint.Modules.SelectMany(x => x.Entities).ToList();
        var entityUpdates = proposal.EntityUpdates
            .GroupBy(x => x.EntityId)
            .ToDictionary(x => x.Key, x => x.First());
        var finalFieldsById = new Dictionary<long, HashSet<string>>();
        var finalEntityIdsByName = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in existingEntities.Where(x => !deletedEntityIds.Contains(x.Id)))
        {
            entityUpdates.TryGetValue(entity.Id, out var update);
            var fields = (update == null ? GetExistingFieldMap(entity) : BuildUpdatedFieldMap(entity, update))
                .Keys
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            finalFieldsById[entity.Id] = fields;
            var finalName = update != null && !string.IsNullOrWhiteSpace(update.Name) ? update.Name.Trim() : entity.Name;
            finalEntityIdsByName[finalName] = entity.Id;
        }

        var proposedFieldsByName = proposal.Entities
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => GetCreatedFieldMap(x.First()).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        foreach (var source in existingEntities.Where(x => !deletedEntityIds.Contains(x.Id)))
        {
            entityUpdates.TryGetValue(source.Id, out var update);
            var deletedRelationIds = update?.DeletedRelationIds.ToHashSet() ?? new HashSet<long>();
            var updatedRelations = update?.UpdatedRelations
                .GroupBy(x => x.RelationId)
                .ToDictionary(x => x.Key, x => x.First())
                ?? new Dictionary<long, UpdateEntityRelationProposal>();
            foreach (var currentRelation in source.Relations.Where(x => !deletedRelationIds.Contains(x.Id)))
            {
                var relation = updatedRelations.TryGetValue(currentRelation.Id, out var relationUpdate)
                    ? MergeRelation(currentRelation, relationUpdate)
                    : MergeRelation(currentRelation, new UpdateEntityRelationProposal());
                ValidateFinalRelationFields(
                    source.Name,
                    relation,
                    finalFieldsById[source.Id],
                    finalFieldsById,
                    finalEntityIdsByName,
                    proposedFieldsByName,
                    result);
            }

            foreach (var relation in update?.NewRelations ?? Enumerable.Empty<CreateEntityRelationProposal>())
            {
                ValidateFinalRelationFields(
                    source.Name,
                    relation,
                    finalFieldsById[source.Id],
                    finalFieldsById,
                    finalEntityIdsByName,
                    proposedFieldsByName,
                    result);
            }
        }

        foreach (var source in proposal.Entities)
        {
            var sourceFields = GetCreatedFieldMap(source).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var relation in source.Relations)
            {
                ValidateFinalRelationFields(
                    source.Name,
                    relation,
                    sourceFields,
                    finalFieldsById,
                    finalEntityIdsByName,
                    proposedFieldsByName,
                    result);
            }
        }
    }

    private static void ValidateFinalRelationFields(
        string sourceEntityName,
        CreateEntityRelationProposal relation,
        IReadOnlySet<string> sourceFields,
        IReadOnlyDictionary<long, HashSet<string>> finalFieldsById,
        IReadOnlyDictionary<string, long> finalEntityIdsByName,
        IReadOnlyDictionary<string, HashSet<string>> proposedFieldsByName,
        ProjectChangeSetValidationResult result)
    {
        if (!sourceFields.Contains(relation.SourceField))
            result.Errors.Add($"Relation '{sourceEntityName}.{relation.RelationName}' uses removed or renamed source field '{relation.SourceField}'.");

        HashSet<string>? targetFields = null;
        if (relation.TargetEntityId > 0)
            finalFieldsById.TryGetValue(relation.TargetEntityId, out targetFields);
        else if (!string.IsNullOrWhiteSpace(relation.TargetEntityName))
        {
            var targetName = relation.TargetEntityName.Trim();
            if (finalEntityIdsByName.TryGetValue(targetName, out var targetId))
                finalFieldsById.TryGetValue(targetId, out targetFields);
            else
                proposedFieldsByName.TryGetValue(targetName, out targetFields);
        }

        if (targetFields != null && !targetFields.Contains(relation.TargetField))
            result.Errors.Add($"Relation '{sourceEntityName}.{relation.RelationName}' uses removed or renamed target field '{relation.TargetField}'.");
    }

    private static void ValidateDeletes(
        AiProjectBlueprintDto blueprint,
        ProjectChangeSetProposal proposal,
        IReadOnlyDictionary<long, AiModuleBlueprintDto> existingModulesById,
        IReadOnlyDictionary<long, AiEntityBlueprintDto> existingEntitiesById,
        IReadOnlySet<long> deletedModuleIds,
        IReadOnlySet<long> deletedEntityIds,
        ProjectChangeSetValidationResult result)
    {
        foreach (var entityId in deletedEntityIds)
        {
            if (!existingEntitiesById.ContainsKey(entityId))
                result.Errors.Add($"Deleted entity '{entityId}' does not exist in this project.");
        }

        foreach (var moduleId in deletedModuleIds)
        {
            if (!existingModulesById.TryGetValue(moduleId, out var module))
            {
                result.Errors.Add($"Deleted module '{moduleId}' does not exist in this project.");
                continue;
            }

            var remainingEntities = module.Entities.Where(x => !deletedEntityIds.Contains(x.Id)).Select(x => x.Name).ToList();
            if (remainingEntities.Count > 0)
                result.Errors.Add($"Module '{module.ModuleName}' cannot be deleted while it still contains entities: {string.Join(", ", remainingEntities)}.");
        }

        var deletedRelationsByEntity = proposal.EntityUpdates
            .GroupBy(x => x.EntityId)
            .ToDictionary(
                x => x.Key,
                x => x.SelectMany(update => update.DeletedRelationIds).ToHashSet());
        foreach (var source in blueprint.Modules.SelectMany(x => x.Entities).Where(x => !deletedEntityIds.Contains(x.Id)))
        {
            deletedRelationsByEntity.TryGetValue(source.Id, out var deletedRelationIds);
            foreach (var relation in source.Relations.Where(x => deletedEntityIds.Contains(x.TargetEntityId)))
            {
                if (deletedRelationIds == null || !deletedRelationIds.Contains(relation.Id))
                    result.Errors.Add($"Entity '{relation.TargetEntityName ?? relation.TargetEntityId.ToString()}' cannot be deleted while relation '{source.Name}.{relation.RelationName}' still references it.");
            }
        }
    }

    private static void ValidateGenerationTargets(
        ProjectChangeSetProposal proposal,
        IReadOnlyDictionary<long, AiEntityBlueprintDto> existingEntitiesById,
        IReadOnlySet<long> deletedEntityIds,
        ProjectChangeSetValidationResult result)
    {
        if (string.Equals(proposal.GenerationMode, "Incremental", StringComparison.OrdinalIgnoreCase)
            && proposal.GenerationEntityIds.Count == 0
            && proposal.Entities.Count == 0
            && proposal.EntityUpdates.Count == 0)
        {
            result.Errors.Add("Incremental generation requires an explicit entity target or an entity created/updated by the same change set.");
        }

        foreach (var entityId in proposal.GenerationEntityIds.Distinct())
        {
            if (!existingEntitiesById.ContainsKey(entityId))
                result.Errors.Add($"Generation target entity '{entityId}' does not exist in this project.");
            if (deletedEntityIds.Contains(entityId))
                result.Errors.Add($"Deleted entity '{entityId}' cannot also be a generation target.");
        }
    }

    private static void AddDuplicateErrors(IEnumerable<string> values, string label, ICollection<string> errors)
    {
        var duplicates = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key);
        foreach (var duplicate in duplicates)
            errors.Add($"Duplicate {label} name '{duplicate}'.");
    }

    private static void AddDuplicateIds(IEnumerable<long> values, string label, ICollection<string> errors)
    {
        foreach (var duplicate in values.Where(x => x > 0).GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key))
            errors.Add($"Duplicate {label} id '{duplicate}'.");
    }

    private static void ValidateEnum<T>(string value, string label, ICollection<string> errors) where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, true, out _))
            errors.Add($"Invalid {label}: '{value}'. Allowed values: {string.Join(", ", Enum.GetNames<T>())}.");
    }

    private sealed record RelationFieldInfo(string Name, string DataType, bool IsPrimaryKey);
}
