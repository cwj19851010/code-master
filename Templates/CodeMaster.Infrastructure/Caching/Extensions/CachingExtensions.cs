using CodeMaster.Core.Configuration;
using CodeMaster.Core.Services;
using CodeMaster.Infrastructure.Caching.Providers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CodeMaster.Infrastructure.Caching.Extensions;

/// <summary>
/// 缓存服务注册扩展
/// </summary>
public static class CachingExtensions
{
    /// <summary>
    /// 添加缓存服务
    /// </summary>
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // 读取配置
        var cacheSection = configuration.GetSection("Cache");
        var provider = cacheSection["Provider"] ?? "Memory";

        var cacheOptions = new CacheOptions
        {
            Provider = provider,
            DefaultExpiration = TimeSpan.Parse(cacheSection["DefaultExpiration"] ?? "01:00:00")
        };

        services.AddSingleton(cacheOptions);

        // 根据配置选择提供者
        switch (provider.ToLower())
        {
            case "memory":
                var memoryOptions = new Core.Configuration.MemoryCacheOptions();
                var memorySection = cacheSection.GetSection("Memory");
                if (int.TryParse(memorySection["SizeLimit"], out var sizeLimit))
                    memoryOptions.SizeLimit = sizeLimit;
                if (double.TryParse(memorySection["CompactionPercentage"], out var compaction))
                    memoryOptions.CompactionPercentage = compaction;
                if (TimeSpan.TryParse(memorySection["ExpirationScanFrequency"], out var scanFreq))
                    memoryOptions.ExpirationScanFrequency = scanFreq;
                services.AddMemoryCacheProvider(memoryOptions);
                break;

            case "redis":
                var redisOptions = new RedisCacheOptions
                {
                    ConnectionString = cacheSection.GetSection("Redis")["ConnectionString"] ?? "localhost:6379",
                    InstanceName = cacheSection.GetSection("Redis")["InstanceName"] ?? "CodeMaster:"
                };
                services.AddRedisCacheProvider(redisOptions);
                break;

            case "hybrid":
                services.AddHybridCacheProvider(cacheOptions);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown cache provider: {cacheOptions.Provider}. " +
                    "Valid values are: Memory, Redis, Hybrid");
        }

        // 注册缓存服务
        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }

    /// <summary>
    /// 添加内存缓存提供者
    /// </summary>
    private static void AddMemoryCacheProvider(this IServiceCollection services, Core.Configuration.MemoryCacheOptions options)
    {
        services.AddMemoryCache(config =>
        {
            if (options.SizeLimit.HasValue)
                config.SizeLimit = options.SizeLimit.Value;
            config.CompactionPercentage = options.CompactionPercentage;
            config.ExpirationScanFrequency = options.ExpirationScanFrequency;
        });
        services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
    }

    /// <summary>
    /// 添加 Redis 缓存提供者
    /// </summary>
    private static void AddRedisCacheProvider(this IServiceCollection services, RedisCacheOptions options)
    {
        // 注册 Redis 分布式缓存
        services.AddStackExchangeRedisCache(config =>
        {
            config.Configuration = options.ConnectionString;
            config.InstanceName = options.InstanceName;
        });

        // 注册 ConnectionMultiplexer（用于模式删除）
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.ConnectTimeout = options.ConnectTimeout;
            configOptions.SyncTimeout = options.SyncTimeout;
            configOptions.AbortOnConnectFail = options.AbortOnConnectFail;
            return ConnectionMultiplexer.Connect(configOptions);
        });

        services.AddSingleton<ICacheProvider, RedisCacheProvider>();
    }

    /// <summary>
    /// 添加两级缓存提供者
    /// </summary>
    private static void AddHybridCacheProvider(this IServiceCollection services, CacheOptions options)
    {
        if (options.Hybrid.EnableL1)
        {
            services.AddMemoryCache(config =>
            {
                if (options.Memory.SizeLimit.HasValue)
                    config.SizeLimit = options.Memory.SizeLimit.Value;
                config.CompactionPercentage = options.Memory.CompactionPercentage;
                config.ExpirationScanFrequency = options.Memory.ExpirationScanFrequency;
            });
        }

        if (options.Hybrid.EnableL2)
        {
            services.AddStackExchangeRedisCache(config =>
            {
                config.Configuration = options.Redis.ConnectionString;
                config.InstanceName = options.Redis.InstanceName;
            });

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configOptions = ConfigurationOptions.Parse(options.Redis.ConnectionString);
                configOptions.ConnectTimeout = options.Redis.ConnectTimeout;
                configOptions.SyncTimeout = options.Redis.SyncTimeout;
                configOptions.AbortOnConnectFail = options.Redis.AbortOnConnectFail;
                return ConnectionMultiplexer.Connect(configOptions);
            });
        }

        services.AddSingleton<ICacheProvider, HybridCacheProvider>();
    }
}
