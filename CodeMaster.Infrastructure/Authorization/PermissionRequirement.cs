using Microsoft.AspNetCore.Authorization;

namespace CodeMaster.Infrastructure.Authorization;

/// <summary>
/// 权限要求
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
        Permissions = new[] { permission };
    }

    public PermissionRequirement(string[] permissions)
    {
        Permissions = permissions;
        Permission = string.Join(",", permissions);
    }

    public string Permission { get; }

    /// <summary>
    /// 权限列表（OR 逻辑：任意一个满足即可）
    /// </summary>
    public string[] Permissions { get; }
}
