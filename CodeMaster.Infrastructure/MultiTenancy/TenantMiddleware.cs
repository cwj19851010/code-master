using CodeMaster.Core.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace CodeMaster.Infrastructure.MultiTenancy;

/// <summary>
/// Tenant middleware.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (IsAnonymousTenantEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("TenantId");
            if (tenantIdClaim != null && long.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                tenantContext.SetTenantId(tenantId);
            }
        }
        else if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValue)
            && long.TryParse(tenantIdValue, out var tenantId))
        {
            tenantContext.SetTenantId(tenantId);
        }

        await _next(context);
    }

    private static bool IsAnonymousTenantEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/account", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase);
    }
}
