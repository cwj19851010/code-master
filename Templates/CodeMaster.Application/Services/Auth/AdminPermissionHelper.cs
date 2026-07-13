using CodeMaster.Domain.Entities.System;
using SqlSugar;

namespace CodeMaster.Application.Services.Auth;

public static class AdminPermissionHelper
{
    public const int HostMenuScope = 0;
    public const int TenantMenuScope = 1;
    public const int SharedMenuScope = 2;

    public static bool IsHostAdmin(SysUser user, IEnumerable<SysRole> roles)
    {
        if (user.TenantId != 0)
        {
            return false;
        }

        return user.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || roles.Any(r => r.RoleKey.Equals("admin", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsTenantAdmin(SysUser user, IEnumerable<SysRole> roles)
    {
        return user.TenantId > 0 && roles.Any(r => r.IsTenantAdmin);
    }

    public static bool IsAdmin(SysUser user, IEnumerable<SysRole> roles)
    {
        return IsHostAdmin(user, roles) || IsTenantAdmin(user, roles);
    }

    public static ISugarQueryable<SysMenu> ApplyMenuScope(ISugarQueryable<SysMenu> query, long tenantId)
    {
        return tenantId == 0
            ? query.Where(m => m.MenuScope == HostMenuScope || m.MenuScope == SharedMenuScope)
            : query.Where(m => m.MenuScope == TenantMenuScope || m.MenuScope == SharedMenuScope);
    }

    public static async Task<List<string>> GetScopedPermissionsAsync(ISqlSugarClient db, long tenantId)
    {
        var query = db.Queryable<SysMenu>()
            .ClearFilter()
            .Where(m => m.Status == 0 && !string.IsNullOrEmpty(m.Perms));

        query = ApplyMenuScope(query, tenantId);

        return await query
            .Select(m => m.Perms!)
            .Distinct()
            .ToListAsync();
    }
}
