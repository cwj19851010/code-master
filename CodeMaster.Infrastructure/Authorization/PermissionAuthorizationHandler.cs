using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CodeMaster.Infrastructure.Authorization;

/// <summary>
/// 权限授权处理器
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (IsHostAdmin(context.User))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in context.User.FindAll("Permission"))
        {
            if (!string.IsNullOrWhiteSpace(claim.Value))
            {
                permissionSet.Add(claim.Value);
            }
        }

        var packed = context.User.FindFirst("Permissions")?.Value;
        if (!string.IsNullOrWhiteSpace(packed))
        {
            foreach (var p in packed.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                permissionSet.Add(p.Trim());
            }
        }

        // 支持多个权限（OR 逻辑）：任意一个权限满足即可
        foreach (var requiredPermission in requirement.Permissions)
        {
            if (permissionSet.Contains(requiredPermission))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsHostAdmin(ClaimsPrincipal user)
    {
        var isHostAdminClaim = user.FindFirst("IsHostAdmin")?.Value;
        if (bool.TryParse(isHostAdminClaim, out var isHostAdmin) && isHostAdmin)
        {
            return true;
        }

        var tenantIdClaim = user.FindFirst("TenantId")?.Value;
        var isHostTenant = long.TryParse(tenantIdClaim, out var tenantId) && tenantId == 0;
        var userName = user.Identity?.Name ?? user.FindFirst(ClaimTypes.Name)?.Value;
        return isHostTenant
            && !string.IsNullOrEmpty(userName)
            && userName.Equals("admin", StringComparison.OrdinalIgnoreCase);
    }
}
