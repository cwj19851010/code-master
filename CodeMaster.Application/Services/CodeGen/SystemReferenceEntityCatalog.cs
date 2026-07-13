using System.Text.Json;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Application.Services.CodeGen;

public static class SystemReferenceEntityCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly IReadOnlyList<SystemReferenceEntityDefinition> Items =
    [
        new(
            Name: "SysUser",
            Description: "用户",
            ApiModuleName: "system",
            ApiEntityName: "user",
            IsTree: false,
            ValueField: "Id",
            DisplayFields: ["NickName", "UserName"],
            Fields:
            [
                Field("Id", "ID", "long", isPrimaryKey: true),
                Field("UserName", "用户名"),
                Field("NickName", "昵称"),
                Field("DeptId", "部门ID", "long", isNullable: true),
                Field("DeptName", "部门名称"),
                Field("PostId", "岗位ID", "long", isNullable: true),
                Field("PostName", "岗位名称"),
                Field("PhoneNumber", "手机号"),
                Field("Email", "邮箱"),
                Field("Status", "状态", "int")
            ]),
        new(
            Name: "SysRole",
            Description: "角色",
            ApiModuleName: "system",
            ApiEntityName: "role",
            IsTree: false,
            ValueField: "Id",
            DisplayFields: ["RoleName", "RoleKey"],
            Fields:
            [
                Field("Id", "ID", "long", isPrimaryKey: true),
                Field("RoleName", "角色名称"),
                Field("RoleKey", "权限字符"),
                Field("RoleSort", "显示顺序", "int"),
                Field("DataScope", "数据范围", "int"),
                Field("Status", "状态", "int")
            ]),
        new(
            Name: "SysDept",
            Description: "部门",
            ApiModuleName: "system",
            ApiEntityName: "dept",
            IsTree: true,
            ValueField: "Id",
            DisplayFields: ["Name"],
            Fields:
            [
                Field("Id", "ID", "long", isPrimaryKey: true),
                Field("Name", "部门名称"),
                Field("ParentId", "父级ID", "long", isNullable: true),
                Field("Ancestors", "祖级列表")
            ]),
        new(
            Name: "SysTenant",
            Description: "租户",
            ApiModuleName: "system",
            ApiEntityName: "tenant",
            IsTree: false,
            ValueField: "Id",
            DisplayFields: ["TenantName", "TenantCode"],
            Fields:
            [
                Field("Id", "ID", "long", isPrimaryKey: true),
                Field("TenantName", "租户名称"),
                Field("TenantCode", "租户编码"),
                Field("IsolationType", "隔离类型", "int"),
                Field("Status", "状态", "int")
            ]),
        new(
            Name: "SysPost",
            Description: "岗位",
            ApiModuleName: "system",
            ApiEntityName: "post",
            IsTree: false,
            ValueField: "Id",
            DisplayFields: ["PostName"],
            Fields:
            [
                Field("Id", "ID", "long", isPrimaryKey: true),
                Field("PostName", "岗位名称"),
                Field("DataScope", "数据范围", "int")
            ])
    ];

    public static IReadOnlyList<SystemReferenceEntityDefinition> GetAll() => Items;

    public static bool TryGet(string? name, out SystemReferenceEntityDefinition definition)
    {
        definition = Items.FirstOrDefault(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.ApiEntityName, name, StringComparison.OrdinalIgnoreCase))!;

        return definition != null;
    }

    public static ReferenceEntityDto ToDto(SystemReferenceEntityDefinition definition)
    {
        return new ReferenceEntityDto
        {
            Name = definition.Name,
            Description = definition.Description,
            IsBuiltin = true,
            IsTree = definition.IsTree,
            ValueField = definition.ValueField,
            DisplayFields = definition.DisplayFields.ToList(),
            SelectOptions = definition.ToSelectOptionsJson(),
            Fields = definition.Fields.Select(ToFieldDto).ToList()
        };
    }

    public static EntityFieldDto ToFieldDto(SystemReferenceFieldDefinition field)
    {
        return new EntityFieldDto
        {
            Name = field.Name,
            Description = field.Description,
            DataType = field.DataType,
            IsNullable = field.IsNullable,
            IsPrimaryKey = field.IsPrimaryKey,
            IsSystemField = field.IsPrimaryKey,
            IsRequired = !field.IsNullable,
            ShowInList = !field.IsPrimaryKey,
            ShowInDetail = true,
            ShowInAddForm = false,
            ShowInEditForm = false,
            FormControlType = field.FormControlType,
            OrderNum = field.OrderNum
        };
    }

    public static EntityField ToEntityField(SystemReferenceFieldDefinition field)
    {
        return new EntityField
        {
            Name = field.Name,
            Description = field.Description,
            DataType = field.DataType,
            IsNullable = field.IsNullable,
            IsPrimaryKey = field.IsPrimaryKey,
            IsSystemField = field.IsPrimaryKey,
            IsRequired = !field.IsNullable,
            FormControlType = field.FormControlType,
            OrderNum = field.OrderNum
        };
    }

    private static SystemReferenceFieldDefinition Field(
        string name,
        string description,
        string dataType = "string",
        bool isNullable = false,
        bool isPrimaryKey = false,
        string formControlType = "input")
    {
        return new SystemReferenceFieldDefinition(
            Name: name,
            Description: description,
            DataType: dataType,
            IsNullable: isNullable,
            IsPrimaryKey: isPrimaryKey,
            FormControlType: formControlType,
            OrderNum: 0);
    }

    public static string ToSelectOptionsJson(this SystemReferenceEntityDefinition definition)
    {
        var contract = new
        {
            source = "builtin-system",
            entity = definition.Name,
            apiModule = definition.ApiModuleName,
            apiEntity = definition.ApiEntityName,
            listMethod = "getList",
            valueField = definition.ValueField,
            displayFields = definition.DisplayFields
        };

        return JsonSerializer.Serialize(contract, JsonOptions);
    }
}

public sealed record SystemReferenceEntityDefinition(
    string Name,
    string Description,
    string ApiModuleName,
    string ApiEntityName,
    bool IsTree,
    string ValueField,
    IReadOnlyList<string> DisplayFields,
    IReadOnlyList<SystemReferenceFieldDefinition> Fields);

public sealed record SystemReferenceFieldDefinition(
    string Name,
    string Description,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    string FormControlType,
    int OrderNum);
