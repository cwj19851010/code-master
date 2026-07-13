using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 职位DTO
/// </summary>
public class SysPostDto
{
    public long Id { get; set; }
    public string PostName { get; set; } = string.Empty;
    public int DataScope { get; set; } = 1;
    public DateTime CreateTime { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 创建职位DTO
/// </summary>
public class CreateSysPostDto
{
    public string PostName { get; set; } = string.Empty;
    public int DataScope { get; set; } = 1;
    public string? Remark { get; set; }
}

/// <summary>
/// 更新职位DTO
/// </summary>
public class UpdateSysPostDto
{
    public string? PostName { get; set; }
    public int? DataScope { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 职位查询DTO
/// </summary>
public class SysPostQueryDto : PagedQueryDto
{
    public string? PostName { get; set; }
}
