namespace CodeMaster.Core.Entities;

/// <summary>
/// 租户接口
/// </summary>
public interface ITenant
{
    /// <summary>
    /// 租户ID
    /// 0 表示宿主（Host）
    /// 其他值表示具体租户
    /// </summary>
    long TenantId { get; set; }
}
