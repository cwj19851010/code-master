using System.Collections.Concurrent;
using System.Text.Json;
using CodeMaster.Core.Services;
using Microsoft.Extensions.Caching.Memory;

namespace CodeMaster.Infrastructure.Caching.Providers;

/// <summary>
/// 内存缓存提供者
/// </summary>
public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public MemoryCacheProvider(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<string?> GetStringAsync(string key)
    {
        var value = _memoryCache.Get<string>(key);
        return Task.FromResult(value);
    }

    public Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }

        // 注册回调，在缓存项被移除时从键列表中删除
        options.RegisterPostEvictionCallback((k, v, r, s) =>
        {
            _keys.TryRemove(k.ToString()!, out _);
        });

        _memoryCache.Set(key, value, options);
        _keys.TryAdd(key, 0);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        _keys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        var exists = _memoryCache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        // 将通配符模式转换为正则表达式
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        var regex = new System.Text.RegularExpressions.Regex(regexPattern);

        // 查找匹配的键
        var keysToRemove = _keys.Keys.Where(k => regex.IsMatch(k)).ToList();

        // 删除匹配的缓存项
        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
