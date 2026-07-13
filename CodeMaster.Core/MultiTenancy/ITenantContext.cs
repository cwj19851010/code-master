namespace CodeMaster.Core.MultiTenancy;

/// <summary>
/// 租户上下文接口
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// 当前租户ID
    /// </summary>
    long? CurrentTenantId { get; }

    /// <summary>
    /// 设置当前租户ID
    /// </summary>
    void SetTenantId(long? tenantId);

    /// <summary>
    /// 是否启用租户隔离
    /// </summary>
    bool IsEnabled { get; }
}
