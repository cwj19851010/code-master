using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Application.Services.CodeGen;

internal sealed record SystemEntityFieldDefinition(
    string Name,
    string Description,
    string DataType,
    bool IsNullable = false,
    bool IsPrimaryKey = false,
    bool IsRequired = false,
    int? MaxLength = null,
    string? DefaultValue = null,
    bool ShowInAddForm = false,
    bool ShowInEditForm = false);

internal static class SystemEntityFieldSynchronizer
{
    private static readonly SystemEntityFieldDefinition Id =
        new("Id", "主键", "long", IsPrimaryKey: true, IsRequired: true);

    private static readonly SystemEntityFieldDefinition ParentId =
        new("ParentId", "父级ID", "long?", IsNullable: true, ShowInAddForm: true, ShowInEditForm: true);

    private static readonly SystemEntityFieldDefinition Ancestors =
        new("Ancestors", "祖级列表", "string?", IsNullable: true);

    private static readonly SystemEntityFieldDefinition TenantId =
        new("TenantId", "租户ID", "long", IsRequired: true);

    private static readonly SystemEntityFieldDefinition DeptId =
        new("DeptId", "部门ID", "long?", IsNullable: true);

    private static readonly SystemEntityFieldDefinition DeptAncestors =
        new("DeptAncestors", "部门祖级", "string?", IsNullable: true);

    private static readonly SystemEntityFieldDefinition CreateUserId =
        new("CreateUserId", "创建人ID", "long?", IsNullable: true);

    private static readonly SystemEntityFieldDefinition CreateBy =
        new("CreateBy", "创建人", "string?", IsNullable: true, MaxLength: 64);

    private static readonly SystemEntityFieldDefinition CreateTime =
        new("CreateTime", "创建时间", "DateTime", DefaultValue: "DateTime.UtcNow");

    private static readonly SystemEntityFieldDefinition UpdateUserId =
        new("UpdateUserId", "更新人ID", "long?", IsNullable: true);

    private static readonly SystemEntityFieldDefinition UpdateBy =
        new("UpdateBy", "更新人", "string?", IsNullable: true, MaxLength: 64);

    private static readonly SystemEntityFieldDefinition UpdateTime =
        new("UpdateTime", "更新时间", "DateTime?", IsNullable: true);

    private static readonly SystemEntityFieldDefinition IsDeleted =
        new("IsDeleted", "是否删除", "bool");

    private static readonly SystemEntityFieldDefinition DeleteUserId =
        new("DeleteUserId", "删除人ID", "long?", IsNullable: true);

    private static readonly SystemEntityFieldDefinition DeleteBy =
        new("DeleteBy", "删除人", "string?", IsNullable: true, MaxLength: 64);

    private static readonly SystemEntityFieldDefinition DeleteTime =
        new("DeleteTime", "删除时间", "DateTime?", IsNullable: true);

    private static readonly IReadOnlyList<SystemEntityFieldDefinition> AllDefinitions =
    [
        Id,
        ParentId,
        Ancestors,
        TenantId,
        DeptId,
        DeptAncestors,
        CreateUserId,
        CreateBy,
        CreateTime,
        UpdateUserId,
        UpdateBy,
        UpdateTime,
        IsDeleted,
        DeleteUserId,
        DeleteBy,
        DeleteTime
    ];

    internal static IReadOnlyList<SystemEntityFieldDefinition> GetRequired(ModuleEntity entity)
    {
        var required = new Dictionary<string, SystemEntityFieldDefinition>(StringComparer.OrdinalIgnoreCase);

        void Add(params SystemEntityFieldDefinition[] definitions)
        {
            foreach (var definition in definitions)
                required[definition.Name] = definition;
        }

        if (entity.HasPrimaryKey) Add(Id);
        if (entity.IsTree) Add(ParentId, Ancestors);
        if (entity.HasTenant) Add(TenantId);
        if (entity.HasDataPermission) Add(DeptId, DeptAncestors, CreateUserId);
        if (entity.HasAudit) Add(CreateUserId, CreateBy, CreateTime, UpdateUserId, UpdateBy, UpdateTime);
        if (entity.HasSoftDelete) Add(IsDeleted, DeleteUserId, DeleteBy, DeleteTime);

        return AllDefinitions.Where(definition => required.ContainsKey(definition.Name)).ToList();
    }

    internal static IReadOnlySet<string> ManagedNames { get; } =
        AllDefinitions.Select(definition => definition.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

    internal static void Apply(EntityField field, SystemEntityFieldDefinition definition)
    {
        field.Name = definition.Name;
        field.Description = definition.Description;
        field.DataType = definition.DataType;
        field.IsNullable = definition.IsNullable;
        field.MaxLength = definition.MaxLength;
        field.Precision = null;
        field.Scale = null;
        field.DefaultValue = definition.DefaultValue;
        field.IsPrimaryKey = definition.IsPrimaryKey;
        field.IsRequired = definition.IsRequired;
        field.IsSystemField = true;
        field.IsIgnore = false;
        field.ShowInList = false;
        field.ShowInDetail = false;
        field.ShowInAddForm = definition.ShowInAddForm;
        field.ShowInEditForm = definition.ShowInEditForm;
        field.ShowInSearch = false;
        field.FormControlType = "input";
        field.FieldCategory = "Normal";
    }
}
