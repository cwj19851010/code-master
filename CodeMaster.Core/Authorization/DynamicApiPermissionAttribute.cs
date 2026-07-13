namespace CodeMaster.Core.Authorization;

/// <summary>
/// 动态API权限配置特性
/// 用于配置整个Service类的默认权限行为
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DynamicApiPermissionAttribute : Attribute
{
    /// <summary>
    /// 是否需要权限验证
    /// true: 需要按照权限规则验证（默认）
    /// false: 只需要登录即可访问
    /// </summary>
    public bool RequirePermission { get; set; } = true;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="requirePermission">是否需要权限验证，默认true</param>
    public DynamicApiPermissionAttribute(bool requirePermission = true)
    {
        RequirePermission = requirePermission;
    }
}
