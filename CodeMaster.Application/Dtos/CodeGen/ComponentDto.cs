namespace CodeMaster.Application.Dtos.CodeGen;

// ================================================================
// ComponentGroup DTOs
// ================================================================

public class SysComponentGroupDto
{
    public long Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public int Status { get; set; }
    public DateTime CreateTime { get; set; }
    public string? Remark { get; set; }
    public int ComponentCount { get; set; }
}

public class CreateSysComponentGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
}

public class UpdateSysComponentGroupDto
{
    public string? GroupName { get; set; }
    public string? GroupCode { get; set; }
    public string? Icon { get; set; }
    public int? Sort { get; set; }
    public int? Status { get; set; }
    public string? Remark { get; set; }
}

public class SysComponentGroupQueryDto
{
    public string? GroupName { get; set; }
    public string? GroupCode { get; set; }
    public int? Status { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

// ================================================================
// Component DTOs
// ================================================================

public class SysComponentDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public long GroupId { get; set; }
    public string? GroupName { get; set; }
    public int Sort { get; set; }
    public int Status { get; set; }
    public DateTime CreateTime { get; set; }
    public string? Remark { get; set; }
    public int PropertyCount { get; set; }
    public int SlotCount { get; set; }
    public int EventCount { get; set; }
    public int ExposeCount { get; set; }
}

public class SysComponentDetailDto : SysComponentDto
{
    public List<SysComponentPropertyDto> Properties { get; set; } = new();
    public List<SysComponentSlotDto> Slots { get; set; } = new();
    public List<SysComponentEventDto> Events { get; set; } = new();
    public List<SysComponentExposeDto> Exposes { get; set; } = new();
}

public class CreateSysComponentDto
{
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public long GroupId { get; set; }
    public int Sort { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
}

public class UpdateSysComponentDto
{
    public string? Name { get; set; }
    public string? Tag { get; set; }
    public string? Link { get; set; }
    public long? GroupId { get; set; }
    public int? Sort { get; set; }
    public int? Status { get; set; }
    public string? Remark { get; set; }
}

public class SysComponentQueryDto
{
    public string? Name { get; set; }
    public string? Tag { get; set; }
    public long? GroupId { get; set; }
    public int? Status { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ================================================================
// ComponentProperty DTOs
// ================================================================

public class SysComponentPropertyDto
{
    public long Id { get; set; }
    public long ComponentId { get; set; }
    public string? ComponentName { get; set; }
    public string PropName { get; set; } = string.Empty;
    public string? PropType { get; set; }
    public string? TypeDescription { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public bool IsCommon { get; set; }
    public string? EnumValues { get; set; }
    public bool IsAdvanced { get; set; }
    public int Sort { get; set; }
    public DateTime CreateTime { get; set; }
}

public class CreateSysComponentPropertyDto
{
    public long ComponentId { get; set; }
    public string PropName { get; set; } = string.Empty;
    public string? PropType { get; set; }
    public string? TypeDescription { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public bool IsCommon { get; set; }
    public string? EnumValues { get; set; }
    public bool IsAdvanced { get; set; }
    public int Sort { get; set; }
}

public class UpdateSysComponentPropertyDto
{
    public string? PropName { get; set; }
    public string? PropType { get; set; }
    public string? TypeDescription { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public bool? IsCommon { get; set; }
    public string? EnumValues { get; set; }
    public bool? IsAdvanced { get; set; }
    public int? Sort { get; set; }
}

public class SysComponentPropertyQueryDto
{
    public long? ComponentId { get; set; }
    public string? PropName { get; set; }
    public bool? IsCommon { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

// ================================================================
// ComponentSlot DTOs
// ================================================================

public class SysComponentSlotDto
{
    public long Id { get; set; }
    public long ComponentId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SlotType { get; set; }
    public string? TypeDescription { get; set; }
    public bool IsCommon { get; set; }
    public int Sort { get; set; }
}

public class CreateSysComponentSlotDto
{
    public long ComponentId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SlotType { get; set; }
    public string? TypeDescription { get; set; }
    public bool IsCommon { get; set; }
    public int Sort { get; set; }
}

public class UpdateSysComponentSlotDto
{
    public string? SlotName { get; set; }
    public string? Description { get; set; }
    public string? SlotType { get; set; }
    public string? TypeDescription { get; set; }
    public bool? IsCommon { get; set; }
    public int? Sort { get; set; }
}

// ================================================================
// ComponentEvent DTOs
// ================================================================

public class SysComponentEventDto
{
    public long Id { get; set; }
    public long ComponentId { get; set; }
    public string? ComponentName { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EventType { get; set; }
    public string? TypeDescription { get; set; }
    public bool IsCommon { get; set; }
    public bool IsSingle { get; set; }
    public int Sort { get; set; }
    public DateTime CreateTime { get; set; }
}

public class CreateSysComponentEventDto
{
    public long ComponentId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EventType { get; set; }
    public string? TypeDescription { get; set; }
    public bool IsCommon { get; set; }
    public bool IsSingle { get; set; }
    public int Sort { get; set; }
}

public class UpdateSysComponentEventDto
{
    public string? EventName { get; set; }
    public string? Description { get; set; }
    public string? EventType { get; set; }
    public string? TypeDescription { get; set; }
    public bool? IsCommon { get; set; }
    public bool? IsSingle { get; set; }
    public int? Sort { get; set; }
}

public class SysComponentEventQueryDto
{
    public long? ComponentId { get; set; }
    public string? EventName { get; set; }
    public bool? IsCommon { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}

// ================================================================
// ComponentExpose DTOs
// ================================================================

public class SysComponentExposeDto
{
    public long Id { get; set; }
    public long ComponentId { get; set; }
    public string? ComponentName { get; set; }
    public string ExposeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExposeType { get; set; }
    public string? TypeDescription { get; set; }
    public bool IsCommon { get; set; }
    public int Sort { get; set; }
}

public class CreateSysComponentExposeDto
{
    public long ComponentId { get; set; }
    public string ExposeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExposeType { get; set; }
    public string? TypeDescription { get; set; }
    public bool IsCommon { get; set; }
    public int Sort { get; set; }
}

public class UpdateSysComponentExposeDto
{
    public string? ExposeName { get; set; }
    public string? Description { get; set; }
    public string? ExposeType { get; set; }
    public string? TypeDescription { get; set; }
    public bool? IsCommon { get; set; }
    public int? Sort { get; set; }
}

// ================================================================
// Bulk DTOs
// ================================================================

public class SetCommonDto
{
    public long Id { get; set; }
    public bool IsCommon { get; set; }
}

public class ImportComponentDto
{
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public List<CreateSysComponentPropertyDto> Properties { get; set; } = new();
    public List<CreateSysComponentSlotDto> Slots { get; set; } = new();
    public List<CreateSysComponentEventDto> Events { get; set; } = new();
    public List<CreateSysComponentExposeDto> Exposes { get; set; } = new();
}

// ================================================================
// SysDirective DTOs（Vue 全局指令，不绑定具体组件）
// ================================================================

public class SysDirectiveDto
{
    public long Id { get; set; }
    public string DirectiveName { get; set; } = string.Empty;
    public bool HasValue { get; set; }
    public string? ValueType { get; set; }
    public string? Description { get; set; }
    public bool IsCommon { get; set; }
    public int Sort { get; set; }
    public DateTime CreateTime { get; set; }
}

public class CreateSysDirectiveDto
{
    public string DirectiveName { get; set; } = string.Empty;
    public bool HasValue { get; set; } = true;
    public string? ValueType { get; set; }
    public string? Description { get; set; }
    public bool IsCommon { get; set; }
    public int Sort { get; set; }
}

public class UpdateSysDirectiveDto
{
    public string? DirectiveName { get; set; }
    public bool? HasValue { get; set; }
    public string? ValueType { get; set; }
    public string? Description { get; set; }
    public bool? IsCommon { get; set; }
    public int? Sort { get; set; }
}
