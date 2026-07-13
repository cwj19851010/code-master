using System.Security.Claims;
using CodeMaster.Core.Data;
using CodeMaster.Core.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CodeMaster.Infrastructure.Middleware;

/// <summary>
/// 数据权限上下文中间件
/// </summary>
public class DataPermissionMiddleware
{
    private readonly RequestDelegate _next;

    public DataPermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDataPermissionContext dataPermissionContext)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            dataPermissionContext.IsEnabled = true;
            dataPermissionContext.UserId = TryGetLongClaim(context.User, ClaimTypes.NameIdentifier);
            dataPermissionContext.DeptId = TryGetLongClaim(context.User, "DeptId");
            dataPermissionContext.DataScope = TryGetIntClaim(context.User, "PostDataScope")
                ?? TryGetIntClaim(context.User, "DataScope")
                ?? (int)PostDataScope.Self;
            dataPermissionContext.IsAdmin = IsAdmin(context.User);
        }

        await _next(context);
    }

    private static long? TryGetLongClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirst(claimType)?.Value;
        return long.TryParse(value, out var result) ? result : null;
    }

    private static int? TryGetIntClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirst(claimType)?.Value;
        return int.TryParse(value, out var result) ? result : null;
    }

    private static bool IsAdmin(ClaimsPrincipal user)
    {
        var isAdminClaim = user.FindFirst("IsAdmin")?.Value;
        if (bool.TryParse(isAdminClaim, out var isAdmin) && isAdmin)
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// 数据权限中间件扩展
/// </summary>
public static class DataPermissionMiddlewareExtensions
{
    public static IApplicationBuilder UseDataPermission(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DataPermissionMiddleware>();
    }
}
