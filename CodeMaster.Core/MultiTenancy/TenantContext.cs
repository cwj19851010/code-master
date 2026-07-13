namespace CodeMaster.Core.MultiTenancy;

/// <summary>
/// 租户上下文实现
/// </summary>
public class TenantContext : ITenantContext
{
    private long? _currentTenantId;

    /// <summary>
    /// 当前租户ID
    /// </summary>
    public long? CurrentTenantId => _currentTenantId;

    /// <summary>
    /// 是否启用租户隔离
    /// </summary>
    public bool IsEnabled => _currentTenantId.HasValue;

    /// <summary>
    /// 设置当前租户ID
    /// </summary>
    public void SetTenantId(long? tenantId)
    {
        _currentTenantId = tenantId;
    }
}
