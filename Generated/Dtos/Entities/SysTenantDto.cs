using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.Entities;

/// <summary>
/// SysTenant查询DTO
/// </summary>
public class SysTenantQueryDto : PagedQueryDto
{
    /// <summary>
    /// TenantCode
    /// </summary>
    public string? TenantCode { get; set; }

    /// <summary>
    /// TenantName
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// IsolationType
    /// </summary>
    public int IsolationType { get; set; }

    /// <summary>
    /// ConfigId
    /// </summary>
    public string? ConfigId { get; set; }

    /// <summary>
    /// ConnectionString
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// DbType
    /// </summary>
    public Int32? DbType { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// ExpireTime
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// SysTenantDTO
/// </summary>
public class SysTenantDto : DtoBase
{
    /// <summary>
    /// TenantCode
    /// </summary>
    public string? TenantCode { get; set; }

    /// <summary>
    /// TenantName
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// IsolationType
    /// </summary>
    public int IsolationType { get; set; }

    /// <summary>
    /// ConfigId
    /// </summary>
    public string? ConfigId { get; set; }

    /// <summary>
    /// ConnectionString
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// DbType
    /// </summary>
    public Int32? DbType { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// ExpireTime
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// Id
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// CreateBy
    /// </summary>
    public string? CreateBy { get; set; }

    /// <summary>
    /// CreateTime
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// UpdateBy
    /// </summary>
    public string? UpdateBy { get; set; }

    /// <summary>
    /// UpdateTime
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// 创建SysTenantDTO
/// </summary>
public class CreateSysTenantDto : CreateDtoBase
{
    /// <summary>
    /// TenantCode
    /// </summary>
    public string? TenantCode { get; set; }

    /// <summary>
    /// TenantName
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// IsolationType
    /// </summary>
    public int IsolationType { get; set; }

    /// <summary>
    /// ConfigId
    /// </summary>
    public string? ConfigId { get; set; }

    /// <summary>
    /// ConnectionString
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// DbType
    /// </summary>
    public Int32? DbType { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// ExpireTime
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// 更新SysTenantDTO
/// </summary>
public class UpdateSysTenantDto : UpdateDtoBase
{
    /// <summary>
    /// TenantCode
    /// </summary>
    public string? TenantCode { get; set; }

    /// <summary>
    /// TenantName
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// IsolationType
    /// </summary>
    public int IsolationType { get; set; }

    /// <summary>
    /// ConfigId
    /// </summary>
    public string? ConfigId { get; set; }

    /// <summary>
    /// ConnectionString
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// DbType
    /// </summary>
    public Int32? DbType { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// ExpireTime
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}
