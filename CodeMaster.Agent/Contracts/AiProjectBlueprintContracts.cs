namespace CodeMaster.Agent.Contracts;

public sealed class AiProjectBlueprintDto
{
    public int SchemaVersion { get; set; } = 1;
    public AiProjectSummaryDto Project { get; set; } = new();
    public List<AiModuleBlueprintDto> Modules { get; set; } = new();
    public List<AiControlCatalogDto> Controls { get; set; } = new();
    public List<AiPageTemplateCatalogDto> PageTemplates { get; set; } = new();
    public List<AiChildTemplateCatalogDto> ChildTemplates { get; set; } = new();
}

public sealed class AiProjectSummaryDto
{
    public long Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DatabaseType { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? FrontendPort { get; set; }
    public int? BackendPort { get; set; }
    public DateTime? InitializedAt { get; set; }
}

public sealed class AiModuleBlueprintDto
{
    public long Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleDescription { get; set; } = string.Empty;
    public string? RoutePath { get; set; }
    public string? Icon { get; set; }
    public int OrderNum { get; set; }
    public bool IsSynced { get; set; }
    public List<AiEntityBlueprintDto> Entities { get; set; } = new();
}

public sealed class AiEntityBlueprintDto
{
    public long Id { get; set; }
    public long ModuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public bool HasPrimaryKey { get; set; }
    public bool IsTree { get; set; }
    public bool IsReadOnly { get; set; }
    public bool HasTenant { get; set; }
    public bool HasDataPermission { get; set; }
    public bool HasAudit { get; set; }
    public bool HasSoftDelete { get; set; }
    public bool GenerateFrontend { get; set; }
    public bool IsChildTable { get; set; }
    public bool IsGenerated { get; set; }
    public DateTime? LastGeneratedTime { get; set; }
    public string? FrontendRoute { get; set; }
    public string? MenuIcon { get; set; }
    public int OrderNum { get; set; }
    public string? Remark { get; set; }
    public List<AiFieldBlueprintDto> Fields { get; set; } = new();
    public List<AiRelationBlueprintDto> Relations { get; set; } = new();
    public List<AiLegacyOneToManyBlueprintDto> LegacyOneToManyRelations { get; set; } = new();
}

public sealed class AiUiPageBlueprintDto
{
    public long EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityDescription { get; set; } = string.Empty;
    public string PageType { get; set; } = string.Empty;
    public List<AiUiStableNodeDto> StableNodes { get; set; } = new();
    public List<AiUiFieldNodeDto> Fields { get; set; } = new();
    public List<string> SupportedOperations { get; set; } =
        ["SetTag", "SetProp", "RemoveProp", "SetGrid", "Move", "Group"];
}

public sealed class AiUiStableNodeDto
{
    public string GenId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class AiUiFieldNodeDto
{
    public long FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public List<string> GenIds { get; set; } = new();
    public bool Visible { get; set; }
    public int OrderNum { get; set; }
}

public sealed class AiFieldBlueprintDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemField { get; set; }
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsIgnore { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsRequired { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public string? RegexPattern { get; set; }
    public bool IsEmail { get; set; }
    public bool IsPhone { get; set; }
    public bool ShowInList { get; set; }
    public bool ShowInDetail { get; set; }
    public bool ShowInAddForm { get; set; }
    public bool ShowInEditForm { get; set; }
    public bool ShowInSearch { get; set; }
    public string FormControlType { get; set; } = string.Empty;
    public int? ListWidth { get; set; }
    public int OrderNum { get; set; }
    public string? SelectDataSource { get; set; }
    public string? SelectOptions { get; set; }
    public bool IsMultiple { get; set; }
    public string? RelatedEntityName { get; set; }
    public string? RelatedEntityIdField { get; set; }
    public string? RelatedEntityDisplayFields { get; set; }
    public string? ResultMappings { get; set; }
    public string FieldCategory { get; set; } = string.Empty;
    public string? Formula { get; set; }
    public string? AggregateType { get; set; }
    public long? AggregateChildEntityId { get; set; }
    public string? AggregateChildFieldName { get; set; }
    public string? AggregateSeparator { get; set; }
    public string? Remark { get; set; }
}

public sealed class AiRelationBlueprintDto
{
    public long Id { get; set; }
    public long SourceEntityId { get; set; }
    public long TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }
    public string? TargetEntityDescription { get; set; }
    public string RelationName { get; set; } = string.Empty;
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string Cardinality { get; set; } = string.Empty;
    public string Ownership { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string DeleteBehavior { get; set; } = string.Empty;
    public int OrderNum { get; set; }
}

public sealed class AiLegacyOneToManyBlueprintDto
{
    public long Id { get; set; }
    public string MasterField { get; set; } = string.Empty;
    public long ChildEntityId { get; set; }
    public string ChildEntityName { get; set; } = string.Empty;
    public string ChildForeignKey { get; set; } = string.Empty;
    public int OrderNum { get; set; }
}

public sealed class AiControlCatalogDto
{
    public string ControlType { get; set; } = string.Empty;
    public List<string> PageSections { get; set; } = new();
}

public sealed class AiPageTemplateCatalogDto
{
    public string PageType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}

public sealed class AiChildTemplateCatalogDto
{
    public string PageType { get; set; } = string.Empty;
    public string ChildType { get; set; } = string.Empty;
}
