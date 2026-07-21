namespace CodeMaster.Application.Services.CodeGen.Marker;

/// <summary>
/// 实体上下文 —— 用于替换 [gen.*] 标记
/// </summary>
public class GenContext
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityNameLower { get; set; } = string.Empty;
    public string EntityNameAllLower { get; set; } = string.Empty;
    public string EntityTitleKey { get; set; } = string.Empty;
    public string EntityDescription { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleNameLower { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;

    /// <summary>字段控件 HTML 拼接结果（按页面区域：addColumns / editColumns / searchColumns / listColumns / detailColumns）</summary>
    public Dictionary<string, string> ColumnHtml { get; set; } = new();

    /// <summary>子表卡片 HTML 拼接结果</summary>
    public string RelationCards { get; set; } = string.Empty;

    /// <summary>子表 Dialog HTML 拼接结果</summary>
    public string RelationDialogs { get; set; } = string.Empty;
}

/// <summary>
/// 子表上下文 —— 用于替换 [relation.*] 标记
/// </summary>
public class RelationContext
{
    public string ChildEntityName { get; set; } = string.Empty;
    public string ChildEntityNameLower { get; set; } = string.Empty;
    public string ChildEntityNameAllLower { get; set; } = string.Empty;
    public string ChildEntityTitleKey { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public string FormRefName { get; set; } = string.Empty;
    public string EditingIndexName { get; set; } = string.Empty;
    public string DialogVisibleName { get; set; } = string.Empty;
    public string ChildEntityDescription { get; set; } = string.Empty;
    public string ChildForeignKey { get; set; } = string.Empty;
    public string MasterField { get; set; } = string.Empty;

    /// <summary>子表表格列 HTML</summary>
    public string TableColumns { get; set; } = string.Empty;

    /// <summary>子表 Dialog 表单字段 HTML</summary>
    public string DialogColumns { get; set; } = string.Empty;
}

/// <summary>
/// 字段上下文 —— 用于替换 [field.*] 标记
/// </summary>
public class FieldContext
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameLower { get; set; } = string.Empty;
    public string LabelKey { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSortable { get; set; }
    public string FormControlType { get; set; } = string.Empty;
    public string SelectDataSource { get; set; } = string.Empty;
    public string SelectOptions { get; set; } = string.Empty;
    public string SelectOptionsLiteral { get; set; } = "[]";
    public string RelatedEntityName { get; set; } = string.Empty;
    public string RelatedEntityNameLower { get; set; } = string.Empty;
    public string RelatedModuleNameLower { get; set; } = string.Empty;
    public string RelatedEntityIdField { get; set; } = string.Empty;
    public string RelatedEntityIdFieldLower { get; set; } = string.Empty;
    public string RelatedDisplayFields { get; set; } = string.Empty;
    public string RelatedDisplayLabel { get; set; } = string.Empty;
    public string RelatedDisplayFieldsArg { get; set; } = string.Empty;
    public string RelatedDisplayLabelExpr { get; set; } = string.Empty;
    public List<DisplayFieldContext> DisplayFields { get; set; } = new();
    public string MultipleSuffix { get; set; } = string.Empty;
    public string MultipleAttr { get; set; } = string.Empty;
    public string RefName { get; set; } = string.Empty;
    public string Col { get; set; } = string.Empty;
    public string Prop { get; set; } = string.Empty;
    public string DictDataUrl { get; set; } = string.Empty;
    public string EnumTypeName { get; set; } = string.Empty;
    public int MaxLength { get; set; }
    public int MinLength { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    /// <summary>表单数据前缀: form (主表) 或 orderItemForm (子表)</summary>
    public string FormPrefix { get; set; } = "form";

    /// <summary>列表行数据前缀: scope.row（主表）或 scope.row.detail（组成实体）</summary>
    public string RowPrefix { get; set; } = "scope.row";

    /// <summary>详情数据前缀: detail（主表）或 detail.detail（组成实体）</summary>
    public string DetailPrefix { get; set; } = "detail";

    /// <summary>所属表名: 空=主表, "OrderItem"=子表</summary>
    public string EntityTable { get; set; } = string.Empty;

    /// <summary>所属字段名</summary>
    public string EntityField { get; set; } = string.Empty;
    public List<SelectTableMappingContext> ResultMappings { get; set; } = new();
    public string MappingHandlerName { get; set; } = string.Empty;
}

public class SelectTableMappingContext
{
    public string SourceField { get; set; } = string.Empty;
    public string SourceFieldLower { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string TargetFieldLower { get; set; } = string.Empty;
}

public class DisplayFieldContext
{
    public string Name { get; set; } = string.Empty;
    public string NameLower { get; set; } = string.Empty;
    public string LabelKey { get; set; } = string.Empty;
    public string ItemValueExpr { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string FormControlType { get; set; } = string.Empty;
    public string SelectDataSource { get; set; } = string.Empty;
    public string SelectOptions { get; set; } = string.Empty;
    public string DictOptions { get; set; } = string.Empty;
    public string ListContent { get; set; } = string.Empty;
    public string DetailContent { get; set; } = string.Empty;
    public int Index { get; set; }
}
