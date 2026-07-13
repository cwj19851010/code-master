using CodeMaster.Core.Configuration;
using CodeMaster.Core.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace CodeMaster.Infrastructure.Caching.Providers;

/// <summary>
/// 两级缓存提供者（内存 + Redis）
/// </summary>
public class HybridCacheProvider : ICacheProvider
{
    private readonly IMemoryCache? _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly CacheOptions _options;

    public HybridCacheProvider(
        CacheOptions options,
        IMemoryCache? memoryCache = null,
        IDistributedCache? distributedCache = null,
        IConnectionMultiplexer? connectionMultiplexer = null)
    {
        _options = options;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<string?> GetStringAsync(string key)
    {
        // 先查一级缓存（内存）
        if (_options.Hybrid.EnableL1 && _memoryCache != null)
        {
            if (_memoryCache.TryGetValue<string>(key, out var memoryValue))
            {
                return memoryValue;
            }
        }

        // 再查二级缓存（Redis）
        if (_options.Hybrid.EnableL2 && _distributedCache != null)
        {
            var redisValue = await _distributedCache.GetStringAsync(key);
            if (redisValue != null)
            {
                // 回填到一级缓存
                if (_options.Hybrid.EnableL1 && _memoryCache != null)
                {
                    var memoryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _options.Hybrid.L1Expiration
                    };
                    _memoryCache.Set(key, redisValue, memoryOptions);
                }
                return redisValue;
            }
        }

        return null;
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        // 写入一级缓存（内存）
        if (_options.Hybrid.EnableL1 && _memoryCache != null)
        {
            var memoryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _options.Hybrid.L1Expiration
            };
            _memoryCache.Set(key, value, memoryOptions);
        }

        // 写入二级缓存（Redis）
        if (_options.Hybrid.EnableL2 && _distributedCache != null)
        {
            var redisOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _options.Hybrid.L2Expiration
            };
            await _distributedCache.SetStringAsync(key, value, redisOptions);
        }
    }

    public async Task RemoveAsync(string key)
    {
        // 删除一级缓存
        if (_options.Hybrid.EnableL1 && _memoryCache != null)
        {
            _memoryCache.Remove(key);
        }

        // 删除二级缓存
        if (_options.Hybrid.EnableL2 && _distributedCache != null)
        {
            await _distributedCache.RemoveAsync(key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        // 先检查一级缓存
        if (_options.Hybrid.EnableL1 && _memoryCache != null)
        {
            if (_memoryCache.TryGetValue(key, out _))
            {
                return true;
            }
        }

        // 再检查二级缓存
        if (_options.Hybrid.EnableL2 && _distributedCache != null)
        {
            var value = await _distributedCache.GetStringAsync(key);
            return value != null;
        }

        return false;
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        // 内存缓存不支持模式删除，只删除 Redis
        if (_options.Hybrid.EnableL2 && _connectionMultiplexer != null)
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length > 0)
            {
                var db = _connectionMultiplexer.GetDatabase();
                await db.KeyDeleteAsync(keys);
            }
        }
    }
}
