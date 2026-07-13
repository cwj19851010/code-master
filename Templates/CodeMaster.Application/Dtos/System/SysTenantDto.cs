namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 租户DTO
/// </summary>
public class SysTenantDto
{
    public long Id { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public int IsolationType { get; set; }
    public string? ConfigId { get; set; }
    public string? ConnectionString { get; set; }
    public int? DbType { get; set; }
    public int Status { get; set; }
    public DateTime? ExpireTime { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}

/// <summary>
/// 创建租户DTO
/// </summary>
public class CreateSysTenantDto
{
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public int IsolationType { get; set; }
    public string? ConfigId { get; set; }
    public string? ConnectionString { get; set; }
    public int? DbType { get; set; }
    public int Status { get; set; } = 0;
    public DateTime? ExpireTime { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 更新租户DTO
/// </summary>
public class UpdateSysTenantDto
{
    public long Id { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public int IsolationType { get; set; }
    public string? ConfigId { get; set; }
    public string? ConnectionString { get; set; }
    public int? DbType { get; set; }
    public int Status { get; set; }
    public DateTime? ExpireTime { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 租户查询DTO
/// </summary>
public class SysTenantQueryDto
{
    public string? TenantCode { get; set; }
    public string? TenantName { get; set; }
    public int? IsolationType { get; set; }
    public int? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
