using CodeMaster.Core.Services;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace CodeMaster.Infrastructure.Caching.Providers;

/// <summary>
/// Redis 缓存提供者
/// </summary>
public class RedisCacheProvider : ICacheProvider
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer? _connectionMultiplexer;

    public RedisCacheProvider(IDistributedCache distributedCache, IConnectionMultiplexer? connectionMultiplexer = null)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<string?> GetStringAsync(string key)
    {
        return await _distributedCache.GetStringAsync(key);
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        var options = new DistributedCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }

        await _distributedCache.SetStringAsync(key, value, options);
    }

    public async Task RemoveAsync(string key)
    {
        await _distributedCache.RemoveAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var value = await _distributedCache.GetStringAsync(key);
        return value != null;
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        if (_connectionMultiplexer == null)
        {
            throw new InvalidOperationException("ConnectionMultiplexer is required for pattern-based removal.");
        }

        var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern).ToArray();

        if (keys.Length > 0)
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(keys);
        }
    }
}
