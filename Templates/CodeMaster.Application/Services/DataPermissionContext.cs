using CodeMaster.Core.Enums;
using CodeMaster.Core.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CodeMaster.Application.Services;

/// <summary>
/// 数据权限上下文服务
/// 从 HttpContext 获取当前用户的数据权限信息
/// </summary>
public class DataPermissionContext : IDataPermissionContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DataPermissionContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 是否启用数据权限过滤
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否管理员（管理员不受数据权限限制）
    /// </summary>
    public bool IsAdmin
    {
        get
        {
            var isAdminClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("IsAdmin");
            return isAdminClaim?.Value == "true";
        }
        set { } // 从 JWT Token 读取，不需要设置
    }

    /// <summary>
    /// 当前用户ID
    /// </summary>
    public long? UserId
    {
        get => GetCurrentUserId();
        set { } // 从 JWT Token 读取，不需要设置
    }

    /// <summary>
    /// 当前部门ID
    /// </summary>
    public long? DeptId
    {
        get => GetCurrentDeptId();
        set { } // 从 JWT Token 读取，不需要设置
    }

    /// <summary>
    /// 当前部门路径（祖先路径）
    /// </summary>
    public string? DeptAncestors
    {
        get => GetCurrentDeptAncestors();
        set { } // 从 JWT Token 读取，不需要设置
    }

    /// <summary>
    /// 数据可见范围
    /// </summary>
    public int DataScope
    {
        get => (int)GetCurrentDataScope();
        set { } // 从 JWT Token 读取，不需要设置
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    public long? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// 获取当前用户的数据权限范围（从职位获取）
    /// </summary>
    public PostDataScope GetCurrentDataScope()
    {
        var dataScopeClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("PostDataScope");
        if (dataScopeClaim != null && Enum.TryParse<PostDataScope>(dataScopeClaim.Value, out var dataScope))
        {
            return dataScope;
        }
        // 默认返回仅本人数据权限（最严格）
        return PostDataScope.Self;
    }

    /// <summary>
    /// 获取当前用户的部门ID
    /// </summary>
    public long? GetCurrentDeptId()
    {
        var deptIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("DeptId");
        if (deptIdClaim != null && long.TryParse(deptIdClaim.Value, out var deptId))
        {
            return deptId;
        }
        return null;
    }

    /// <summary>
    /// 获取当前用户的部门路径（祖先路径）
    /// 格式：0,1,2,3
    /// </summary>
    public string? GetCurrentDeptAncestors()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("DeptAncestors")?.Value;
    }

    /// <summary>
    /// 判断指定部门路径是否在当前用户的权限范围内
    /// </summary>
    /// <param name="targetDeptAncestors">目标数据的部门路径</param>
    /// <returns>true表示在权限范围内，false表示不在</returns>
    public bool IsInDataScope(string? targetDeptAncestors)
    {
        var dataScope = GetCurrentDataScope();

        // 全部数据权限
        if (dataScope == PostDataScope.All)
        {
            return true;
        }

        var currentDeptAncestors = GetCurrentDeptAncestors();
        if (string.IsNullOrEmpty(currentDeptAncestors) || string.IsNullOrEmpty(targetDeptAncestors))
        {
            return false;
        }

        // 本部门及以下数据权限：判断目标部门路径是否包含当前用户部门路径
        // 例如：当前用户部门路径 "0,1,2"，目标数据部门路径 "0,1,2,3" 或 "0,1,2" 都在权限范围内
        if (dataScope == PostDataScope.DeptAndBelow)
        {
            return targetDeptAncestors.StartsWith(currentDeptAncestors);
        }

        // 本部门数据权限：判断目标部门路径是否与当前用户部门路径完全相同
        if (dataScope == PostDataScope.Dept)
        {
            return targetDeptAncestors == currentDeptAncestors;
        }

        // 仅本人数据权限：需要在调用方通过 CreateUserId 判断
        return false;
    }
}
