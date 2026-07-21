using System.ComponentModel;
using CodeMaster.Application.Dtos.CodeGen;

namespace CodeMaster.Agent.Contracts;

public sealed class AiConversationDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public long ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastMessageAt { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class CreateAiConversationRequest
{
    public long ProjectId { get; set; }
    public long ProviderId { get; set; }
    public string? Title { get; set; }
}

public sealed class AiMessageDto
{
    public long Id { get; set; }
    public string? RequestId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public int Sequence { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class SendAiMessageRequest
{
    public long ConversationId { get; set; }
    public string? RequestId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public sealed class AiChatResult
{
    public AiMessageDto Message { get; set; } = new();
    public List<AiToolExecutionDto> PendingApprovals { get; set; } = new();
}

public sealed class AiToolExecutionDto
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long ProjectId { get; set; }
    public string? RequestId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class CreateModuleProposal
{
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleDescription { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int OrderNum { get; set; }
    public string? RoutePath { get; set; }
    public string? Remark { get; set; }
}

public sealed class CreateEntityProposal
{
    public long ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Description("Optional custom physical table name. Leave null for CodeMaster's default plural snake_case name without a prefix. Set only when the user explicitly requests a custom table name; never invent a prefix.")]
    public string? TableName { get; set; }
    [Description("Whether to generate the standard long Id field and IEntity<long>. Writable and tree entities require this. Keyless entities must be read-only.")]
    public bool HasPrimaryKey { get; set; } = true;
    [Description("Adds ITree, ParentId, and Ancestors. Tree entities must have a primary key.")]
    public bool IsTree { get; set; }
    [Description("Disables create/update/delete. Keyed read-only entities keep GetById; keyless read-only entities expose list/query/export only.")]
    public bool IsReadOnly { get; set; }
    [Description("Adds ITenant and the protected TenantId system field.")]
    public bool HasTenant { get; set; }
    [Description("Adds IDept and protected DeptId, DeptAncestors, and CreateUserId system fields.")]
    public bool HasDataPermission { get; set; }
    [Description("Adds IAuditEntity and CreateUserId/CreateBy/CreateTime/UpdateUserId/UpdateBy/UpdateTime system fields.")]
    public bool HasAudit { get; set; } = true;
    [Description("Adds ISoftDelete and IsDeleted/DeleteTime/DeleteBy/DeleteUserId system fields.")]
    public bool HasSoftDelete { get; set; } = true;
    public bool GenerateFrontend { get; set; } = true;
    public bool IsChildTable { get; set; }
    public string? FrontendRoute { get; set; }
    public string? MenuIcon { get; set; }
    public int OrderNum { get; set; }
    public string? Remark { get; set; }
    public List<CreateEntityFieldProposal> Fields { get; set; } = new();
    public List<CreateEntityRelationProposal> Relations { get; set; } = new();
}

public sealed class CreateEntityFieldProposal
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
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
    public bool ShowInList { get; set; } = true;
    public bool ShowInDetail { get; set; } = true;
    public bool ShowInAddForm { get; set; } = true;
    public bool ShowInEditForm { get; set; } = true;
    public bool ShowInSearch { get; set; }
    [Description("UI control: input, textarea, number, select, select-table, date, datetime, switch, checkbox, radio-group, checkbox-group, file, image, editor, cascader, or table-column.")]
    public string FormControlType { get; set; } = "input";
    public int? ListWidth { get; set; }
    public int OrderNum { get; set; }
    public string? SelectDataSource { get; set; }
    public string? SelectOptions { get; set; }
    public bool IsMultiple { get; set; }
    public string? RelatedEntityName { get; set; }
    public string? RelatedEntityIdField { get; set; }
    public string? RelatedEntityDisplayFields { get; set; }
    public string? ResultMappings { get; set; }
    [Description("Normal, Computed, or Aggregate. Computed and Aggregate fields are generated as read-only calculated form values.")]
    public string FieldCategory { get; set; } = "Normal";
    [Description("For Computed fields, an arithmetic expression using [FieldName] references, for example [Price]*[Quantity].")]
    public string? Formula { get; set; }
    [Description("For Aggregate fields: Sum, Avg, or Concat.")]
    public string? AggregateType { get; set; }
    [Description("Existing child entity id from an owned one-to-many relation. Create the relation first when the id is not yet known.")]
    public long? AggregateChildEntityId { get; set; }
    [Description("Child field used by the aggregate. Sum/Avg require numeric fields; Concat requires text.")]
    public string? AggregateChildFieldName { get; set; }
    [Description("Optional separator for Concat aggregates.")]
    public string? AggregateSeparator { get; set; }
    public string? Remark { get; set; }
}

public sealed class CreateEntityRelationProposal
{
    [Description("Existing target entity id. Use TargetEntityName instead when the target is created in the same change set.")]
    public long TargetEntityId { get; set; }

    [Description("Target entity name. Required when the target is created in the same change set.")]
    public string? TargetEntityName { get; set; }

    [Description("Navigation/aggregate property name, for example Items, Detail, or Customer.")]
    public string RelationName { get; set; } = string.Empty;

    [Description("Field on the source entity. OneToMany/OwnedOneToOne: principal primary key, normally Id. ManyToOne Reference: dependent foreign key, for example CustomerId.")]
    public string SourceField { get; set; } = string.Empty;

    [Description("Field on the target entity. OneToMany/OwnedOneToOne: dependent foreign key, for example OrderId. ManyToOne Reference: target primary key, normally Id.")]
    public string TargetField { get; set; } = string.Empty;

    [Description("OneToOne, OneToMany, or ManyToOne. ManyToMany is not currently supported.")]
    public string Cardinality { get; set; } = string.Empty;

    [Description("Owned for aggregate child relations; Reference for foreign-key references to an independent entity.")]
    public string Ownership { get; set; } = string.Empty;
    public bool IsRequired { get; set; }

    [Description("Delete for owned aggregate children; Restrict or Keep for independent references.")]
    public string DeleteBehavior { get; set; } = string.Empty;
    public int OrderNum { get; set; }
}

public sealed class ProjectChangeSetProposal
{
    public string Summary { get; set; } = string.Empty;
    public List<CreateModuleProposal> Modules { get; set; } = new();
    public List<CreateEntityProposal> Entities { get; set; } = new();
    public List<UpdateModuleProposal> ModuleUpdates { get; set; } = new();
    public List<UpdateEntityProposal> EntityUpdates { get; set; } = new();
    public List<long> DeleteEntityIds { get; set; } = new();
    public List<long> DeleteModuleIds { get; set; } = new();
    public string GenerationMode { get; set; } = "None";
    public List<long> GenerationEntityIds { get; set; } = new();
    public bool BuildAfterGeneration { get; set; }
}

public sealed class UpdateModuleProposal
{
    public long ModuleId { get; set; }
    public string? ModuleName { get; set; }
    public string? ModuleDescription { get; set; }
    public string? Icon { get; set; }
    public int? OrderNum { get; set; }
    public string? RoutePath { get; set; }
    public string? Remark { get; set; }
}

public sealed class UpdateEntityProposal
{
    public long EntityId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    [Description("Optional custom physical table name. Omit to keep the current value. Use an empty string only when the user explicitly asks to restore CodeMaster's default unprefixed table name.")]
    public string? TableName { get; set; }
    [Description("Whether the entity has the standard long Id primary key. Setting false also requires IsReadOnly=true and IsTree=false.")]
    public bool? HasPrimaryKey { get; set; }
    [Description("Tree entities require HasPrimaryKey=true.")]
    public bool? IsTree { get; set; }
    [Description("Keyed read-only keeps GetById; keyless read-only exposes list/query/export only.")]
    public bool? IsReadOnly { get; set; }
    public bool? HasTenant { get; set; }
    public bool? HasDataPermission { get; set; }
    public bool? HasAudit { get; set; }
    public bool? HasSoftDelete { get; set; }
    public bool? GenerateFrontend { get; set; }
    public bool? IsChildTable { get; set; }
    public string? FrontendRoute { get; set; }
    public string? MenuIcon { get; set; }
    public int? OrderNum { get; set; }
    public string? Remark { get; set; }
    public List<CreateEntityFieldProposal> NewFields { get; set; } = new();
    public List<UpdateEntityFieldProposal> UpdatedFields { get; set; } = new();
    public List<long> DeletedFieldIds { get; set; } = new();
    public List<CreateEntityRelationProposal> NewRelations { get; set; } = new();
    public List<UpdateEntityRelationProposal> UpdatedRelations { get; set; } = new();
    public List<long> DeletedRelationIds { get; set; } = new();
}

public sealed class UpdateEntityFieldProposal
{
    public long FieldId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? DataType { get; set; }
    public bool? IsNullable { get; set; }
    public int? MaxLength { get; set; }
    public bool ClearMaxLength { get; set; }
    public int? Precision { get; set; }
    public bool ClearPrecision { get; set; }
    public int? Scale { get; set; }
    public bool ClearScale { get; set; }
    public string? DefaultValue { get; set; }
    public bool? IsIgnore { get; set; }
    public bool? IsPrimaryKey { get; set; }
    public bool? IsRequired { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public string? RegexPattern { get; set; }
    public bool? IsEmail { get; set; }
    public bool? IsPhone { get; set; }
    public bool? ShowInList { get; set; }
    public bool? ShowInDetail { get; set; }
    public bool? ShowInAddForm { get; set; }
    public bool? ShowInEditForm { get; set; }
    public bool? ShowInSearch { get; set; }
    [Description("UI control type. Supported values match CreateEntityFieldProposal.FormControlType.")]
    public string? FormControlType { get; set; }
    public int? ListWidth { get; set; }
    public bool ClearListWidth { get; set; }
    public int? OrderNum { get; set; }
    public string? SelectDataSource { get; set; }
    public string? SelectOptions { get; set; }
    public bool? IsMultiple { get; set; }
    public string? RelatedEntityName { get; set; }
    public string? RelatedEntityIdField { get; set; }
    public string? RelatedEntityDisplayFields { get; set; }
    public string? ResultMappings { get; set; }
    [Description("Normal, Computed, or Aggregate.")]
    public string? FieldCategory { get; set; }
    [Description("Computed arithmetic formula using [FieldName] references.")]
    public string? Formula { get; set; }
    [Description("Aggregate type: Sum, Avg, or Concat.")]
    public string? AggregateType { get; set; }
    [Description("Existing child entity id from an owned one-to-many relation.")]
    public long? AggregateChildEntityId { get; set; }
    public bool ClearAggregateChildEntityId { get; set; }
    [Description("Child source field used by the aggregate.")]
    public string? AggregateChildFieldName { get; set; }
    public string? AggregateSeparator { get; set; }
    public string? Remark { get; set; }
}

public sealed class UpdateEntityRelationProposal
{
    public long RelationId { get; set; }
    public long? TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }
    public string? RelationName { get; set; }
    public string? SourceField { get; set; }
    public string? TargetField { get; set; }
    public string? Cardinality { get; set; }
    public string? Ownership { get; set; }
    public bool? IsRequired { get; set; }
    public string? DeleteBehavior { get; set; }
    public int? OrderNum { get; set; }
}

public sealed class ProjectChangeSetValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int ModuleCount { get; set; }
    public int EntityCount { get; set; }
    public int FieldCount { get; set; }
    public int RelationCount { get; set; }
    public int UpdatedModuleCount { get; set; }
    public int UpdatedEntityCount { get; set; }
    public int DeletedModuleCount { get; set; }
    public int DeletedEntityCount { get; set; }
}

public sealed class ProjectChangeSetExecutionResult
{
    public long ProjectId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<ProjectChangeCreatedItem> CreatedModules { get; set; } = new();
    public List<ProjectChangeCreatedItem> CreatedEntities { get; set; } = new();
    public List<ProjectChangeCreatedItem> UpdatedModules { get; set; } = new();
    public List<ProjectChangeCreatedItem> UpdatedEntities { get; set; } = new();
    public List<long> DeletedModuleIds { get; set; } = new();
    public List<long> DeletedEntityIds { get; set; } = new();
    public int CreatedRelationCount { get; set; }
    public List<AiClientActionDto> ClientActions { get; set; } = new();
    public string? ClientExecutionOutput { get; set; }
    public string? ClientExecutionFailedAction { get; set; }
    public string? ClientExecutionError { get; set; }
}

public sealed class UiPageEnhancementProposal
{
    [Description("Short user-facing summary of the visual change.")]
    public string Summary { get; set; } = string.Empty;

    [Description("Scaffold for Login/Dashboard, or EntityPage for index/add/edit/detail.")]
    public string TargetKind { get; set; } = "EntityPage";

    [Description("Login or Dashboard when TargetKind=Scaffold.")]
    public string? Page { get; set; }

    [Description("Existing entity id when TargetKind=EntityPage.")]
    public long? EntityId { get; set; }

    [Description("Existing entity name alternative when EntityId is not supplied.")]
    public string? EntityName { get; set; }

    [Description("index, add, edit, or detail when TargetKind=EntityPage.")]
    public string? PageType { get; set; }

    [Description("Enterprise, Technology, Industrial, Commerce, or Minimal.")]
    public string Style { get; set; } = "Enterprise";

    public string? Headline { get; set; }

    public string? Subtitle { get; set; }

    public List<string> Highlights { get; set; } = new();

    [Description("Optional six-digit hex color such as #2563eb.")]
    public string? PrimaryColor { get; set; }

    [Description("Optional six-digit hex color such as #0f766e.")]
    public string? SecondaryColor { get; set; }

    [Description("Replace prior Agent design operations instead of merging them.")]
    public bool ReplaceExistingDesign { get; set; }

    [Description("Structured operations anchored by stable genId values. Supported types: SetTag, SetProp, RemoveProp, SetGrid, Move, Group.")]
    public List<ProjectUiNodeOperationDto> Operations { get; set; } = new();

    public bool BuildAfterApply { get; set; } = true;
}

public sealed class UiPageEnhancementExecutionResult
{
    public long ProjectId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string TargetKind { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
    public List<AiClientActionDto> ClientActions { get; set; } = new();
    public string? ClientExecutionOutput { get; set; }
    public string? ClientExecutionFailedAction { get; set; }
    public string? ClientExecutionError { get; set; }
}

public sealed class ProjectChangeCreatedItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class EntityBlueprintQuery
{
    public long EntityId { get; set; }
    public string? EntityName { get; set; }
}

public sealed class UiPageBlueprintQuery
{
    public long? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string PageType { get; set; } = "index";
}

public sealed class AiClientActionDto
{
    public string Action { get; set; } = string.Empty;
    public long ProjectId { get; set; }
    public long? EntityId { get; set; }
    public List<long> EntityIds { get; set; } = new();
    public string? TargetKind { get; set; }
    public string? Page { get; set; }
    public string? PageType { get; set; }
    public string? Style { get; set; }
    public string? Headline { get; set; }
    public string? Subtitle { get; set; }
    public List<string> Highlights { get; set; } = new();
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public bool ReplaceExistingDesign { get; set; }
    public List<ProjectUiNodeOperationDto> Operations { get; set; } = new();
}

public sealed class CompleteAiClientActionsRequest
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FailedAction { get; set; }
    public string? DiagnosticOutput { get; set; }
}

public sealed class CompleteAiClientActionsResult
{
    public AiToolExecutionDto Execution { get; set; } = new();
    public AiMessageDto? RepairMessage { get; set; }
    public List<AiToolExecutionDto> PendingApprovals { get; set; } = new();
    public int RepairAttempt { get; set; }
    public bool AutomaticRepairStopped { get; set; }
    public string? AnalysisError { get; set; }
}
