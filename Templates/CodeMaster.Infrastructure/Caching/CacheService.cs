using System.Text.Json;
using CodeMaster.Core.Configuration;
using CodeMaster.Core.Services;

namespace CodeMaster.Infrastructure.Caching;

/// <summary>
/// 缓存服务实现（统一业务层接口）
/// </summary>
public class CacheService : ICacheService
{
    private readonly ICacheProvider _provider;
    private readonly CacheOptions _options;

    public CacheService(ICacheProvider provider, CacheOptions options)
    {
        _provider = provider;
        _options = options;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _provider.GetStringAsync(key);
        if (value == null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var json = JsonSerializer.Serialize(value);
        var finalExpiration = expiration ?? GetExpirationByKey(key);
        await _provider.SetStringAsync(key, json, finalExpiration);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
    {
        // 自动匹配策略
        var expiration = GetExpirationByKey(key);
        return await GetOrCreateInternalAsync(key, factory, expiration);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, string policyName)
    {
        // 显式指定策略
        var expiration = _options.Policies.TryGetValue(policyName, out var policy)
            ? policy.Expiration
            : _options.DefaultExpiration;
        return await GetOrCreateInternalAsync(key, factory, expiration);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        // 完全自定义过期时间
        return await GetOrCreateInternalAsync(key, factory, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        await _provider.RemoveAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _provider.ExistsAsync(key);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        await _provider.RemoveByPatternAsync(pattern);
    }

    /// <summary>
    /// 内部实现：获取或创建缓存
    /// </summary>
    private async Task<T> GetOrCreateInternalAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
    {
        // 先尝试获取
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        // 缓存未命中，执行工厂方法
        var value = await factory();

        // 存入缓存
        await SetAsync(key, value, expiration);

        return value;
    }

    /// <summary>
    /// 根据键前缀自动匹配策略
    /// </summary>
    private TimeSpan GetExpirationByKey(string key)
    {
        foreach (var (name, policy) in _options.Policies)
        {
            var prefix = name.ToLower() + ":";
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return policy.Expiration;
            }
        }
        return _options.DefaultExpiration;
    }
}
