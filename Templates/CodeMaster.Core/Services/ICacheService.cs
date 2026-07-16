namespace CodeMaster.Core.Services;

/// <summary>
/// 缓存服务接口（业务层使用）
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 获取缓存值（自动匹配策略）
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// 设置缓存值（自动匹配策略）
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// 获取或创建缓存（自动匹配策略）
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory);

    /// <summary>
    /// 获取或创建缓存（显式指定策略）
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, string policyName);

    /// <summary>
    /// 获取或创建缓存（完全自定义过期时间）
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);

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
