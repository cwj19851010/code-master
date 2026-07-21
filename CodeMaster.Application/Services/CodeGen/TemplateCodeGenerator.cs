using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeMaster.Application.Services.CodeGen.Marker;
using CodeMaster.Application.Services.CodeGen.Relations;
using CodeMaster.Application.ScriptBuilder;
using CodeMaster.Domain.Entities.CodeGen;
using SqlSugar;
using ScriptSection = CodeMaster.Application.Services.CodeGen.Marker.ScriptSection;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// DB-template based code generator.
/// </summary>
public class TemplateCodeGenerator
{
    private readonly ISqlSugarClient _db;
    private readonly Dictionary<long, RelationContext> _relationContexts = new();

    public TemplateCodeGenerator(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<PageGenerationResult> GeneratePageAsync(
        long entityId, string pageType, string projectName, string moduleName)
    {
        var result = new PageGenerationResult();

        var entity = await _db.Queryable<ModuleEntity>().Where(e => e.Id == entityId).FirstAsync();
        if (entity == null) throw new Exception($"Entity not found: {entityId}");

        _userFieldOverrides = await LoadUserFieldOverrides(entity, pageType, moduleName, projectName);

        var fields = await _db.Queryable<EntityField>()
            .Where(f => f.ModuleEntityId == entityId).OrderBy(f => f.OrderNum).ToListAsync();

        var relations = new List<OneToManyRelation>();
        if (_db.DbMaintenance.IsAnyTable("sys_one_to_many_relation"))
            relations = await _db.Queryable<OneToManyRelation>()
                .Where(r => r.ModuleEntityId == entityId).OrderBy(r => r.OrderNum).ToListAsync();
        var relationGraph = await new EntityRelationGraphBuilder(_db).BuildForProjectAsync(entity.ProjectId);
        var ownedOneRelations = relationGraph.GetOwnedOutgoing(entityId)
            .Where(edge => !edge.IsLegacy && edge.Cardinality == EntityRelationCardinality.OneToOne)
            .ToList();
        _relationContexts.Clear();

        var mainTemplate = await _db.Queryable<SysPageTemplate>()
            .Where(t => t.PageType == pageType && t.IsDeleted == false).FirstAsync();
        if (mainTemplate == null) throw new Exception($"No template for {pageType}");

        var genCtx = new GenContext
        {
            EntityName = entity.Name,
            EntityNameLower = ToCamelCase(entity.Name),
            EntityNameAllLower = entity.Name.ToLower(),
            EntityTitleKey = ToCamelCase(entity.Name),
            EntityDescription = entity.Description ?? entity.Name,
            ModuleName = moduleName,
            ModuleNameLower = ToCamelCase(moduleName)
        };

        result.EntityName = entity.Name;

        // Marker maps for script post-processing
        result.GenMarkerMap = new Dictionary<string, string> {
            ["entityName"] = entity.Name,
            ["entityNameLower"] = ToCamelCase(entity.Name),
            ["entityNameAllLower"] = entity.Name.ToLower(),
            ["entityTitleKey"] = ToCamelCase(entity.Name),
            ["entityDescription"] = entity.Description ?? entity.Name,
        };

        var globalSection = ScriptBuilder.ScriptSection.FromMarker(DeserializeScript(mainTemplate.ScriptSections));

        // Render field controls
        var pageFields = GetPageFields(fields, pageType);
        var fieldHtmlList = new List<string>();
        foreach (var field in pageFields)
        {
            var pageSection = pageType switch {
                "index" => field.FormControlType == "table-column" ? "list" : "search",
                "detail" => "detail",
                _ => pageType
            };
            var (html, section, fieldContext) = await RenderFieldControl(field, pageSection, entity, genCtx);
            if (!string.IsNullOrEmpty(html)) fieldHtmlList.Add(html);
            if (section != null)
            {
                var baseGenId = BuildFieldGenId(field);
                var genIds = BuildActualGenIds(baseGenId, fieldContext, pageSection);
                RecordFieldScript(result, field.Name, section, genIds, "", BuildFieldScriptLookupGenIds(baseGenId, field, "field"));
            }
        }

        var colKey = pageType == "index" ? "search" : pageType;
        genCtx.ColumnHtml[colKey] = string.Join("", fieldHtmlList);

        if (pageType == "index")
        {
            var listCols = new List<string>();
            foreach (var f in fields.Where(f => f.ShowInList || ShouldRenderRelatedDisplayField(f)))
            {
                var (h, s, fieldContext) = await RenderFieldControl(f, "list", entity, genCtx);
                if (!string.IsNullOrEmpty(h)) listCols.Add(h);
                if (s != null)
                {
                    var baseGenId = BuildColumnGenId(f);
                    var genIds = BuildActualGenIds(baseGenId, fieldContext, "list");
                    RecordFieldScript(result, f.Name, s, genIds, "", BuildFieldScriptLookupGenIds(baseGenId, f, "col"));
                }
            }
            genCtx.ColumnHtml["list"] = string.Join("", listCols);
        }

        // Render child tables
        var childSections = new Dictionary<long, ScriptBuilder.ScriptSection>();
        var cardHtmlList = new List<string>();
        var dialogHtmlList = new List<string>();

        foreach (var rel in relations)
        {
            var childEntity = await _db.Queryable<ModuleEntity>().Where(e => e.Id == rel.ChildEntityId).FirstAsync();
            if (childEntity == null) continue;

            var childFields = await _db.Queryable<EntityField>()
                .Where(f => f.ModuleEntityId == rel.ChildEntityId).OrderBy(f => f.OrderNum).ToListAsync();

            var relCtx = CreateRelationContext(rel, childEntity);
            _relationContexts[rel.Id] = relCtx;
            var childSection = new ScriptBuilder.ScriptSection();

            // Child Card
            var cardTpl = await _db.Queryable<SysChildTemplate>()
                .Where(t => t.PageType == pageType && t.ChildType == "card" && t.IsDeleted == false).FirstAsync();
            if (cardTpl != null)
            {
                var tableCols = new List<string>();
                foreach (var f in childFields.Where(f => ShouldRenderChildTableField(f, rel.ChildForeignKey)))
                {
                    var (ch, cs, fieldContext) = await RenderFieldControl(f, "list", childEntity, genCtx);
                    if (!string.IsNullOrEmpty(ch)) tableCols.Add(ch);
                    if (cs != null)
                    {
                        childSection.Merge(ScriptBuilder.ScriptSection.FromMarker(cs));
                        var baseGenId = BuildColumnGenId(f);
                        var genIds = BuildActualGenIds(baseGenId, fieldContext, "list");
                        RecordChildFieldScript(result, f, cs, genIds, childEntity.Name, BuildFieldScriptLookupGenIds(baseGenId, f, "col"));
                    }
                }
                relCtx.TableColumns = string.Join("", tableCols);
                cardHtmlList.Add(MarkerReplacer.ReplaceRelation(cardTpl.HtmlContent, relCtx));
                childSection.Merge(ScriptBuilder.ScriptSection.FromMarker(DeserializeScript(cardTpl.ScriptSections)));
            }

            // Child Dialog
            var dialogTpl = await _db.Queryable<SysChildTemplate>()
                .Where(t => t.PageType == pageType && t.ChildType == "dialog" && t.IsDeleted == false).FirstAsync();
            if (dialogTpl != null)
            {
                var dialogCols = new List<string>();
                foreach (var f in childFields.Where(f => f.ShowInAddForm && f.Name != rel.ChildForeignKey))
                {
                    var childFormPrefix = $"{ToCamelCase(childEntity.Name)}Form";
                    var (dh, ds, fieldContext) = await RenderFieldControl(f, pageType, childEntity, genCtx, childFormPrefix);
                    if (!string.IsNullOrEmpty(dh)) dialogCols.Add(dh);
                    if (ds != null)
                    {
                        childSection.Merge(ScriptBuilder.ScriptSection.FromMarker(ds));
                        var baseGenId = BuildFieldGenId(f);
                        var genIds = BuildActualGenIds(baseGenId, fieldContext, pageType);
                        RecordChildFieldScript(result, f, ds, genIds, childEntity.Name, BuildFieldScriptLookupGenIds(baseGenId, f, "field"));
                    }
                }
                relCtx.DialogColumns = string.Join("", dialogCols);
                dialogHtmlList.Add(MarkerReplacer.ReplaceRelation(dialogTpl.HtmlContent, relCtx));
                childSection.Merge(ScriptBuilder.ScriptSection.FromMarker(DeserializeScript(dialogTpl.ScriptSections)));
            }

            AddComputedFieldScripts(childSection, childFields, pageType, $"{ToCamelCase(childEntity.Name)}Form");

            // Add relation markers for script post-processing
            var rm = new Dictionary<string, string> {
                ["entityName"] = relCtx.ChildEntityName,
                ["entityNameLower"] = relCtx.ChildEntityNameLower,
                ["entityNameAllLower"] = relCtx.ChildEntityNameAllLower,
                ["entityTitleKey"] = relCtx.ChildEntityTitleKey,
                ["collectionName"] = relCtx.CollectionName,
            };
            foreach (var kv in rm)
                if (!result.RelationMarkerMap.ContainsKey(kv.Key))
                    result.RelationMarkerMap[kv.Key] = kv.Value;

            if (!childSection.IsEmpty && pageType != "detail")
                childSections[rel.Id] = childSection;
        }

        // Render owned one-to-one fields through the same control templates with nested model paths.
        foreach (var relation in ownedOneRelations)
        {
            var targetEntity = await _db.Queryable<ModuleEntity>()
                .Where(item => item.Id == relation.TargetEntityId)
                .FirstAsync();
            if (targetEntity == null) continue;

            var targetFields = await _db.Queryable<EntityField>()
                .Where(field => field.ModuleEntityId == relation.TargetEntityId)
                .OrderBy(field => field.OrderNum)
                .ToListAsync();
            var relationPath = ToCamelCase(relation.RelationName);

            if (pageType == "index")
            {
                var columns = new List<string>();
                foreach (var field in targetFields.Where(field =>
                             field.Name != relation.TargetField &&
                             (field.ShowInList || ShouldRenderRelatedDisplayField(field))))
                {
                    var (html, section, fieldContext) = await RenderFieldControl(
                        field, "list", targetEntity, genCtx, $"scope.row.{relationPath}");
                    html = PrefixOwnedFieldGenIds(html, relation.Id);
                    if (!string.IsNullOrEmpty(html)) columns.Add(html);
                    if (section != null)
                    {
                        var baseGenId = $"gen_owned_{relation.Id}_col_{field.Id}";
                        var genIds = BuildActualGenIds(baseGenId, fieldContext, "list");
                        RecordFieldScript(result, $"{relation.RelationName}.{field.Name}", section, genIds, targetEntity.Name);
                    }
                }
                genCtx.ColumnHtml["list"] = genCtx.ColumnHtml.GetValueOrDefault("list", string.Empty) + string.Join("", columns);
                continue;
            }

            if (pageType is "add" or "edit")
            {
                var columns = new List<string>();
                foreach (var field in targetFields.Where(field =>
                             !field.IsPrimaryKey &&
                             field.Name != relation.TargetField &&
                             (pageType == "add" ? field.ShowInAddForm : field.ShowInEditForm)))
                {
                    var (html, section, fieldContext) = await RenderFieldControl(
                        field, pageType, targetEntity, genCtx, $"form.{relationPath}");
                    html = PrefixOwnedFieldGenIds(html, relation.Id);
                    if (!string.IsNullOrEmpty(html)) columns.Add(html);
                    if (section != null)
                    {
                        var baseGenId = $"gen_owned_{relation.Id}_field_{field.Id}";
                        var genIds = BuildActualGenIds(baseGenId, fieldContext, pageType);
                        RecordFieldScript(result, $"{relation.RelationName}.{field.Name}", section, genIds, targetEntity.Name);
                    }
                }

                var optionalToggle = relation.IsRequired
                    ? string.Empty
                    : $"<el-switch :model-value=\"form.{relationPath} !== null\" active-text=\"启用\" inactive-text=\"不启用\" @change=\"enabled => form.{relationPath} = enabled ? {{}} : null\" />";
                cardHtmlList.Add(
                    $"<el-card shadow=\"never\" style=\"margin-top:20px\" data-gen-id=\"gen_owned_{relation.Id}\">" +
                    $"<template #header><div class=\"card-header\" style=\"display:flex;align-items:center;justify-content:space-between\"><span>{{{{ $t('{ToCamelCase(targetEntity.Name)}') }}}}</span>{optionalToggle}</div></template>" +
                    $"<el-form v-if=\"form.{relationPath}\" :model=\"form\" :rules=\"rules\" label-width=\"120px\"><el-row :gutter=\"20\">{string.Join("", columns)}</el-row></el-form>" +
                    $"<el-empty v-else description=\"未启用{targetEntity.Description}\" :image-size=\"60\" /></el-card>");
                continue;
            }

            if (pageType == "detail")
            {
                var columns = new List<string>();
                foreach (var field in targetFields.Where(field =>
                             field.Name != relation.TargetField &&
                             (field.ShowInDetail || ShouldRenderRelatedDisplayField(field))))
                {
                    var (html, section, fieldContext) = await RenderFieldControl(
                        field, "detail", targetEntity, genCtx, $"detail.{relationPath}");
                    html = PrefixOwnedFieldGenIds(html, relation.Id);
                    if (!string.IsNullOrEmpty(html)) columns.Add(html);
                    if (section != null)
                    {
                        var baseGenId = $"gen_owned_{relation.Id}_field_{field.Id}";
                        var genIds = BuildActualGenIds(baseGenId, fieldContext, "detail");
                        RecordFieldScript(result, $"{relation.RelationName}.{field.Name}", section, genIds, targetEntity.Name);
                    }
                }
                cardHtmlList.Add(
                    $"<el-card v-if=\"detail.{relationPath}\" shadow=\"never\" style=\"margin-top:20px\" data-gen-id=\"gen_owned_{relation.Id}\">" +
                    $"<template #header><div class=\"card-header\"><span>{{{{ $t('{ToCamelCase(targetEntity.Name)}') }}}}</span></div></template>" +
                    $"<el-descriptions :column=\"2\" border>{string.Join("", columns)}</el-descriptions></el-card>");
            }
        }

        genCtx.RelationCards = string.Join("\n", cardHtmlList);
        genCtx.RelationDialogs = string.Join("\n", dialogHtmlList);

        var pageTemplateHtml = ApplyEntityCapabilities(mainTemplate.HtmlContent, globalSection, entity, pageType);
        result.VueContent = MarkerReplacer.ReplaceGen(pageTemplateHtml, genCtx);

        // Page/global script is prepared first. Event handler functions are moved
        // onto the component node that declares the event, while runtime output
        // still receives a deduplicated aggregate of page + node scripts.
        globalSection.ReplaceMarkers(s => MarkerReplacer.ReplaceGen(s, genCtx));
        AddRelationInitializers(globalSection, relations, pageType);
        AddOwnedOneInitializers(globalSection, ownedOneRelations, pageType);
        AddComputedFieldScripts(globalSection, fields, pageType, "form");
        AddAggregateFieldScripts(globalSection, fields, relations, pageType);
        AddMultipleFieldNormalizers(globalSection, fields, pageType);
        AddDictLabelHelper(globalSection, fields, pageType);
        AddMultipleQueryNormalizer(globalSection, fields, pageType);
        AddUploadDisplayHelper(globalSection, result.VueContent);

        List<CodeMaster.Infrastructure.VueParser.Model.Component>? tree = null;

        // Parse generated HTML into a component tree and embed node-level scripts.
        try
        {
            tree = new CodeMaster.Infrastructure.VueParser.VueTemplateParser().ParseTemplateContent(result.VueContent);
            EmbedScriptsIntoTree(tree, result.FieldScripts);
            EmbedScriptsIntoTree(tree, result.ChildFieldScripts);
            MoveEventFunctionsToNodes(tree, globalSection);
            result.TreeJson = global::System.Text.Json.JsonSerializer.Serialize(tree,
                new global::System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = global::System.Text.Json.JsonNamingPolicy.CamelCase });
        }
        catch { /* Tree parsing failure should not block code generation. */ }

        // ---- ScriptSection -> ScriptRenderer pipeline ----
        var renderer = new ScriptRenderer();

        var runtimeSection = new ScriptBuilder.ScriptSection();
        runtimeSection.Merge(globalSection);
        if (tree != null)
        {
            runtimeSection.Merge(CollectNodeScriptSections(tree));
        }
        else
        {
            MergeScriptEntries(runtimeSection, result.FieldScripts);
            MergeScriptEntries(runtimeSection, result.ChildFieldScripts);
        }

        result.MainScriptJson = JsonSerializer.Serialize(globalSection, new JsonSerializerOptions { WriteIndented = false });
        result.MainScriptContent = renderer.RenderComposable(runtimeSection, entity.Name);
        var mainExports = runtimeSection.GetExportNames();

        // Child entities: replace relation markers, then render.
        var childExportsByEntity = new Dictionary<string, List<string>>();
        foreach (var rel in relations)
        {
            if (!_relationContexts.TryGetValue(rel.Id, out var relCtx)) continue;
            if (!childSections.TryGetValue(rel.Id, out var childSection)) continue;
            var relationSection = new ScriptBuilder.ScriptSection();
            relationSection.Merge(childSection);
            relationSection.ReplaceMarkers(s => MarkerReplacer.ReplaceRelation(s, relCtx));

            if (relationSection.IsEmpty) continue;

            var childFunctionName = relCtx.ChildEntityName + "Child";
            var scriptJson = JsonSerializer.Serialize(relationSection, new JsonSerializerOptions { WriteIndented = false });
            var scriptContent = renderer.RenderComposable(relationSection, childFunctionName, "form");
            var exports = relationSection.GetExportNames();

            result.ChildScripts[relCtx.ChildEntityName] = new ChildPageScript
            {
                EntityName = relCtx.ChildEntityName,
                ScriptJson = scriptJson,
                ScriptContent = scriptContent,
                ExportNames = exports
            };
            childExportsByEntity[relCtx.ChildEntityName] = exports;
        }
        result.ChildEntityNames = _relationContexts.Values
            .Select(c => c.ChildEntityName)
            .Distinct()
            .ToList();

        // Build .vue import and composable call.
        var entityLower = ToCamelCase(entity.Name);
        var mainExportNames = string.Join(", ", mainExports);
        result.VueImportLine = $"import {{ use{entity.Name} }} from './{entityLower}.{pageType}.auto.js';\nconst {{ {mainExportNames} }} = use{entity.Name}();";
        if (childExportsByEntity.Count > 0)
        {
            var childLines = new List<string>();
            var mainExportSet = new HashSet<string>(mainExports);
            foreach (var rel in relations)
            {
                if (!_relationContexts.TryGetValue(rel.Id, out var relCtx)) continue;
                if (!childExportsByEntity.TryGetValue(relCtx.ChildEntityName, out var exports) || exports.Count == 0) continue;
                var childExportNames = string.Join(", ", exports.Where(e => !mainExportSet.Contains(e)));
                if (string.IsNullOrWhiteSpace(childExportNames)) continue;
                childLines.Add($"import {{ use{relCtx.ChildEntityName}Child }} from './{relCtx.ChildEntityNameLower}.{pageType}.auto.js';");
                childLines.Add($"const {{ {childExportNames} }} = use{relCtx.ChildEntityName}Child(form);");
            }
            result.ChildImportLine = string.Join("\n", childLines);
        }

        return result;
    }

    private async Task<(string html, ScriptSection? section, FieldContext? fieldContext)> RenderFieldControl(
        EntityField field, string pageSection, ModuleEntity entity, GenContext genCtx, string formPrefix = "form")
    {
        var controlType = MapControlType(field.FormControlType, pageSection, field.DataType);
        var template = await _db.Queryable<SysFieldControlTemplate>()
            .Where(t => t.ControlType == controlType && t.PageSection == pageSection && t.IsDeleted == false).FirstAsync();
        if (template == null)
            template = await _db.Queryable<SysFieldControlTemplate>()
                .Where(t => t.ControlType == "input" && t.PageSection == pageSection && t.IsDeleted == false).FirstAsync();
        if (template == null) return ("", null, null);
        if (pageSection == "search") formPrefix = "queryParams";

        var rowPrefix = pageSection == "list" && formPrefix.StartsWith("scope.row", StringComparison.Ordinal)
            ? formPrefix
            : "scope.row";
        var detailPrefix = pageSection == "detail" && formPrefix.StartsWith("detail", StringComparison.Ordinal)
            ? formPrefix
            : "detail";
        var propertyName = ToCamelCase(field.Name) + (field.IsMultiple && pageSection is "add" or "edit" ? "List" : "");
        var propertyPath = pageSection switch
        {
            "list" => BuildRelativeModelPath(rowPrefix, "scope.row", propertyName),
            "detail" => BuildRelativeModelPath(detailPrefix, "detail", propertyName),
            "add" or "edit" => BuildFormPropertyPath(formPrefix, propertyName),
            _ => propertyName
        };

        var relatedEntityIdField = string.IsNullOrWhiteSpace(field.RelatedEntityIdField)
            ? "Id"
            : field.RelatedEntityIdField.Trim();
        var resultMappings = SelectTableResultMappingParser.Parse(field.ResultMappings);

        var fctx = new FieldContext
        {
            Id = field.Id.ToString(),
            Name = field.Name,
            NameLower = ToCamelCase(field.Name),
            LabelKey = ToCamelCase(field.Name),
            Description = field.Description ?? field.Name,
            DataType = field.DataType ?? "string",
            IsNullable = field.IsNullable,
            IsPrimaryKey = field.IsPrimaryKey,
            IsRequired = field.IsRequired,
            IsSortable = field.IsSortable,
            FormPrefix = formPrefix,
            RowPrefix = rowPrefix,
            DetailPrefix = detailPrefix,
            FormControlType = field.FormControlType ?? "input",
            SelectDataSource = field.SelectDataSource ?? "",
            SelectOptions = field.SelectOptions ?? "",
            SelectOptionsLiteral = BuildStaticChoiceOptionsLiteral(field.SelectOptions),
            RelatedEntityName = field.RelatedEntityName ?? "",
            RelatedEntityNameLower = ToCamelCase(field.RelatedEntityName ?? ""),
            RelatedEntityIdField = relatedEntityIdField,
            RelatedEntityIdFieldLower = ToCamelCase(relatedEntityIdField),
            RelatedDisplayFields = field.RelatedEntityDisplayFields ?? "",
            MultipleSuffix = field.IsMultiple ? "List" : "",
            MultipleAttr = field.IsMultiple ? "multiple collapse-tags collapse-tags-tooltip" : "",
            Prop = pageSection switch
            {
                "list" => $"prop=\"{propertyPath}\"",
                "add" or "edit" => $"prop=\"{propertyPath}\"",
                "search" => $"prop=\"{ToCamelCase(field.Name)}\"",
                _ => "",
            },
            Col = (pageSection == "add" || pageSection == "edit") ? ":xs=\"24\" :sm=\"12\"" : "",
            DictDataUrl = field.SelectDataSource == "dict" ? $"/api/dictdata?dictType={field.SelectOptions}" : "",
            EntityTable = formPrefix == "form" ? "" : entity.Name,
            EntityField = field.Name,
            ResultMappings = resultMappings.Select(mapping => new SelectTableMappingContext
            {
                SourceField = mapping.SourceField,
                SourceFieldLower = ToCamelCase(mapping.SourceField),
                TargetField = mapping.TargetField,
                TargetFieldLower = ToCamelCase(mapping.TargetField)
            }).ToList(),
            MappingHandlerName = $"handle{ToJsFunctionSuffix(formPrefix.Replace('.', '_'))}{field.Name}Selection",
        };

        // 琛ュ厖 select-table / cascader 鍏宠仈瀹炰綋淇℃伅
        if (!string.IsNullOrEmpty(field.RelatedEntityName) &&
            (field.FormControlType == "select-table" || field.FormControlType == "cascader"))
        {
            try
            {
                var relatedEntity = await _db.Queryable<ModuleEntity>()
                    .Where(e => e.Name == field.RelatedEntityName && e.ProjectId == entity.ProjectId)
                    .FirstAsync();
                if (relatedEntity != null)
                {
                    fctx.RelatedEntityName = relatedEntity.Name;
                    fctx.RelatedEntityNameLower = ToCamelCase(relatedEntity.Name);
                    fctx.RefName = fctx.RelatedEntityNameLower + "Options";

                    var relatedModule = await _db.Queryable<ProjectModule>()
                        .Where(m => m.Id == relatedEntity.ModuleId)
                        .FirstAsync();
                    if (relatedModule != null)
                    {
                        fctx.RelatedModuleNameLower = ToCamelCase(relatedModule.ModuleName);
                    }

                    var displayFields = ParseDisplayFields(field.RelatedEntityDisplayFields);
                    var relatedFields = await _db.Queryable<EntityField>()
                        .Where(f => f.ModuleEntityId == relatedEntity.Id)
                        .ToListAsync();
                    if (displayFields.Count > 0)
                    {
                        fctx.RelatedDisplayLabel = displayFields[0]["name"];
                    }
                    else
                    {
                        var firstStringField = await _db.Queryable<EntityField>()
                            .Where(f => f.ModuleEntityId == relatedEntity.Id && f.DataType == "string" && !f.IsPrimaryKey)
                            .OrderBy(f => f.OrderNum)
                            .FirstAsync();
                        fctx.RelatedDisplayLabel = firstStringField?.Name ?? relatedEntity.Name;

                        var fallbackDisplayField = new Dictionary<string, string>();
                        fallbackDisplayField["name"] = fctx.RelatedDisplayLabel;
                        fallbackDisplayField["name_lower"] = ToCamelCase(fctx.RelatedDisplayLabel);
                        displayFields.Add(fallbackDisplayField);
                    }

                    fctx.RelatedDisplayFieldsArg = BuildDisplayFieldsArg(displayFields);
                    fctx.RelatedDisplayLabelExpr = BuildDisplayLabelExpr(displayFields);
                    fctx.DisplayFields = BuildDisplayFieldContexts(displayFields, fctx, relatedFields);
                }
                else if (SystemReferenceEntityCatalog.TryGet(field.RelatedEntityName, out var referenceEntity))
                {
                    fctx.RelatedEntityName = referenceEntity.Name;
                    fctx.RelatedEntityNameLower = ToCamelCase(referenceEntity.ApiEntityName);
                    fctx.RelatedModuleNameLower = referenceEntity.ApiModuleName;
                    fctx.RefName = fctx.RelatedEntityNameLower + "Options";

                    var displayFields = ParseDisplayFields(field.RelatedEntityDisplayFields);
                    if (displayFields.Count == 0)
                    {
                        foreach (var displayField in referenceEntity.DisplayFields)
                        {
                            displayFields.Add(new Dictionary<string, string>
                            {
                                ["name"] = displayField,
                                ["name_lower"] = ToCamelCase(displayField)
                            });
                        }
                    }

                    fctx.RelatedDisplayLabel = displayFields.Count > 0
                        ? displayFields[0]["name"]
                        : referenceEntity.ValueField;
                    fctx.RelatedDisplayFieldsArg = BuildDisplayFieldsArg(displayFields);
                    fctx.RelatedDisplayLabelExpr = BuildDisplayLabelExpr(displayFields);
                    fctx.DisplayFields = BuildDisplayFieldContexts(
                        displayFields,
                        fctx,
                        referenceEntity.Fields.Select(SystemReferenceEntityCatalog.ToEntityField).ToList());
                }
            }
            catch
            {
                // Related metadata is best-effort; field rendering can still continue.
            }
        }

        var html = MarkerReplacer.ReplaceField(template.HtmlContent, fctx);
        if (pageSection is "add" or "edit" && field.IsRequired)
            html = InjectRequiredRule(html, field);
        if (pageSection is "add" or "edit" &&
            field.FormControlType == "select-table" &&
            fctx.ResultMappings.Count > 0)
        {
            html = InjectSelectChangeHandler(html, fctx.MappingHandlerName);
        }
        if (pageSection is "add" or "edit" && IsCalculatedField(field))
            html = DisableEditableControls(html);
        // 娉ㄥ叆 entity-table / entity-field 鏍囪
        var entityAttrs = $" data-entity-table=\"{fctx.EntityTable}\" data-entity-field=\"{fctx.EntityField}\"";
        html = InjectEntityAttrs(html, entityAttrs);

        if (pageSection == "list")
        {
            var slotBody = field.FormControlType switch
            {
                "select" or "radio" or "radio-group" or "checkbox-group"
                    when !string.IsNullOrWhiteSpace(field.SelectOptions) =>
                    $"  <template #default=\"scope\">\n    {{{{ getDictLabel({fctx.RowPrefix}.{fctx.NameLower}, {fctx.NameLower}Options) }}}}\n  </template>\n</el-table-column>",
                "date" =>
                    $"  <template #default=\"scope\">\n    {{{{ formatDate({fctx.RowPrefix}.{fctx.NameLower}) }}}}\n  </template>\n</el-table-column>",
                "datetime" =>
                    $"  <template #default=\"scope\">\n    {{{{ formatDate({fctx.RowPrefix}.{fctx.NameLower}, true) }}}}\n  </template>\n</el-table-column>",
                _ => null
            };
            if (slotBody != null)
                html = html.Replace("/>", ">\n" + slotBody);
        }

        // Replace [field.xxx] markers inside the field script.
        var script = ReplaceFieldMarkersInScript(template.ScriptSections, fctx);
        ConfigureChoiceOptionsScript(script, field, fctx);
        AddSelectTableResultMappingScript(script, fctx, pageSection);
        if (pageSection == "list")
            await MergeListSupportScriptAsync(script, field, fctx);
        AddSelectTableDisplaySupportScript(script, fctx, pageSection);
        return (html, script, fctx);
    }

    private static string ApplyEntityCapabilities(
        string html,
        ScriptBuilder.ScriptSection section,
        ModuleEntity entity,
        string pageType)
    {
        if (pageType != "index") return html;

        var hasAdd = !entity.IsReadOnly;
        var hasEdit = !entity.IsReadOnly;
        var hasDelete = !entity.IsReadOnly;
        var hasDetail = entity.HasPrimaryKey;

        if (!hasAdd)
        {
            html = RemoveButtonByGenId(html, "gen_action_add");
            section.Functions.RemoveAll(item => item.Name == "handleAdd");
        }

        if (!hasEdit)
        {
            html = RemoveButtonByGenId(html, "gen_action_edit");
            section.Functions.RemoveAll(item => item.Name == "handleEdit");
        }

        if (!hasDelete)
        {
            html = RemoveButtonByGenId(html, "gen_action_delete");
            section.Functions.RemoveAll(item => item.Name == "handleDelete");
        }

        if (!hasDetail)
        {
            html = RemoveButtonByGenId(html, "gen_action_detail");
            section.Functions.RemoveAll(item => item.Name == "handleDetail");
        }

        if (!hasEdit && !hasDelete && !hasDetail)
            html = RemoveElementByGenId(html, "el-table-column", "gen_operations");

        return html;
    }

    private static string RemoveButtonByGenId(string html, string genId) =>
        RemoveElementByGenId(html, "el-button", genId);

    private static string RemoveElementByGenId(string html, string tagName, string genId)
    {
        var pattern = $@"<{Regex.Escape(tagName)}\b(?=[^>]*\bdata-gen-id\s*=\s*[""']{Regex.Escape(genId)}[""'])[^>]*>.*?</{Regex.Escape(tagName)}>";
        return Regex.Replace(html, pattern, string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
    }

    private static string InjectRequiredRule(string html, EntityField field)
    {
        var itemStart = html.IndexOf("<el-form-item", StringComparison.OrdinalIgnoreCase);
        if (itemStart < 0) return html;

        var itemEnd = html.IndexOf('>', itemStart);
        if (itemEnd < 0) return html;

        var openingTag = html[itemStart..itemEnd];
        if (Regex.IsMatch(openingTag, @"\s:?rules\s*=", RegexOptions.IgnoreCase))
            return html;

        var trigger = field.FormControlType is
            "select" or "select-table" or "cascader" or
            "date" or "datetime" or "switch" or
            "radio" or "radio-group" or "checkbox" or "checkbox-group" or
            "file" or "image"
                ? "change"
                : "blur";
        var message = ToJsSingleQuotedString($"请输入{field.Description ?? field.Name}");
        var rule = $" :rules=\"[{{ required: true, message: {message}, trigger: '{trigger}' }}]\"";
        return html.Insert(itemStart + "<el-form-item".Length, rule);
    }

    private static string ToJsSingleQuotedString(string value) =>
        "'" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal) + "'";

    private static string BuildRelativeModelPath(string prefix, string root, string propertyName)
    {
        if (string.Equals(prefix, root, StringComparison.Ordinal))
            return propertyName;
        var relative = prefix.StartsWith(root + ".", StringComparison.Ordinal)
            ? prefix[(root.Length + 1)..]
            : prefix;
        return string.IsNullOrWhiteSpace(relative) ? propertyName : $"{relative}.{propertyName}";
    }

    private static string BuildFormPropertyPath(string formPrefix, string propertyName)
    {
        if (string.Equals(formPrefix, "form", StringComparison.Ordinal))
            return propertyName;
        if (formPrefix.StartsWith("form.", StringComparison.Ordinal))
            return $"{formPrefix["form.".Length..]}.{propertyName}";

        // Child dialogs bind their el-form directly to e.g. orderItemForm.
        return propertyName;
    }

    private static string InjectSelectChangeHandler(string html, string handlerName)
    {
        var index = html.IndexOf("<el-select", StringComparison.Ordinal);
        if (index < 0) return html;
        var insertAt = index + "<el-select".Length;
        return html.Insert(insertAt, $" @change=\"{handlerName}\"");
    }

    private static void AddSelectTableResultMappingScript(
        ScriptSection section,
        FieldContext context,
        string pageSection)
    {
        if (pageSection is not ("add" or "edit") || context.ResultMappings.Count == 0) return;
        var optionsName = context.RelatedEntityNameLower + "Options";
        var lines = new List<string>
        {
            $"const selected = {optionsName}.value.find(item => String(item.value) === String(value));"
        };
        lines.AddRange(context.ResultMappings.Select(mapping =>
            $"{context.FormPrefix}.{mapping.TargetFieldLower} = selected ? selected.{mapping.SourceFieldLower} : null;"));
        section.Functions.Add(new FunctionBlock
        {
            Name = context.MappingHandlerName,
            Parameters = "value",
            Body = lines
        });
    }

    private static void ConfigureChoiceOptionsScript(
        ScriptSection section,
        EntityField field,
        FieldContext context)
    {
        if (!IsChoiceControl(field.FormControlType) || string.IsNullOrWhiteSpace(field.SelectOptions))
            return;
        if (string.Equals(field.SelectDataSource, "dict", StringComparison.OrdinalIgnoreCase))
            return;

        var optionsName = context.NameLower + "Options";
        var optionsRef = section.Refs.FirstOrDefault(item => item.Name == optionsName);
        if (optionsRef == null)
        {
            section.Refs.Add(new RefItem
            {
                Name = optionsName,
                InitialValue = context.SelectOptionsLiteral
            });
        }
        else
        {
            optionsRef.InitialValue = context.SelectOptionsLiteral;
        }

        foreach (var hook in section.Hooks)
        {
            hook.Body.RemoveAll(line =>
                line.Contains("getDataListByType", StringComparison.Ordinal) &&
                line.Contains(optionsName, StringComparison.Ordinal));
        }
        section.Hooks.RemoveAll(hook => hook.Body.Count == 0);
    }

    private static bool IsChoiceControl(string? controlType) =>
        controlType is "select" or "radio" or "radio-group" or "checkbox-group";

    private static string BuildStaticChoiceOptionsLiteral(string? rawOptions)
    {
        if (string.IsNullOrWhiteSpace(rawOptions)) return "[]";

        var trimmed = rawOptions.Trim();
        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                var options = new List<Dictionary<string, object?>>();
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var value = item.TryGetProperty("value", out var valueElement)
                            ? JsonSerializer.Deserialize<object?>(valueElement.GetRawText())
                            : null;
                        var label = item.TryGetProperty("label", out var labelElement)
                            ? labelElement.ToString()
                            : value?.ToString() ?? string.Empty;
                        options.Add(new Dictionary<string, object?>
                        {
                            ["label"] = label,
                            ["value"] = value ?? label
                        });
                    }
                    else
                    {
                        var value = item.ToString();
                        options.Add(new Dictionary<string, object?> { ["label"] = value, ["value"] = value });
                    }
                }
                return JsonSerializer.Serialize(options);
            }
        }
        catch (JsonException)
        {
            // Comma-separated enum values are also supported by MCP and the Web UI.
        }

        return JsonSerializer.Serialize(trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => new Dictionary<string, object?> { ["label"] = value, ["value"] = value })
            .ToList());
    }

    private async Task MergeListSupportScriptAsync(ScriptSection script, EntityField field, FieldContext fctx)
    {
        string? supportControlType = field.FormControlType switch
        {
            "select" or "radio" or "radio-group" or "checkbox-group"
                when field.SelectDataSource == "dict" => "select",
            _ => null
        };

        if (supportControlType == null) return;

        var supportTemplate = await _db.Queryable<SysFieldControlTemplate>()
            .Where(t => t.ControlType == supportControlType && t.PageSection == "add" && t.IsDeleted == false)
            .FirstAsync();
        if (supportTemplate == null) return;

        var supportScript = ReplaceFieldMarkersInScript(supportTemplate.ScriptSections, fctx);
        script.Merge(supportScript);
    }

    private static void AddSelectTableDisplaySupportScript(ScriptSection script, FieldContext fctx, string pageSection)
    {
        if (pageSection is not ("list" or "detail")) return;
        if (!string.Equals(fctx.FormControlType, "select-table", StringComparison.OrdinalIgnoreCase)) return;
        if (fctx.DisplayFields.Count == 0) return;

        var typedFields = fctx.DisplayFields
            .Where(RequiresSelectDisplayValues)
            .ToList();
        if (typedFields.Count == 0) return;

        AddOrReplaceMarkerFunction(script, new FunctionBlock
        {
            Name = "getSelectDisplayValues",
            Parameters = "val, options, field",
            Body =
            {
                "if (val === undefined || val === null || val === '') return []",
                "if (!options || !options.length) return []",
                "const ids = Array.isArray(val) ? val : String(val).split(',').map(v => v.trim()).filter(Boolean)",
                "const fields = String(field || '').split(',').map(f => f.trim()).filter(Boolean)",
                "const values = []",
                "const appendValue = (raw) => {",
                "  if (Array.isArray(raw)) { raw.forEach(appendValue); return }",
                "  if (raw === undefined || raw === null || raw === '') return",
                "  if (typeof raw === 'string' && raw.includes(',')) { raw.split(',').map(v => v.trim()).filter(Boolean).forEach(appendValue); return }",
                "  values.push(raw)",
                "}",
                "ids.forEach(id => {",
                "  const item = options.find(o => String(o.value) === String(id))",
                "  if (!item) return",
                "  fields.forEach(f => appendValue(item[f]))",
                "})",
                "return values"
            }
        });

        if (typedFields.Any(IsDateDisplayField))
        {
            AddOrReplaceMarkerFunction(script, new FunctionBlock
            {
                Name = "formatDate",
                Parameters = "val, withTime",
                Body =
                {
                    "if (!val) return '-'",
                    "const d = new Date(val)",
                    "const pad = (n) => String(n).padStart(2, '0')",
                    "const date = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`",
                    "return withTime ? `${date} ${pad(d.getHours())}:${pad(d.getMinutes())}` : date"
                }
            });
        }

        if (typedFields.Any(IsSwitchDisplayField))
        {
            AddOrReplaceMarkerFunction(script, new FunctionBlock
            {
                Name = "toDisplayBool",
                Parameters = "value",
                Body =
                {
                    "if (typeof value === 'boolean') return value",
                    "if (typeof value === 'number') return value === 1",
                    "const text = String(value).toLowerCase()",
                    "return text === 'true' || text === '1' || text === 'yes' || text === 'y'"
                }
            });
        }

        var dictFields = typedFields
            .Where(IsDictDisplayField)
            .Where(f => !string.IsNullOrWhiteSpace(f.DictOptions) && !string.IsNullOrWhiteSpace(f.SelectOptions))
            .GroupBy(f => f.DictOptions)
            .Select(g => g.First())
            .ToList();
        if (dictFields.Count == 0) return;

        AddMarkerImport(script, new ImportItem
        {
            Path = "@/api/system/dict",
            Destructured = "getDataListByType"
        });
        AddOrReplaceMarkerFunction(script, new FunctionBlock
        {
            Name = "getDictLabel",
            Parameters = "val, options",
            Body =
            {
                "if (val === undefined || val === null || val === '') return '-'",
                "if (!options || !options.length) return Array.isArray(val) ? val.join(', ') : val",
                "const resolve = (v) => {",
                "  v = String(v).trim()",
                "  if (!v) return ''",
                "  const item = options.find(o => String(o.value) === v)",
                "  return item?.label ?? v",
                "}",
                "if (Array.isArray(val)) return val.map(resolve).filter(Boolean).join(', ')",
                "if (typeof val === 'string' && val.includes(',')) return val.split(',').map(resolve).filter(Boolean).join(', ')",
                "return resolve(val)"
            }
        });

        foreach (var dictField in dictFields)
        {
            AddMarkerRef(script, new RefItem { Name = dictField.DictOptions, InitialValue = "[]" });
            AddOrAppendMarkerHook(script, "onMounted",
                $"getDataListByType({ToJsStringLiteral(dictField.SelectOptions)}).then(res => {{ {dictField.DictOptions}.value = res.map(item => ({{ label: item.label, value: item.value }})) }})");
        }
    }

    private static bool RequiresSelectDisplayValues(DisplayFieldContext field) =>
        IsImageDisplayField(field) ||
        IsFileDisplayField(field) ||
        IsDateDisplayField(field) ||
        IsSwitchDisplayField(field) ||
        IsDictDisplayField(field);

    private static bool IsImageDisplayField(DisplayFieldContext field) =>
        string.Equals(field.FormControlType, "image", StringComparison.OrdinalIgnoreCase);

    private static bool IsFileDisplayField(DisplayFieldContext field) =>
        string.Equals(field.FormControlType, "file", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(field.FormControlType, "upload", StringComparison.OrdinalIgnoreCase);

    private static bool IsDateDisplayField(DisplayFieldContext field) =>
        string.Equals(field.FormControlType, "date", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(field.FormControlType, "datetime", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(field.DataType, "DateTime", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(field.DataType, "DateTime?", StringComparison.OrdinalIgnoreCase);

    private static bool IsSwitchDisplayField(DisplayFieldContext field) =>
        string.Equals(field.FormControlType, "switch", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(field.DataType, "bool", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(field.DataType, "boolean", StringComparison.OrdinalIgnoreCase);

    private static bool IsDictDisplayField(DisplayFieldContext field) =>
        string.Equals(field.FormControlType, "select", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(field.SelectDataSource, "dict", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(field.SelectOptions);

    private static void AddMarkerImport(ScriptSection section, ImportItem import)
    {
        var existing = section.Imports.FirstOrDefault(i => i.Path == import.Path);
        if (existing == null)
        {
            section.Imports.Add(import);
            return;
        }

        if (!string.IsNullOrWhiteSpace(import.Destructured) && string.IsNullOrWhiteSpace(existing.Destructured))
            existing.Destructured = import.Destructured;
    }

    private static void AddMarkerRef(ScriptSection section, RefItem reference)
    {
        var existing = section.Refs.FirstOrDefault(r => r.Name == reference.Name);
        if (existing == null)
        {
            section.Refs.Add(reference);
            return;
        }

        if (string.IsNullOrWhiteSpace(existing.InitialValue))
            existing.InitialValue = reference.InitialValue;
    }

    private static void AddOrReplaceMarkerFunction(ScriptSection section, FunctionBlock function)
    {
        var existing = section.Functions.FirstOrDefault(f => f.Name == function.Name);
        if (existing == null)
        {
            section.Functions.Add(function);
            return;
        }

        existing.Parameters = function.Parameters;
        existing.IsAsync = function.IsAsync;
        existing.Body = function.Body;
    }

    private static void AddOrAppendMarkerHook(ScriptSection section, string name, string bodyLine)
    {
        var existing = section.Hooks.FirstOrDefault(h => h.Name == name);
        if (existing == null)
        {
            section.Hooks.Add(new HookBlock { Name = name, Body = { bodyLine } });
            return;
        }

        if (!existing.Body.Contains(bodyLine))
            existing.Body.Add(bodyLine);
    }

    private static string MapControlType(string formControlType, string pageSection, string dataType)
    {
        if (pageSection == "list")
            return formControlType switch { "image" => "image", "file" or "upload" => "file", "select-table" => "select-table", "editor" => "editor", _ => "table-column" };
        if (pageSection == "search")
            return formControlType switch
            {
                "date" or "datetime" or "select" or "select-table" => formControlType,
                "switch" or "checkbox" => "switch",
                "radio" or "radio-group" or "checkbox-group" => "select",
                _ => "search-input"
            };
        if (pageSection == "detail" && formControlType is "select" or "radio" or "radio-group" or "checkbox-group")
            return "select";
        return formControlType switch
        {
            "select-enum" => "select",
            "radio" => "radio-group",
            "input" or "textarea" or "number" or "switch" or "select" or "select-table"
                or "date" or "datetime" or "editor" or "file" or "image" or "cascader"
                or "checkbox" or "radio-group" or "checkbox-group" => formControlType,
            _ => dataType switch { "int" or "long" or "decimal" or "float" or "double" => "number", "bool" => "switch", "DateTime" or "DateTime?" => "date", _ => "input" }
        };
    }

    private static bool ShouldRenderChildTableField(EntityField field, string? childForeignKey)
    {
        if (field.Name == childForeignKey) return false;
        if (field.ShowInList) return true;

        // Relation display fields are meaningful in child tables even when the raw FK
        // is not marked as a normal list column.
        return ShouldRenderRelatedDisplayField(field);
    }

    private static bool ShouldRenderRelatedDisplayField(EntityField field)
    {
        return field.FormControlType == "select-table" &&
               !string.IsNullOrWhiteSpace(field.RelatedEntityName) &&
               !string.IsNullOrWhiteSpace(field.RelatedEntityDisplayFields);
    }

    private static ScriptSection DeserializeScript(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new ScriptSection();
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<ScriptSection>(json, opts) ?? new ScriptSection();
        }
        catch
        {
            return new ScriptSection();
        }
    }

    private void RecordFieldScript(PageGenerationResult result, string fieldName, ScriptSection section, IReadOnlyList<string> genIds, string entityTable = "", IEnumerable<string>? legacyGenIds = null)
    {
        if (section == null) return;
        if (IsMarkerScriptEmpty(section)) return;
        if (genIds.Count == 0) return;

        // 妫€鏌ョ敤鎴锋槸鍚﹀湪 fields.json 涓慨鏀硅繃姝ゅ瓧娈电殑 ScriptSection
        var lookupKeys = BuildFieldScriptLookupKeys(genIds, legacyGenIds);
        var userOverride = TryLoadUserFieldScript(lookupKeys);
        var finalSection = userOverride ?? ScriptBuilder.ScriptSection.FromMarker(section);
        if (finalSection.IsEmpty) return;

        var entry = new { script = global::System.Text.Json.JsonSerializer.Serialize(finalSection), tableId = entityTable, fieldId = fieldName };
        var entryJson = global::System.Text.Json.JsonSerializer.Serialize(entry);
        foreach (var genId in genIds)
            result.FieldScripts[genId] = entryJson;
    }

    private void RecordChildFieldScript(PageGenerationResult result, EntityField field, ScriptSection section, IReadOnlyList<string> genIds, string entityTable, IEnumerable<string>? legacyGenIds = null)
    {
        if (section == null) return;
        if (IsMarkerScriptEmpty(section)) return;
        if (genIds.Count == 0) return;

        var lookupKeys = BuildFieldScriptLookupKeys(genIds, legacyGenIds);
        var finalSection = TryLoadUserFieldScript(lookupKeys) ?? ScriptBuilder.ScriptSection.FromMarker(section);
        if (finalSection.IsEmpty) return;
        var entry = new { script = JsonSerializer.Serialize(finalSection), tableId = entityTable, fieldId = field.Name };
        var entryJson = JsonSerializer.Serialize(entry);

        if (!result.ChildFieldScriptsByEntity.TryGetValue(entityTable, out var entityScripts))
        {
            entityScripts = new Dictionary<string, string>();
            result.ChildFieldScriptsByEntity[entityTable] = entityScripts;
        }

        foreach (var genId in genIds)
        {
            result.ChildFieldScripts[genId] = entryJson;
            entityScripts[genId] = entryJson;
        }
    }

    /// <summary>灏濊瘯浠庡凡鏈?fields.json 鍔犺浇鐢ㄦ埛淇敼杩囩殑瀛楁鑴氭湰</summary>
    private static bool IsMarkerScriptEmpty(ScriptSection section) =>
        section.Imports.Count == 0 &&
        section.Uses.Count == 0 &&
        section.Refs.Count == 0 &&
        section.Reactives.Count == 0 &&
        section.Functions.Count == 0 &&
        section.Hooks.Count == 0 &&
        section.Computed.Count == 0 &&
        section.Watches.Count == 0;

    private ScriptBuilder.ScriptSection? TryLoadUserFieldScript(IEnumerable<string> keys)
    {
        if (_userFieldOverrides == null) return null;

        string? entryJson = null;
        foreach (var key in keys)
        {
            if (_userFieldOverrides.TryGetValue(key, out entryJson))
                break;
        }
        if (entryJson == null) return null;
        try
        {
            var script = DeserializeNodeScriptSection(entryJson);
            return script.IsEmpty ? null : script;
        }
        catch { return null; }
    }

    private Dictionary<string, string>? _userFieldOverrides;

    private async Task<Dictionary<string, string>?> LoadUserFieldOverrides(ModuleEntity entity, string pageType, string moduleName, string projectName)
    {
        try
        {
            var project = await _db.Queryable<Project>().Where(p => p.ProjectName == projectName && p.IsDeleted == false).FirstAsync();
            if (project == null || string.IsNullOrWhiteSpace(project.ProjectPath)) return null;

            var dir = Path.Combine(project.ProjectPath, $"{projectName}.Vue", "src", "views",
                moduleName.ToLower(), entity.Name.ToLower());
            if (!Directory.Exists(dir)) return null;

            var result = new Dictionary<string, string>();
            foreach (var fp in Directory.GetFiles(dir, $"*.{pageType}.fields.json"))
            {
                var json = await File.ReadAllTextAsync(fp);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict == null) continue;
                foreach (var kv in dict)
                    result[kv.Key] = kv.Value;
            }

            return result.Count == 0 ? null : result;
        }
        catch { return null; }
    }

    /// <summary>鏇挎崲鎺т欢鑴氭湰涓殑 [field.xxx] 鏍囪</summary>
    private static ScriptSection ReplaceFieldMarkersInScript(string templateScriptJson, FieldContext fctx)
    {
        if (string.IsNullOrWhiteSpace(templateScriptJson)) return new ScriptSection();
        var json = templateScriptJson;
        json = json.Replace("[field.id]", fctx.Id)
                   .Replace("[field.fieldId]", fctx.Id)
                   .Replace("[field.name]", fctx.Name)
                   .Replace("[field.nameLower]", fctx.NameLower)
                   .Replace("[field.labelKey]", fctx.LabelKey)
                   .Replace("[field.description]", fctx.Description)
                   .Replace("[field.selectOptions]", fctx.SelectOptions ?? "")
                   .Replace("[field.selectDataSource]", fctx.SelectDataSource ?? "")
                   .Replace("[field.relatedEntityName]", fctx.RelatedEntityName ?? "")
                   .Replace("[field.relatedEntityNameLower]", fctx.RelatedEntityNameLower ?? "")
                   .Replace("[field.relatedEntityIdField]", DefaultIfBlank(fctx.RelatedEntityIdField, "Id"))
                   .Replace("[field.relatedEntityIdFieldLower]", DefaultIfBlank(fctx.RelatedEntityIdFieldLower, "id"))
                   .Replace("[field.relatedDisplayFields]", fctx.RelatedDisplayFields ?? "")
                   .Replace("[field.relatedDisplayLabel]", fctx.RelatedDisplayLabel ?? "")
                   .Replace("[field.relatedDisplayFieldsArg]", fctx.RelatedDisplayFieldsArg ?? "")
                   .Replace("[field.relatedDisplayLabelExpr]", fctx.RelatedDisplayLabelExpr ?? "")
                   .Replace("[field.relatedModuleNameLower]", fctx.RelatedModuleNameLower ?? "")
                   .Replace("[field.multipleSuffix]", fctx.MultipleSuffix ?? "")
                   .Replace("[field.multipleAttr]", fctx.MultipleAttr ?? "")
                   .Replace("[field.dictDataUrl]", fctx.DictDataUrl ?? "")
                   .Replace("[field.formPrefix]", fctx.FormPrefix);
        var result = DeserializeScript(json);
        return result;
    }

    /// <summary>Inject field ownership attributes into every top-level generated root tag.</summary>
    private static string InjectEntityAttrs(string html, string attrs)
    {
        if (string.IsNullOrEmpty(html)) return html;
        var sb = new StringBuilder(html.Length + attrs.Length);
        var index = 0;
        var depth = 0;

        while (index < html.Length)
        {
            var tagStart = html.IndexOf('<', index);
            if (tagStart < 0)
            {
                sb.Append(html, index, html.Length - index);
                break;
            }

            sb.Append(html, index, tagStart - index);

            if (html.AsSpan(tagStart).StartsWith("<!--".AsSpan(), StringComparison.Ordinal))
            {
                var commentEnd = html.IndexOf("-->", tagStart, StringComparison.Ordinal);
                if (commentEnd < 0)
                {
                    sb.Append(html, tagStart, html.Length - tagStart);
                    break;
                }

                sb.Append(html, tagStart, commentEnd + 3 - tagStart);
                index = commentEnd + 3;
                continue;
            }

            var tagEnd = FindTagEnd(html, tagStart);
            if (tagEnd < 0)
            {
                sb.Append(html, tagStart, html.Length - tagStart);
                break;
            }

            var isClosingTag = tagStart + 1 < html.Length && html[tagStart + 1] == '/';
            var tagText = html.Substring(tagStart, tagEnd - tagStart + 1);

            if (isClosingTag)
            {
                if (depth > 0) depth--;
                sb.Append(tagText);
                index = tagEnd + 1;
                continue;
            }

            var isSpecialTag = tagStart + 1 < html.Length && html[tagStart + 1] == '!';
            var isSelfClosing = IsSelfClosingTag(tagText);
            var isFieldNode = tagText.Contains("data-gen-id=\"gen_field_", StringComparison.Ordinal) ||
                              tagText.Contains("data-gen-id='gen_field_", StringComparison.Ordinal) ||
                              tagText.Contains("data-gen-id=\"gen_col_", StringComparison.Ordinal) ||
                              tagText.Contains("data-gen-id='gen_col_", StringComparison.Ordinal);
            if (!isSpecialTag && (depth == 0 || isFieldNode) &&
                !tagText.Contains("data-entity-table=", StringComparison.Ordinal) &&
                !tagText.Contains("data-entity-field=", StringComparison.Ordinal))
            {
                var insertAt = isSelfClosing ? tagText.LastIndexOf('/') : tagText.Length - 1;
                tagText = tagText.Insert(insertAt, attrs);
            }

            sb.Append(tagText);
            if (!isSelfClosing && !isSpecialTag) depth++;
            index = tagEnd + 1;
        }

        return sb.ToString();
    }

    private static int FindTagEnd(string html, int tagStart)
    {
        var quote = '\0';
        for (var i = tagStart + 1; i < html.Length; i++)
        {
            var c = html[i];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
                continue;
            }

            if (c == '"' || c == '\'')
            {
                quote = c;
                continue;
            }

            if (c == '>') return i;
        }

        return -1;
    }

    private static bool IsSelfClosingTag(string tagText)
    {
        var i = tagText.Length - 2;
        while (i >= 0 && char.IsWhiteSpace(tagText[i])) i--;
        return i >= 0 && tagText[i] == '/';
    }

    /// <summary>灏嗗瓧娈电骇 ScriptSection 宓屽叆缁勪欢鏍戠殑瀵瑰簲鑺傜偣涓?/summary>
    private static void EmbedScriptsIntoTree(List<CodeMaster.Infrastructure.VueParser.Model.Component> nodes,
        Dictionary<string, string> scripts)
    {
        if (scripts.Count == 0) return;
        foreach (var n in nodes)
        {
            if (!string.IsNullOrEmpty(n.GenId) && scripts.TryGetValue(n.GenId, out var scriptJson))
            {
                try
                {
                    n.ScriptSection = ExtractScriptJson(scriptJson);
                }
                catch { n.ScriptSection = scriptJson; }
            }
            if (n.Children != null) EmbedScriptsIntoTree(n.Children, scripts);
            if (n.UseSlots != null)
            {
                foreach (var s in n.UseSlots)
                    if (s.Components != null) EmbedScriptsIntoTree(s.Components, scripts);
            }
        }
    }

    private static void MoveEventFunctionsToNodes(
        List<CodeMaster.Infrastructure.VueParser.Model.Component> nodes,
        ScriptBuilder.ScriptSection pageSection)
    {
        if (pageSection.Functions.Count == 0) return;

        var movedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in EnumerateNodes(nodes))
        {
            if (node.Events == null || node.Events.Count == 0) continue;

            foreach (var evt in node.Events)
            {
                var functionName = ExtractEventFunctionName(evt.Body ?? evt.Expression);
                if (string.IsNullOrWhiteSpace(functionName)) continue;

                var function = pageSection.Functions.FirstOrDefault(f => f.Name == functionName);
                if (function == null) continue;

                var nodeSection = DeserializeNodeScriptSection(node.ScriptSection);
                AddOrReplaceFunction(nodeSection, CloneFunction(function));
                node.ScriptSection = JsonSerializer.Serialize(nodeSection, new JsonSerializerOptions { WriteIndented = false });
                movedNames.Add(function.Name);
            }
        }

        if (movedNames.Count > 0)
            pageSection.Functions.RemoveAll(f => movedNames.Contains(f.Name));
    }

    private static ScriptBuilder.ScriptSection CollectNodeScriptSections(
        List<CodeMaster.Infrastructure.VueParser.Model.Component> nodes)
    {
        var section = new ScriptBuilder.ScriptSection();
        foreach (var node in EnumerateNodes(nodes))
        {
            var nodeSection = DeserializeNodeScriptSection(node.ScriptSection);
            if (!nodeSection.IsEmpty)
                section.Merge(nodeSection);
        }
        return section;
    }

    private static void MergeScriptEntries(
        ScriptBuilder.ScriptSection target,
        Dictionary<string, string> scripts)
    {
        foreach (var script in scripts.Values)
        {
            var section = DeserializeNodeScriptSection(script);
            if (!section.IsEmpty)
                target.Merge(section);
        }
    }

    private static IEnumerable<CodeMaster.Infrastructure.VueParser.Model.Component> EnumerateNodes(
        IEnumerable<CodeMaster.Infrastructure.VueParser.Model.Component> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;

            if (node.Children != null)
            {
                foreach (var child in EnumerateNodes(node.Children))
                    yield return child;
            }

            if (node.UseSlots == null) continue;
            foreach (var slot in node.UseSlots)
            {
                if (slot.Components == null) continue;
                foreach (var child in EnumerateNodes(slot.Components))
                    yield return child;
            }
        }
    }

    private static string ExtractScriptJson(string raw)
    {
        var scriptJson = raw;
        for (var i = 0; i < 3; i++)
        {
            if (string.IsNullOrWhiteSpace(scriptJson)) return string.Empty;

            using var doc = JsonDocument.Parse(scriptJson);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                scriptJson = doc.RootElement.GetString() ?? string.Empty;
                continue;
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("script", out var inner))
            {
                scriptJson = inner.ValueKind == JsonValueKind.String
                    ? inner.GetString() ?? string.Empty
                    : inner.GetRawText();
                continue;
            }

            return doc.RootElement.GetRawText();
        }

        return scriptJson;
    }

    private static ScriptBuilder.ScriptSection DeserializeNodeScriptSection(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new ScriptBuilder.ScriptSection();

        string scriptJson;
        try
        {
            scriptJson = ExtractScriptJson(raw);
        }
        catch
        {
            return new ScriptBuilder.ScriptSection();
        }

        if (string.IsNullOrWhiteSpace(scriptJson)) return new ScriptBuilder.ScriptSection();

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        if (LooksLikeMarkerScript(scriptJson))
        {
            var marker = TryDeserializeMarkerScript(scriptJson, opts);
            if (marker != null && !IsMarkerScriptEmpty(marker))
                return ScriptBuilder.ScriptSection.FromMarker(marker);
        }

        try
        {
            var builder = JsonSerializer.Deserialize<ScriptBuilder.ScriptSection>(scriptJson, opts);
            if (builder != null && !builder.IsEmpty && IsUsableBuilderScript(builder))
                return builder;
        }
        catch { }

        var fallbackMarker = TryDeserializeMarkerScript(scriptJson, opts);
        return fallbackMarker != null && !IsMarkerScriptEmpty(fallbackMarker)
            ? ScriptBuilder.ScriptSection.FromMarker(fallbackMarker)
            : new ScriptBuilder.ScriptSection();
    }

    private static ScriptSection? TryDeserializeMarkerScript(string json, JsonSerializerOptions opts)
    {
        try { return JsonSerializer.Deserialize<ScriptSection>(json, opts); }
        catch { return null; }
    }

    private static bool LooksLikeMarkerScript(string json) =>
        json.Contains("\"path\"", StringComparison.Ordinal) ||
        json.Contains("\"destructured\"", StringComparison.Ordinal) ||
        json.Contains("\"initialValue\"", StringComparison.Ordinal) ||
        json.Contains("\"uses\"", StringComparison.Ordinal);

    private static bool IsUsableBuilderScript(ScriptBuilder.ScriptSection section) =>
        section.Imports.All(i => !string.IsNullOrWhiteSpace(i.From) || string.Equals(i.Mode, "sideEffect", StringComparison.OrdinalIgnoreCase));

    private static string? ExtractEventFunctionName(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return null;

        var text = expression.Trim();
        var match = Regex.Match(text, @"^(?:async\s*)?(?<name>[A-Za-z_$][\w$]*)\s*(?:\(|$)");
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static FunctionInfo CloneFunction(FunctionInfo function) => new()
    {
        Name = function.Name,
        Params = function.Params,
        IsAsync = function.IsAsync,
        Body = function.Body
    };

    private static string ToCamelCase(string s) => string.IsNullOrEmpty(s) ? s : char.ToLower(s[0]) + s[1..];

    private static string DefaultIfBlank(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string BuildFieldGenId(EntityField field) => $"gen_field_{field.Id}";

    private static string BuildColumnGenId(EntityField field) => $"gen_col_{field.Id}";

    private static string PrefixOwnedFieldGenIds(string html, long relationId)
    {
        if (string.IsNullOrEmpty(html)) return html;
        return html
            .Replace("gen_field_", $"gen_owned_{relationId}_field_", StringComparison.Ordinal)
            .Replace("gen_col_", $"gen_owned_{relationId}_col_", StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> BuildActualGenIds(string baseGenId, FieldContext? fieldContext, string pageSection)
    {
        if (fieldContext?.FormControlType == "select-table" &&
            (pageSection == "list" || pageSection == "detail") &&
            fieldContext.DisplayFields.Count > 0)
        {
            return fieldContext.DisplayFields
                .Select(displayField => $"{baseGenId}_{displayField.NameLower}")
                .Distinct()
                .ToList();
        }

        return new[] { baseGenId };
    }

    private static IEnumerable<string> BuildFieldScriptLookupGenIds(string baseGenId, EntityField field, string kind)
    {
        return new[] { baseGenId }.Concat(BuildLegacyFieldGenIds(field, kind));
    }

    private static IEnumerable<string> BuildFieldScriptLookupKeys(IReadOnlyList<string> genIds, IEnumerable<string>? legacyGenIds)
    {
        return legacyGenIds == null
            ? genIds.Distinct()
            : genIds.Concat(legacyGenIds).Distinct();
    }

    private static IEnumerable<string> BuildLegacyFieldGenIds(EntityField field, string kind)
    {
        if (kind == "col")
            return new[] { $"gen_col_{field.Name}" };

        return new[] { $"gen_field_{field.Name}", $"gen_search_{field.Name}" };
    }

    private static RelationContext CreateRelationContext(OneToManyRelation relation, ModuleEntity childEntity)
    {
        var childName = childEntity.Name;
        var childLower = ToCamelCase(childName);
        var collectionName = ToCollectionName(childName);

        return new RelationContext
        {
            ChildEntityName = childName,
            ChildEntityNameLower = childLower,
            ChildEntityNameAllLower = childName.ToLower(),
            ChildEntityTitleKey = childLower,
            CollectionName = collectionName,
            FormName = childLower + "Form",
            FormRefName = childLower + "FormRef",
            EditingIndexName = childLower + "EditingIndex",
            DialogVisibleName = childLower + "DialogVisible",
            ChildEntityDescription = childEntity.Description ?? childName,
            ChildForeignKey = relation.ChildForeignKey ?? "Id",
            MasterField = relation.MasterField ?? "Id"
        };
    }

    private static string ToCollectionName(string entityName)
    {
        var lower = ToCamelCase(entityName);
        if (lower.EndsWith("s", StringComparison.Ordinal)) return lower;
        return lower + "s";
    }

    private static void AddComputedFieldScripts(ScriptBuilder.ScriptSection section, List<EntityField> fields, string pageType, string formPrefix)
    {
        if (pageType is not ("add" or "edit")) return;

        var computedFields = fields
            .Where(f => IsFieldCategory(f, "Computed") && !string.IsNullOrWhiteSpace(f.Formula))
            .ToList();
        if (computedFields.Count == 0) return;

        AddCalcHelpers(section);

        foreach (var field in computedFields)
        {
            var targetName = ToCamelCase(field.Name);
            var functionName = $"calc{ToJsFunctionSuffix(field.Name)}";
            var dependencies = ExtractFormulaDependencies(field.Formula!)
                .Where(name => !string.Equals(ToCamelCase(name), targetName, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var expression = BuildFormulaExpression(field.Formula!, formPrefix);

            AddOrReplaceFunction(section, new FunctionInfo
            {
                Name = functionName,
                Body = $"{formPrefix}.{targetName} = normalizeCalcValue({expression})"
            });

            AddOrAppendWatch(section, BuildCalcWatchSource(formPrefix, dependencies), $"{functionName}();",
                deep: dependencies.Count == 0, immediate: true);
        }
    }

    private void AddAggregateFieldScripts(ScriptBuilder.ScriptSection section, List<EntityField> fields, List<OneToManyRelation> relations, string pageType)
    {
        if (pageType is not ("add" or "edit")) return;

        var aggregateFields = fields
            .Where(f => IsFieldCategory(f, "Aggregate") &&
                        f.AggregateChildEntityId.HasValue &&
                        !string.IsNullOrWhiteSpace(f.AggregateChildFieldName))
            .ToList();
        if (aggregateFields.Count == 0) return;

        AddCalcHelpers(section);

        foreach (var field in aggregateFields)
        {
            var relation = relations.FirstOrDefault(r => r.ChildEntityId == field.AggregateChildEntityId!.Value);
            if (relation == null || !_relationContexts.TryGetValue(relation.Id, out var relCtx)) continue;

            var targetName = ToCamelCase(field.Name);
            var childFieldName = ToCamelCase(field.AggregateChildFieldName!);
            var functionName = $"calc{ToJsFunctionSuffix(field.Name)}";
            var collectionName = relCtx.CollectionName;
            var body = BuildAggregateFunctionBody(targetName, collectionName, childFieldName,
                field.AggregateType, field.AggregateSeparator);

            AddOrReplaceFunction(section, new FunctionInfo
            {
                Name = functionName,
                Body = body
            });

            AddOrAppendWatch(section, $"form.{collectionName}", $"{functionName}();", deep: true, immediate: true);
        }
    }

    private static void AddCalcHelpers(ScriptBuilder.ScriptSection section)
    {
        section.Merge(new ScriptBuilder.ScriptSection
        {
            Imports =
            {
                new ImportInfo { From = "vue", Mode = "named", Names = "watch" }
            }
        });

        AddOrReplaceFunction(section, new FunctionInfo
        {
            Name = "toCalcNumber",
            Params = "value",
            Body = string.Join("\n", new[]
            {
                "const number = Number(value)",
                "return Number.isFinite(number) ? number : 0"
            })
        });

        AddOrReplaceFunction(section, new FunctionInfo
        {
            Name = "normalizeCalcValue",
            Params = "value",
            Body = string.Join("\n", new[]
            {
                "const number = toCalcNumber(value)",
                "return Math.round((number + Number.EPSILON) * 1000000) / 1000000"
            })
        });
    }

    private static string BuildFormulaExpression(string formula, string formPrefix)
    {
        return Regex.Replace(formula, @"\[(?<name>[A-Za-z_]\w*)\]", match =>
            $"toCalcNumber({formPrefix}.{ToCamelCase(match.Groups["name"].Value)})");
    }

    private static List<string> ExtractFormulaDependencies(string formula)
    {
        return Regex.Matches(formula, @"\[(?<name>[A-Za-z_]\w*)\]")
            .Select(m => m.Groups["name"].Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
    }

    private static string BuildCalcWatchSource(string formPrefix, List<string> dependencies)
    {
        if (dependencies.Count == 0) return formPrefix;
        return "[" + string.Join(", ", dependencies.Select(name => $"{formPrefix}.{ToCamelCase(name)}")) + "]";
    }

    private static string BuildAggregateFunctionBody(string targetName, string collectionName, string childFieldName, string? aggregateType, string? separator)
    {
        var rowsLine = $"const rows = (form.{collectionName} || []).filter(item => item && String(item.rowStatus) !== '3')";
        var valueExpr = $"item.{childFieldName}";
        return (aggregateType ?? "Sum") switch
        {
            "Avg" => string.Join("\n", new[]
            {
                rowsLine,
                $"const values = rows.map(item => {valueExpr}).filter(value => value !== undefined && value !== null && value !== '')",
                $"form.{targetName} = values.length ? normalizeCalcValue(values.reduce((sum, value) => sum + toCalcNumber(value), 0) / values.length) : 0"
            }),
            "Concat" => string.Join("\n", new[]
            {
                rowsLine,
                $"form.{targetName} = rows.map(item => {valueExpr}).filter(value => value !== undefined && value !== null && value !== '').map(value => String(value)).join({ToJsStringLiteral(string.IsNullOrEmpty(separator) ? ", " : separator!)})"
            }),
            _ => string.Join("\n", new[]
            {
                rowsLine,
                $"form.{targetName} = normalizeCalcValue(rows.reduce((sum, item) => sum + toCalcNumber({valueExpr}), 0))"
            })
        };
    }

    private static void AddOrAppendWatch(ScriptBuilder.ScriptSection section, string source, string bodyLine, bool deep, bool immediate)
    {
        var existing = section.Watches.FirstOrDefault(w => w.Source == source);
        if (existing == null)
        {
            section.Watches.Add(new WatchInfo
            {
                Source = source,
                Body = bodyLine,
                Deep = deep,
                Immediate = immediate
            });
            return;
        }

        var lines = existing.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .ToHashSet(StringComparer.Ordinal);
        if (!lines.Contains(bodyLine.Trim()))
            existing.Body = string.IsNullOrWhiteSpace(existing.Body)
                ? bodyLine
                : existing.Body.TrimEnd() + "\n" + bodyLine;
        existing.Deep = existing.Deep || deep;
        existing.Immediate = existing.Immediate || immediate;
    }

    private static bool IsCalculatedField(EntityField field) =>
        IsFieldCategory(field, "Computed") || IsFieldCategory(field, "Aggregate");

    private static bool IsFieldCategory(EntityField field, string category) =>
        string.Equals(field.FieldCategory, category, StringComparison.OrdinalIgnoreCase);

    private static string DisableEditableControls(string html)
    {
        foreach (var tag in new[]
        {
            "el-input", "el-input-number", "el-select", "el-date-picker", "el-switch",
            "el-cascader", "el-radio-group", "el-checkbox-group", "el-upload"
        })
        {
            html = Regex.Replace(html, $@"<{Regex.Escape(tag)}\b[^<>]*?>", match =>
            {
                var tagText = match.Value;
                if (Regex.IsMatch(tagText, @"\s:?disabled(?:\s|=|/|>)", RegexOptions.IgnoreCase))
                    return tagText;

                var insertAt = tagText.EndsWith("/>", StringComparison.Ordinal) ? tagText.Length - 2 : tagText.Length - 1;
                return tagText.Insert(insertAt, " disabled");
            }, RegexOptions.IgnoreCase);
        }

        return html;
    }

    private static string ToJsFunctionSuffix(string value)
    {
        var name = Regex.Replace(value, @"[^\w]", "");
        if (string.IsNullOrWhiteSpace(name)) return "Field";
        if (char.IsDigit(name[0])) name = "_" + name;
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static string ToJsStringLiteral(string value) =>
        JsonSerializer.Serialize(value);

    private void AddRelationInitializers(ScriptBuilder.ScriptSection mainSection, List<OneToManyRelation> relations, string pageType)
    {
        if (relations.Count == 0 || pageType is not ("add" or "edit")) return;

        var fields = new Dictionary<string, string>();
        foreach (var rel in relations)
        {
            if (_relationContexts.TryGetValue(rel.Id, out var ctx))
                fields[ctx.CollectionName] = "[]";
        }

        if (fields.Count == 0) return;
        mainSection.Merge(new ScriptBuilder.ScriptSection
        {
            Reactives =
            {
                new ReactiveInfo { Name = "form", Fields = fields }
            }
        });

        if (pageType == "edit")
        {
            var getDetail = mainSection.Functions.FirstOrDefault(f => f.Name == "getDetail");
            if (getDetail != null)
            {
                var normalizationLines = fields.Keys.Select(name => $"form.{name} = form.{name} || [];");
                getDetail.Body = string.IsNullOrWhiteSpace(getDetail.Body)
                    ? string.Join("\n", normalizationLines)
                    : getDetail.Body.TrimEnd() + "\n" + string.Join("\n", normalizationLines);
            }
        }
    }

    private static void AddOwnedOneInitializers(
        ScriptBuilder.ScriptSection mainSection,
        IReadOnlyList<EntityRelationEdge> relations,
        string pageType)
    {
        if (relations.Count == 0 || pageType is not ("add" or "edit")) return;

        var fields = relations.ToDictionary(
            relation => ToCamelCase(relation.RelationName),
            relation => relation.IsRequired ? "{}" : "null");
        mainSection.Merge(new ScriptBuilder.ScriptSection
        {
            Reactives =
            {
                new ReactiveInfo { Name = "form", Fields = fields }
            }
        });

        if (pageType != "edit") return;
        var getDetail = mainSection.Functions.FirstOrDefault(function => function.Name == "getDetail");
        if (getDetail == null) return;
        var requiredNormalizers = relations
            .Where(relation => relation.IsRequired)
            .Select(relation =>
            {
                var relationName = ToCamelCase(relation.RelationName);
                return $"form.{relationName} = form.{relationName} || {{}};";
            })
            .ToList();
        if (requiredNormalizers.Count == 0) return;
        getDetail.Body = string.IsNullOrWhiteSpace(getDetail.Body)
            ? string.Join("\n", requiredNormalizers)
            : getDetail.Body.TrimEnd() + "\n" + string.Join("\n", requiredNormalizers);
    }

    /// <summary>瑙ｆ瀽 JSON 鏁扮粍鏍煎紡鐨勬樉绀哄瓧娈靛垪琛紝濡?["Name","Phone"]</summary>
    private static void AddMultipleFieldNormalizers(ScriptBuilder.ScriptSection mainSection, List<EntityField> fields, string pageType)
    {
        if (pageType is not ("add" or "edit")) return;

        var multipleFields = fields
            .Where(f => f.IsMultiple && !f.IsPrimaryKey && (pageType == "add" ? f.ShowInAddForm : f.ShowInEditForm))
            .ToList();
        if (multipleFields.Count == 0) return;

        var formFields = multipleFields.ToDictionary(
            f => ToCamelCase(f.Name) + "List",
            _ => "[]");

        mainSection.Merge(new ScriptBuilder.ScriptSection
        {
            Reactives =
            {
                new ReactiveInfo { Name = "form", Fields = formFields }
            }
        });

        AddOrReplaceFunction(mainSection, new FunctionInfo
        {
            Name = "normalizeMultipleFields",
            Params = "source",
            Body = BuildNormalizeMultipleFieldsBody(multipleFields)
        });

        var handleSubmit = mainSection.Functions.FirstOrDefault(f => f.Name == "handleSubmit");
        if (handleSubmit != null && !handleSubmit.Body.Contains("normalizeMultipleFields(form)", StringComparison.Ordinal))
        {
            var body = handleSubmit.Body;
            if (body.Contains(".create(form);", StringComparison.Ordinal))
            {
                body = body.Replace("    try { await ", "    const submitData = normalizeMultipleFields(form);\n    try { await ", StringComparison.Ordinal)
                           .Replace(".create(form);", ".create(submitData);", StringComparison.Ordinal);
            }
            else if (body.Contains(".update(form.id, form);", StringComparison.Ordinal))
            {
                body = body.Replace("    try { await ", "    const submitData = normalizeMultipleFields(form);\n    try { await ", StringComparison.Ordinal)
                           .Replace(".update(form.id, form);", ".update(form.id, submitData);", StringComparison.Ordinal);
            }
            handleSubmit.Body = body;
        }

        if (pageType != "edit") return;

        AddOrReplaceFunction(mainSection, new FunctionInfo
        {
            Name = "hydrateMultipleFields",
            Params = "target",
            Body = BuildHydrateMultipleFieldsBody(multipleFields)
        });

        var getDetail = mainSection.Functions.FirstOrDefault(f => f.Name == "getDetail");
        if (getDetail != null && !getDetail.Body.Contains("hydrateMultipleFields(form)", StringComparison.Ordinal))
        {
            getDetail.Body = getDetail.Body.Replace(
                "Object.assign(form, res);",
                "Object.assign(form, res); hydrateMultipleFields(form);",
                StringComparison.Ordinal);
        }
    }

    private static void AddDictLabelHelper(ScriptBuilder.ScriptSection mainSection, List<EntityField> fields, string pageType)
    {
        if (pageType is not ("index" or "detail")) return;
        if (!fields.Any(f => IsChoiceControl(f.FormControlType) && !string.IsNullOrWhiteSpace(f.SelectOptions))) return;

        AddOrReplaceFunction(mainSection, new FunctionInfo
        {
            Name = "getDictLabel",
            Params = "val, options",
            Body = string.Join("\n", new[]
            {
                "if (val === undefined || val === null || val === '') return '-'",
                "if (!options || !options.length) return Array.isArray(val) ? val.join(', ') : val",
                "const resolve = (v) => {",
                "  v = String(v).trim()",
                "  if (!v) return ''",
                "  const item = options.find(o => String(o.value) === v)",
                "  return item?.label ?? v",
                "}",
                "if (Array.isArray(val)) return val.map(resolve).filter(Boolean).join(', ')",
                "if (typeof val === 'string' && val.includes(',')) return val.split(',').map(resolve).filter(Boolean).join(', ')",
                "return resolve(val)"
            })
        });
    }

    private static void AddMultipleQueryNormalizer(ScriptBuilder.ScriptSection mainSection, List<EntityField> fields, string pageType)
    {
        if (pageType != "index") return;

        var multipleFields = fields
            .Where(f => f.IsMultiple && f.ShowInSearch && !f.IsPrimaryKey)
            .ToList();
        if (multipleFields.Count == 0) return;

        AddOrReplaceFunction(mainSection, new FunctionInfo
        {
            Name = "normalizeQueryParams",
            Params = "source",
            Body = BuildNormalizeQueryParamsBody(multipleFields)
        });

        var getList = mainSection.Functions.FirstOrDefault(f => f.Name == "getList");
        if (getList == null || getList.Body.Contains("normalizeQueryParams(queryParams)", StringComparison.Ordinal))
            return;

        getList.Body = getList.Body.Replace(
            "const params = { ...queryParams, pageNum: pagination.page, pageSize: pagination.pageSize };",
            "const params = { ...normalizeQueryParams(queryParams), pageNum: pagination.page, pageSize: pagination.pageSize };",
            StringComparison.Ordinal);
    }

    private static void AddUploadDisplayHelper(ScriptBuilder.ScriptSection mainSection, string vueContent)
    {
        if (!vueContent.Contains("getUploadValues(", StringComparison.Ordinal) &&
            !vueContent.Contains("getUploadFileName(", StringComparison.Ordinal))
            return;

        AddOrReplaceFunction(mainSection, new FunctionInfo
        {
            Name = "getUploadValues",
            Params = "value",
            Body = string.Join("\n", new[]
            {
                "if (!value) return []",
                "if (Array.isArray(value)) return value.filter(Boolean)",
                "return String(value).split(',').map(v => v.trim()).filter(Boolean)"
            })
        });

        AddOrReplaceFunction(mainSection, new FunctionInfo
        {
            Name = "getUploadFileName",
            Params = "url, index = 0",
            Body = string.Join("\n", new[]
            {
                "const fallback = `file_${index + 1}`",
                "if (!url) return fallback",
                "const clean = String(url).split('?')[0].split('#')[0]",
                "const rawName = clean.split('/').filter(Boolean).pop() || fallback",
                "try { return decodeURIComponent(rawName) || rawName }",
                "catch { return rawName }"
            })
        });
    }

    private static string BuildNormalizeMultipleFieldsBody(List<EntityField> fields)
    {
        var lines = new List<string> { "const data = { ...source }" };
        foreach (var field in fields)
        {
            var name = ToCamelCase(field.Name);
            var listName = name + "List";
            lines.Add($"data.{name} = Array.isArray(source.{listName}) ? source.{listName}.join(',') : (source.{listName} ?? source.{name} ?? '')");
        }
        lines.Add("return data");
        return string.Join("\n", lines);
    }

    private static string BuildNormalizeQueryParamsBody(List<EntityField> fields)
    {
        var lines = new List<string> { "const data = { ...source }" };
        foreach (var field in fields)
        {
            var name = ToCamelCase(field.Name);
            lines.Add($"if (Array.isArray(source.{name})) data.{name} = source.{name}.join(',')");
        }
        lines.Add("return data");
        return string.Join("\n", lines);
    }

    private static string BuildHydrateMultipleFieldsBody(List<EntityField> fields)
    {
        var lines = new List<string>();
        foreach (var field in fields)
        {
            var name = ToCamelCase(field.Name);
            var listName = name + "List";
            lines.Add($"target.{listName} = Array.isArray(target.{name}) ? target.{name} : (target.{name} ? String(target.{name}).split(',').map(v => v.trim()).filter(Boolean) : [])");
        }
        return string.Join("\n", lines);
    }

    private static void AddOrReplaceFunction(ScriptBuilder.ScriptSection section, FunctionInfo function)
    {
        var existing = section.Functions.FirstOrDefault(f => f.Name == function.Name);
        if (existing == null)
        {
            section.Functions.Add(function);
            return;
        }

        existing.Params = function.Params;
        existing.IsAsync = function.IsAsync;
        existing.Body = function.Body;
    }

    private static List<Dictionary<string, string>> ParseDisplayFields(string? displayFields)
    {
        var result = new List<Dictionary<string, string>>();
        if (string.IsNullOrWhiteSpace(displayFields)) return result;

        void AddField(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();
            if (result.Any(f => string.Equals(f["name"], name, StringComparison.OrdinalIgnoreCase))) return;
            result.Add(new Dictionary<string, string> { ["name"] = name, ["name_lower"] = ToCamelCase(name) });
        }

        try
        {
            using var doc = JsonDocument.Parse(displayFields);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        AddField(item.GetString());
                    }
                    else if (item.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var key in new[] { "name", "field", "fieldName", "prop", "key" })
                        {
                            if (item.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                            {
                                AddField(prop.GetString());
                                break;
                            }
                        }
                    }
                }
                return result;
            }

            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                foreach (var field in doc.RootElement.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>())
                    AddField(field);
                return result;
            }
        }
        catch
        {
            foreach (var field in displayFields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                AddField(field);
        }

        return result;
    }

    private static string BuildDisplayFieldsArg(List<Dictionary<string, string>> displayFields)
    {
        return string.Join(",", displayFields
            .Select(f => f.TryGetValue("name", out var name) ? name : "")
            .Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static List<DisplayFieldContext> BuildDisplayFieldContexts(
        List<Dictionary<string, string>> displayFields,
        FieldContext owner,
        List<EntityField> relatedFields)
    {
        return displayFields.Select((df, index) =>
        {
            var name = df.TryGetValue("name", out var rawName) ? rawName : string.Empty;
            var nameLower = df.TryGetValue("name_lower", out var rawNameLower) ? rawNameLower : ToCamelCase(name);
            var relatedField = relatedFields.FirstOrDefault(f =>
                string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            var dictOptions = BuildDisplayDictOptionsName(owner, name, relatedField);
            var ctx = new DisplayFieldContext
            {
                Name = name,
                NameLower = nameLower,
                LabelKey = nameLower,
                ItemValueExpr = $"item.{nameLower}",
                DataType = relatedField?.DataType ?? string.Empty,
                FormControlType = relatedField?.FormControlType ?? string.Empty,
                SelectDataSource = relatedField?.SelectDataSource ?? string.Empty,
                SelectOptions = relatedField?.SelectOptions ?? string.Empty,
                DictOptions = dictOptions,
                Index = index
            };

            ctx.ListContent = BuildDisplayFieldContent(ctx, owner, owner.RowPrefix, isDetail: false);
            ctx.DetailContent = BuildDisplayFieldContent(ctx, owner, owner.DetailPrefix, isDetail: true);
            return ctx;
        }).ToList();
    }

    private static string BuildDisplayDictOptionsName(FieldContext owner, string fieldName, EntityField? relatedField)
    {
        if (relatedField == null ||
            !string.Equals(relatedField.FormControlType, "select", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(relatedField.SelectDataSource, "dict", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(relatedField.SelectOptions))
        {
            return string.Empty;
        }

        var prefix = string.IsNullOrWhiteSpace(owner.RelatedEntityNameLower) ? "related" : owner.RelatedEntityNameLower;
        return $"{prefix}{ToJsFunctionSuffix(fieldName)}Options";
    }

    private static string BuildDisplayFieldContent(DisplayFieldContext displayField, FieldContext owner, string source, bool isDetail)
    {
        var sourceExpr = $"{source}.{owner.NameLower}";
        var optionsName = $"{owner.RelatedEntityNameLower}Options";
        var valuesExpr = $"getSelectDisplayValues({sourceExpr}, {optionsName}, '{displayField.NameLower}')";

        if (IsImageDisplayField(displayField))
            return BuildImageDisplayContent(valuesExpr, isDetail);

        if (IsFileDisplayField(displayField))
            return BuildFileDisplayContent(valuesExpr);

        if (IsDateDisplayField(displayField))
        {
            var withTime = string.Equals(displayField.FormControlType, "datetime", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(displayField.DataType, "DateTime", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(displayField.DataType, "DateTime?", StringComparison.OrdinalIgnoreCase);
            var formatArgs = withTime ? ", true" : string.Empty;
            return $"{{{{ {valuesExpr}.map(value => formatDate(value{formatArgs})).filter(Boolean).join(', ') || '-' }}}}";
        }

        if (IsSwitchDisplayField(displayField))
        {
            return $"<span v-if=\"{valuesExpr}.length\"><el-tag v-for=\"(value,index) in {valuesExpr}\" :key=\"index\" :type=\"toDisplayBool(value) ? 'success' : 'danger'\" style=\"margin-right:4px\">{{{{ toDisplayBool(value) ? $t('yes') : $t('no') }}}}</el-tag></span><span v-else>-</span>";
        }

        if (IsDictDisplayField(displayField))
        {
            return $"{{{{ {valuesExpr}.map(value => getDictLabel(value, {displayField.DictOptions})).filter(Boolean).join(', ') || '-' }}}}";
        }

        return $"{{{{ getSelectLabel({sourceExpr}, {optionsName}, '{displayField.NameLower}') }}}}";
    }

    private static string BuildImageDisplayContent(string valuesExpr, bool isDetail)
    {
        var size = isDetail ? 80 : 40;
        return $"<div v-if=\"{valuesExpr}.length\" class=\"cm-image-list\"><el-image v-for=\"(url,index) in {valuesExpr}\" :key=\"index\" :src=\"url\" :preview-src-list=\"{valuesExpr}\" :initial-index=\"index\" fit=\"cover\" style=\"width:{size}px;height:{size}px;margin-right:8px\" /></div><span v-else>-</span>";
    }

    private static string BuildFileDisplayContent(string valuesExpr)
    {
        return BuildUploadLinkDisplayContent(valuesExpr);
    }

    private static string BuildUploadLinkDisplayContent(string valuesExpr)
    {
        return $"<div v-if=\"{valuesExpr}.length\" class=\"cm-file-link-list\"><el-link v-for=\"(url,index) in {valuesExpr}\" :key=\"index\" :href=\"url\" :download=\"getUploadFileName(url, index)\" target=\"_blank\" type=\"primary\" style=\"margin-right:8px\">{{{{ getUploadFileName(url, index) }}}}</el-link></div><span v-else>-</span>";
    }

    /// <summary>鏍规嵁鏄剧ず瀛楁鍒楄〃鐢熸垚 :label 缁戝畾琛ㄨ揪寮?/summary>
    private static string BuildDisplayLabelExpr(List<Dictionary<string, string>> displayFields)
    {
        if (displayFields.Count == 0) return "item.label";
        if (displayFields.Count == 1)
        {
            var df = displayFields[0];
            return $"item.{df["name_lower"]}";
        }
        return "[" + string.Join(", ", displayFields.Select(df => $"item.{df["name_lower"]}")) + "].filter(v => v !== undefined && v !== null && v !== '').join(' / ')";
    }

    private static List<EntityField> GetPageFields(List<EntityField> allFields, string pageType) => pageType switch
    {
        "index" => allFields.Where(f => f.ShowInSearch).ToList(),
        "add" => allFields.Where(f => f.ShowInAddForm && !f.IsPrimaryKey).ToList(),
        "edit" => allFields.Where(f => f.ShowInEditForm && !f.IsPrimaryKey).ToList(),
        "detail" => allFields.Where(f => f.ShowInDetail || ShouldRenderRelatedDisplayField(f)).ToList(),
        _ => allFields
    };
}

public class PageGenerationResult
{
    public string VueContent { get; set; } = string.Empty;
    public string MainScriptContent { get; set; } = string.Empty;
    /// <summary>.vue 鏂囦欢涓殑 import 璇彞</summary>
    public string VueImportLine { get; set; } = string.Empty;
    /// <summary>瀛愯〃 .vue 鏂囦欢涓殑 import 璇彞</summary>
    public string ChildImportLine { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public List<string> ChildEntityNames { get; set; } = new();
    public Dictionary<string, string> GenMarkerMap { get; set; } = new();
    public Dictionary<string, string> RelationMarkerMap { get; set; } = new();
    /// <summary>鍚堝苟鍚庣殑涓昏〃 ScriptSection JSON锛堜緵璁捐鍣ㄧ紪杈戯級</summary>
    public string MainScriptJson { get; set; } = string.Empty;
    public Dictionary<string, ChildPageScript> ChildScripts { get; set; } = new();
    /// <summary>姣忎釜瀛楁鎺т欢鐨?ScriptSection锛宬ey=gen_id锛堝 gen_field_123456789锛?/summary>
    public Dictionary<string, string> FieldScripts { get; set; } = new();
    /// <summary>瀛愯〃姣忎釜瀛楁鎺т欢鐨?ScriptSection锛宬ey=gen_id</summary>
    public Dictionary<string, string> ChildFieldScripts { get; set; } = new();
    /// <summary>鎸夊瓙瀹炰綋鎷嗗垎鐨勫瓧娈?ScriptSection锛宬ey=瀛愬疄浣撳悕锛寁alue=璇ュ瓙瀹炰綋鐨勫瓧娈佃剼鏈?/summary>
    public Dictionary<string, Dictionary<string, string>> ChildFieldScriptsByEntity { get; set; } = new();
    /// <summary>缁勪欢鏍?JSON锛堜緵璁捐鍣ㄥ姞杞斤紝涓嶅啀瑙ｆ瀽 .vue 鏂囦欢锛?/summary>
    public string TreeJson { get; set; } = string.Empty;
}

public class ChildPageScript
{
    public string EntityName { get; set; } = string.Empty;
    public string ScriptContent { get; set; } = string.Empty;
    public string ScriptJson { get; set; } = string.Empty;
    public List<string> ExportNames { get; set; } = new();
}
