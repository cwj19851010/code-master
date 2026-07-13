namespace CodeMaster.Core.Configuration;

/// <summary>
/// 缓存配置选项
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// 缓存提供者类型：Memory | Redis | Hybrid
    /// </summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>
    /// 默认过期时间
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 内存缓存配置
    /// </summary>
    public MemoryCacheOptions Memory { get; set; } = new();

    /// <summary>
    /// Redis 缓存配置
    /// </summary>
    public RedisCacheOptions Redis { get; set; } = new();

    /// <summary>
    /// 两级缓存配置
    /// </summary>
    public HybridCacheOptions Hybrid { get; set; } = new();

    /// <summary>
    /// 缓存策略配置
    /// </summary>
    public Dictionary<string, CachePolicyOptions> Policies { get; set; } = new();
}

/// <summary>
/// 内存缓存配置
/// </summary>
public class MemoryCacheOptions
{
    /// <summary>
    /// 缓存大小限制（MB）
    /// </summary>
    public long? SizeLimit { get; set; }

    /// <summary>
    /// 压缩百分比
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.25;

    /// <summary>
    /// 过期扫描频率
    /// </summary>
    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Redis 缓存配置
/// </summary>
public class RedisCacheOptions
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// 实例名称（键前缀）
    /// </summary>
    public string InstanceName { get; set; } = "CodeMaster:";

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// 同步超时时间（毫秒）
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    /// 连接失败时是否中止
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;
}

/// <summary>
/// 两级缓存配置
/// </summary>
public class HybridCacheOptions
{
    /// <summary>
    /// 一级缓存（内存）过期时间
    /// </summary>
    public TimeSpan L1Expiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 二级缓存（Redis）过期时间
    /// </summary>
    public TimeSpan L2Expiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 是否启用一级缓存
    /// </summary>
    public bool EnableL1 { get; set; } = true;

    /// <summary>
    /// 是否启用二级缓存
    /// </summary>
    public bool EnableL2 { get; set; } = true;
}

/// <summary>
/// 缓存策略配置
/// </summary>
public class CachePolicyOptions
{
    /// <summary>
    /// 过期时间
    /// </summary>
    public TimeSpan Expiration { get; set; }

    /// <summary>
    /// 是否使用滑动过期
    /// </summary>
    public bool SlidingExpiration { get; set; } = false;
}
