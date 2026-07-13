using System.Text.RegularExpressions;

namespace CodeMaster.Application.Services.CodeGen.Marker;

/// <summary>
/// 标记替换引擎 —— 替换模板中的 [gen.*] / [relation.*] / [field.*] 标记
/// </summary>
public static class MarkerReplacer
{
    // 匹配 [gen.xxx] 标记
    private static readonly Regex GenMarker = new(@"\[gen\.(\w+)\]", RegexOptions.Compiled);

    // 匹配 [relation.xxx] 标记
    private static readonly Regex RelationMarker = new(@"\[relation\.(\w+)\]", RegexOptions.Compiled);

    // 匹配 [field.xxx] 标记
    private static readonly Regex FieldMarker = new(@"\[field\.(\w+)\]", RegexOptions.Compiled);

    private static readonly Regex FieldDisplayLoopElement = new(
        @"<(?<tag>[\w:-]+)(?<before>[^<>]*?)\s+\[v-for\s*=\s*(?<quote>[""'])(?<alias>[A-Za-z_]\w*)\s+in\s+field\.(?:displayFields|DisplayFields)\k<quote>\](?<after>[^<>]*?)>(?<body>.*?)</\k<tag>>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex FieldDisplayLoopSelfClosingElement = new(
        @"<(?<tag>[\w:-]+)(?<before>[^<>]*?)\s+\[v-for\s*=\s*(?<quote>[""'])(?<alias>[A-Za-z_]\w*)\s+in\s+field\.(?:displayFields|DisplayFields)\k<quote>\](?<after>[^<>]*?)/>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex DisplayFieldMarker = new(@"\[(?<alias>[A-Za-z_]\w*)\.(?<key>\w+)\]", RegexOptions.Compiled);

    /// <summary>
    /// 替换主模板中的 [gen.*] 标记
    /// </summary>
    public static string ReplaceGen(string template, GenContext ctx)
    {
        return GenMarker.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return key switch
            {
                "entityName" => ctx.EntityName,
                "entityNameLower" => ctx.EntityNameLower,
                "entityNameAllLower" => ctx.EntityNameAllLower,
                "entityTitleKey" => ctx.EntityTitleKey,
                "entityDescription" or "description" => ctx.EntityDescription,
                "moduleName" => ctx.ModuleName,
                "moduleNameLower" => ctx.ModuleNameLower,
                "tableName" => ctx.TableName,
                "addColumns" => ctx.ColumnHtml.GetValueOrDefault("add", string.Empty),
                "editColumns" => ctx.ColumnHtml.GetValueOrDefault("edit", string.Empty),
                "searchColumns" => ctx.ColumnHtml.GetValueOrDefault("search", string.Empty),
                "listColumns" => ctx.ColumnHtml.GetValueOrDefault("list", string.Empty),
                "detailColumns" => ctx.ColumnHtml.GetValueOrDefault("detail", string.Empty),
                "relationCards" => ctx.RelationCards,
                "relationDialogs" => ctx.RelationDialogs,
                _ => match.Value
            };
        });
    }

    private static string ExpandFieldDisplayLoops(string template, FieldContext ctx)
    {
        template = FieldDisplayLoopSelfClosingElement.Replace(template, match =>
        {
            var alias = match.Groups["alias"].Value;
            var tag = match.Groups["tag"].Value;
            var attrs = NormalizeAttributes(match.Groups["before"].Value + match.Groups["after"].Value);
            var node = $"<{tag}{attrs} />";
            return string.Concat(ctx.DisplayFields.Select(displayField => ReplaceDisplayFieldMarkers(node, alias, displayField)));
        });

        return FieldDisplayLoopElement.Replace(template, match =>
        {
            var alias = match.Groups["alias"].Value;
            var tag = match.Groups["tag"].Value;
            var attrs = NormalizeAttributes(match.Groups["before"].Value + match.Groups["after"].Value);
            var body = match.Groups["body"].Value;
            return string.Concat(ctx.DisplayFields.Select(displayField =>
            {
                var node = $"<{tag}{attrs}>{body}</{tag}>";
                return ReplaceDisplayFieldMarkers(node, alias, displayField);
            }));
        });
    }

    private static string NormalizeAttributes(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        value = Regex.Replace(value, @"\s+", " ").Trim();
        return value.Length == 0 ? string.Empty : " " + value;
    }

    private static string ReplaceDisplayFieldMarkers(string template, string alias, DisplayFieldContext ctx)
    {
        return DisplayFieldMarker.Replace(template, match =>
        {
            if (!string.Equals(match.Groups["alias"].Value, alias, StringComparison.Ordinal))
                return match.Value;

            return match.Groups["key"].Value switch
            {
                "name" => ctx.Name,
                "nameLower" => ctx.NameLower,
                "labelKey" => ctx.LabelKey,
                "itemValueExpr" => ctx.ItemValueExpr,
                "dataType" => ctx.DataType,
                "formControlType" => ctx.FormControlType,
                "selectDataSource" => ctx.SelectDataSource,
                "selectOptions" => ctx.SelectOptions,
                "dictOptions" => ctx.DictOptions,
                "listContent" => ctx.ListContent,
                "detailContent" => ctx.DetailContent,
                "index" => ctx.Index.ToString(),
                _ => match.Value
            };
        });
    }

    /// <summary>
    /// 替换子表模板中的 [relation.*] 标记
    /// </summary>
    public static string ReplaceRelation(string template, RelationContext ctx)
    {
        return RelationMarker.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return key switch
            {
                "entityName" => ctx.ChildEntityName,
                "entityNameLower" => ctx.ChildEntityNameLower,
                "entityNameAllLower" => ctx.ChildEntityNameAllLower,
                "entityTitleKey" or "childEntityTitleKey" => ctx.ChildEntityTitleKey,
                "collectionName" => ctx.CollectionName,
                "formName" => ctx.FormName,
                "formRefName" => ctx.FormRefName,
                "editingIndexName" => ctx.EditingIndexName,
                "dialogVisibleName" => ctx.DialogVisibleName,
                "description" => ctx.ChildEntityDescription,
                "foreignKey" => ctx.ChildForeignKey,
                "masterField" => ctx.MasterField,
                "tableColumns" or "cardColumns" => ctx.TableColumns,
                "dialogColumns" => ctx.DialogColumns,
                _ => match.Value
            };
        });
    }

    /// <summary>
    /// 替换控件模板中的 [field.*] 标记
    /// </summary>
    public static string ReplaceField(string template, FieldContext ctx)
    {
        template = ExpandFieldDisplayLoops(template, ctx);
        return FieldMarker.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return key switch
            {
                "id" or "fieldId" => ctx.Id,
                "name" => ctx.Name,
                "nameLower" => ctx.NameLower,
                "labelKey" => ctx.LabelKey,
                "description" => string.IsNullOrEmpty(ctx.Description) ? ctx.Name : ctx.Description,
                "dataType" => ctx.DataType,
                "isNullable" => ctx.IsNullable.ToString().ToLower(),
                "isPrimaryKey" => ctx.IsPrimaryKey.ToString().ToLower(),
                "isRequired" => ctx.IsRequired ? "required" : "",
                "required" => ctx.IsRequired ? "required" : "",
                "sortable" => ctx.IsSortable ? "sortable" : "",
                "formControlType" => ctx.FormControlType,
                "selectDataSource" => ctx.SelectDataSource,
                "selectOptions" => ctx.SelectOptions ?? string.Empty,
                "relatedEntityName" => ctx.RelatedEntityName,
                "relatedEntityNameLower" => ctx.RelatedEntityNameLower,
                "relatedModuleNameLower" => ctx.RelatedModuleNameLower,
                "relatedEntityIdField" => ctx.RelatedEntityIdField,
                "relatedEntityIdFieldLower" => ctx.RelatedEntityIdFieldLower,
                "relatedDisplayFields" => ctx.RelatedDisplayFields,
                "relatedDisplayLabel" => ctx.RelatedDisplayLabel,
                "relatedDisplayFieldsArg" => ctx.RelatedDisplayFieldsArg,
                "relatedDisplayLabelExpr" => ctx.RelatedDisplayLabelExpr,
                "multipleSuffix" => ctx.MultipleSuffix,
                "multipleAttr" => ctx.MultipleAttr,
                "refName" => ctx.RefName,
                "col" => ctx.Col,
                "prop" => ctx.Prop,
                "dictDataUrl" => ctx.DictDataUrl,
                "dictOptions" => ctx.NameLower + "Options",
                "enumTypeName" => ctx.EnumTypeName,
                "maxLength" => ctx.MaxLength.ToString(),
                "minLength" => ctx.MinLength.ToString(),
                "defaultValue" => ctx.DefaultValue,
                "placeholder" => ctx.Placeholder,
                "formPrefix" => ctx.FormPrefix,
                "entityTable" => ctx.EntityTable,
                "entityField" => ctx.EntityField,
                _ => match.Value
            };
        });
    }

    /// <summary>
    /// 从模板中提取所有 [field.*] 标记名（用于检测模板支持哪些字段属性）
    /// </summary>
    public static HashSet<string> ExtractFieldMarkers(string template)
    {
        var markers = new HashSet<string>();
        foreach (Match m in FieldMarker.Matches(template))
            markers.Add(m.Groups[1].Value);
        return markers;
    }
}
