namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 字典类型DTO（树形结构，包含子节点）
/// </summary>
public class SysDictTypeDto
{
    public long Id { get; set; }
    public string DictName { get; set; } = string.Empty;
    public string DictType { get; set; } = string.Empty;
    public string? Label { get; set; }  // 统一字段：对于类型节点为空
    public string? Value { get; set; }  // 统一字段：对于类型节点为空
    public string? LangKey { get; set; }  // 统一字段：对于类型节点为空
    public int? IsDefault { get; set; }  // 统一字段：对于类型节点为空
    public int Status { get; set; }
    public int Sort { get; set; }
    public DateTime CreateTime { get; set; }
    public string? Remark { get; set; }
    public List<SysDictDataDto>? Children { get; set; }  // 子节点：字典数据列表
}

public class CreateSysDictTypeDto
{
    public string DictName { get; set; } = string.Empty;
    public string DictType { get; set; } = string.Empty;
    public string? LangKey { get; set; }
    public int Status { get; set; } = 0;
    public int Sort { get; set; } = 0;
    public string? Remark { get; set; }
}

public class UpdateSysDictTypeDto
{
    public string? DictName { get; set; }
    public string? DictType { get; set; }
    public string? LangKey { get; set; }
    public int? Status { get; set; }
    public int? Sort { get; set; }
    public string? Remark { get; set; }
}

public class SysDictTypeQueryDto
{
    public string? DictName { get; set; }
    public string? DictType { get; set; }
    public string? Label { get; set; }  // 新增：支持子表查询
    public string? Value { get; set; }  // 新增：支持子表查询
    public string? LangKey { get; set; }  // 新增：支持子表查询
    public int? Status { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// 字典数据DTO
/// </summary>
public class SysDictDataDto
{
    public long Id { get; set; }
    public string DictType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? LangKey { get; set; }
    public int IsDefault { get; set; }
    public int Status { get; set; }
    public int Sort { get; set; }
    public DateTime CreateTime { get; set; }
    public string? Remark { get; set; }
}

public class CreateSysDictDataDto
{
    public string DictType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? LangKey { get; set; }
    public int IsDefault { get; set; } = 0;
    public int Status { get; set; } = 0;
    public int Sort { get; set; } = 0;
    public string? Remark { get; set; }
}

public class UpdateSysDictDataDto
{
    public string? DictType { get; set; }
    public string? Label { get; set; }
    public string? Value { get; set; }
    public string? LangKey { get; set; }
    public int? IsDefault { get; set; }
    public int? Status { get; set; }
    public int? Sort { get; set; }
    public string? Remark { get; set; }
}

public class SysDictDataQueryDto
{
    public string? DictType { get; set; }
    public string? Label { get; set; }
    public int? Status { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
