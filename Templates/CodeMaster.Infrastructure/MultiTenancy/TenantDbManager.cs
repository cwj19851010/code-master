using CodeMaster.Core.Entities;
using CodeMaster.Domain.Entities.System;
using Microsoft.AspNetCore.Http;
using SqlSugar;
using System.Collections.Concurrent;

namespace CodeMaster.Infrastructure.MultiTenancy;

/// <summary>
/// 租户数据库管理器
/// 支持物理隔离（独立数据库）和逻辑隔离（共享数据库）
/// </summary>
public class TenantDbManager
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISqlSugarClient _defaultDb;
    private static readonly ConcurrentDictionary<long, ISqlSugarClient> _tenantDbCache = new();

    public TenantDbManager(IHttpContextAccessor httpContextAccessor, ISqlSugarClient defaultDb)
    {
        _httpContextAccessor = httpContextAccessor;
        _defaultDb = defaultDb;
    }

    /// <summary>
    /// 获取当前租户的数据库连接
    /// </summary>
    public async Task<ISqlSugarClient> GetTenantDbAsync()
    {
        var tenantId = GetCurrentTenantId();

        // 宿主使用默认数据库
        if (tenantId == 0)
        {
            return _defaultDb;
        }

        // 检查缓存
        if (_tenantDbCache.TryGetValue(tenantId, out var cachedDb))
        {
            return cachedDb;
        }

        // 查询租户配置
        var tenant = await _defaultDb.Queryable<SysTenant>()
            .Where(t => t.Id == tenantId)
            .FirstAsync();

        if (tenant == null)
        {
            throw new Exception($"租户 {tenantId} 不存在");
        }

        // 逻辑隔离：使用默认数据库
        if (tenant.IsolationType == 1)
        {
            return _defaultDb;
        }

        // 物理隔��：创建独立数据库连接
        if (tenant.IsolationType == 0 && !string.IsNullOrEmpty(tenant.ConnectionString))
        {
            var tenantDb = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = tenant.ConnectionString,
                DbType = (DbType)tenant.DbType,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            // 配置软删除过滤器
            tenantDb.QueryFilter.AddTableFilter<ISoftDelete>(entity => entity.IsDeleted == false);

            // 缓存连接
            _tenantDbCache.TryAdd(tenantId, tenantDb);

            return tenantDb;
        }

        // 默认返回主数据库
        return _defaultDb;
    }

    /// <summary>
    /// 获取当前租户ID
    /// </summary>
    private long GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return 0;
        }

        var tenantIdClaim = httpContext.User.FindFirst("TenantId");
        if (tenantIdClaim != null && long.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }

        return 0;
    }

    /// <summary>
    /// 清除租户数据库缓存
    /// </summary>
    public static void ClearCache(long tenantId)
    {
        _tenantDbCache.TryRemove(tenantId, out _);
    }

    /// <summary>
    /// 初始化租户数据库（物理隔离时使用）
    /// </summary>
    public async Task InitializeTenantDatabaseAsync(long tenantId, string connectionString, DbType dbType)
    {
        var tenantDb = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = dbType,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });

        try
        {
            // 创建所有表结构
            var types = new[]
            {
                typeof(SysUser),
                typeof(SysRole),
                typeof(SysDept),
                typeof(SysMenu),
                typeof(SysUserRole),
                typeof(SysRoleMenu)
            };

            tenantDb.CodeFirst.InitTables(types);

            // 插入基础数据（可选）
            // 例如：默认部门、默认菜单等

            Console.WriteLine($"租户 {tenantId} 的数据库初始化成功");
        }
        catch (Exception ex)
        {
            throw new Exception($"初始化租户数据库失败: {ex.Message}", ex);
        }
    }
}
