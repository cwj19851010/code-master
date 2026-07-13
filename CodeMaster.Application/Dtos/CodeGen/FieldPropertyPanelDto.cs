namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 字段属性面板数据（设计器右侧面板用）
/// 合并了模板解析结果 + SysComponent/SysDirective 元数据
/// </summary>
public class FieldPropertyPanelDto
{
    public string FieldName { get; set; } = string.Empty;
    public string? GenId { get; set; }
    public string ComponentTag { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string FormControlType { get; set; } = string.Empty;

    public List<PropertyItemDto> Properties { get; set; } = new();
    public List<DirectiveItemDto> Directives { get; set; } = new();
    public List<EventItemDto> Events { get; set; } = new();
    public List<SlotItemDto> Slots { get; set; } = new();
}

public class PropertyItemDto
{
    public string PropName { get; set; } = string.Empty;
    public string? PropType { get; set; }
    public string? EnumValues { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public bool IsCommon { get; set; }
    public bool IsAdvanced { get; set; }

    /// <summary>模板中已存在此属性（勾选状态）</summary>
    public bool IsActive { get; set; }

    /// <summary>当前值（模板中解析出的值）</summary>
    public string? CurrentValue { get; set; }

    /// <summary>值类型：static | bind | boolean</summary>
    public string ValueType { get; set; } = "static";
}

public class EventItemDto
{
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCommon { get; set; }
    public bool IsSingle { get; set; }

    public bool IsActive { get; set; }
    public string? CurrentValue { get; set; }
    public bool IsSingleActive { get; set; }
}

public class SlotItemDto
{
    public string SlotName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCommon { get; set; }

    public bool IsActive { get; set; }
}

public class DirectiveItemDto
{
    public string DirectiveName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool HasValue { get; set; }
    public bool IsCommon { get; set; }

    public bool IsActive { get; set; }
    public string? CurrentValue { get; set; }
}
