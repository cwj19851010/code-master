using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 部门DTO
/// </summary>
public class SysDeptDto : EntityDto
{
    public long? ParentId { get; set; }
    public string? Ancestors { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<SysDeptDto>? Children { get; set; }
}

/// <summary>
/// 创建部门DTO
/// </summary>
public class CreateSysDeptDto
{
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 更新部门DTO
/// </summary>
public class UpdateSysDeptDto
{
    public long? NewParentId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 部门查询DTO
/// </summary>
public class SysDeptQueryDto : PagedQueryDto
{
    public string? Name { get; set; }
}
