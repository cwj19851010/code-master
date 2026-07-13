using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 租户实体
/// </summary>
public class SysTenant : EntityBaseWithTenant
{
    /// <summary>
    /// 租户编码
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 隔离类型（0物理隔离 1逻辑隔离）
    /// </summary>
    public int IsolationType { get; set; } = 1;

    /// <summary>
    /// 数据库配置ID（物理隔离使用）
    /// </summary>
    public string? ConfigId { get; set; }

    /// <summary>
    /// 连接字符串（物理隔离使用）
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 数据库类型（1SqlServer 2MySQL 3PostgreSQL）
    /// </summary>
    public int? DbType { get; set; }

    /// <summary>
    /// 状态（0正常 1停用）
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpireTime { get; set; }
}
