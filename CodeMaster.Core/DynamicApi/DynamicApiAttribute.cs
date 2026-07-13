namespace CodeMaster.Core.DynamicApi;

/// <summary>
/// 动态 API 特性（用于自定义动态 API 行为）
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DynamicApiAttribute : Attribute
{
    /// <summary>
    /// 是否启用动态 API（默认 true）
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// HTTP 方法（GET/POST/PUT/DELETE/PATCH）
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// 自定义路由（相对于 Controller 路由）
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// 权限标识
    /// </summary>
    public string? Permission { get; set; }
}
