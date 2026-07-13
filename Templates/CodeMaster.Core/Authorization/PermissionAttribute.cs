using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CodeMaster.Core.Authorization;

/// <summary>
/// 权限验证特性
/// 用于标记需要特定权限才能访问的 API
/// 支持多个权限（OR 逻辑）：任意一个权限满足即可访问
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAttribute : AuthorizeAttribute, IFilterMetadata
{
    /// <summary>
    /// 权限代码（多个权限用逗号或竖线分隔，表示 OR 逻辑）
    /// </summary>
    public string PermissionCode { get; }

    /// <summary>
    /// 权限代码数组
    /// </summary>
    public string[] PermissionCodes { get; }

    /// <summary>
    /// 构造函数（单个权限）
    /// </summary>
    /// <param name="permissionCode">权限代码，支持多个权限用逗号或竖线分隔（如："system:user:view,system:user:list" 或 "system:user:view|system:user:list"）</param>
    public PermissionAttribute(string permissionCode)
    {
        PermissionCode = permissionCode;

        // 支持逗号或竖线分隔多个权限
        PermissionCodes = permissionCode
            .Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToArray();

        // 如果有多个权限，使用 PermissionOr 策略；否则使用 Permission 策略
        if (PermissionCodes.Length > 1)
        {
            Policy = $"PermissionOr:{string.Join(",", PermissionCodes)}";
        }
        else
        {
            Policy = $"Permission:{PermissionCodes[0]}";
        }
    }

    /// <summary>
    /// 构造函数（多个权限）
    /// </summary>
    /// <param name="permissionCodes">权限代码数组，任意一个满足即可</param>
    public PermissionAttribute(params string[] permissionCodes)
    {
        PermissionCodes = permissionCodes;
        PermissionCode = string.Join(",", permissionCodes);

        // 如果有多个权限，使用 PermissionOr 策略；否则使用 Permission 策略
        if (PermissionCodes.Length > 1)
        {
            Policy = $"PermissionOr:{string.Join(",", PermissionCodes)}";
        }
        else
        {
            Policy = $"Permission:{PermissionCodes[0]}";
        }
    }
}
