using CodeMaster.Core.Entities;
using Microsoft.AspNetCore.Http;
using SqlSugar;

namespace CodeMaster.Core.Filters;

/// <summary>
/// 租户过滤器
/// </summary>
public class TenantFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 配置租户过滤器
    /// </summary>
    public void ConfigureTenantFilter(ISqlSugarClient db)
    {
        // 获取当前租户ID
        var tenantId = GetCurrentTenantId();

        // 为所有实现 ITenant 接口的实体添加租户过滤
        db.QueryFilter.AddTableFilter<Entities.ITenant>(tenant =>
            tenantId == 0
                ? tenant.TenantId == 0  // 宿主：只查询 TenantId = 0 的数据
                : tenant.TenantId == tenantId);  // 租户：只查询自己的数据
    }

    /// <summary>
    /// 获取当前租户ID
    /// </summary>
    private long GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return 0; // 默认为宿主
        }

        // 从 JWT Claims 中获取租户ID
        var tenantIdClaim = httpContext.User.FindFirst("TenantId");
        if (tenantIdClaim != null && long.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }

        // 从 Header 中获取租户ID（备用方案）
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            if (long.TryParse(tenantIdHeader, out var headerTenantId))
            {
                return headerTenantId;
            }
        }

        return 0; // 默认为宿主
    }

    /// <summary>
    /// 设置实体的租户ID
    /// </summary>
    public void SetTenantId<T>(T entity) where T : Entities.ITenant
    {
        if (entity.TenantId == 0)
        {
            entity.TenantId = GetCurrentTenantId();
        }
    }
}
