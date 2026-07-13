namespace CodeMaster.Core.Services;

/// <summary>
/// 缓存提供者接口（底层实现）
/// </summary>
public interface ICacheProvider
{
    /// <summary>
    /// 获取字符串值
    /// </summary>
    Task<string?> GetStringAsync(string key);

    /// <summary>
    /// 设置字符串值
    /// </summary>
    Task SetStringAsync(string key, string value, TimeSpan? expiration = null);

    /// <summary>
    /// 移除缓存项
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// 检查键是否存在
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 按模式删除（如 "user:*"）
    /// </summary>
    Task RemoveByPatternAsync(string pattern);
}
