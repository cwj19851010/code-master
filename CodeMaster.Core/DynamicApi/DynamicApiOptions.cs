namespace CodeMaster.Core.DynamicApi;

/// <summary>
/// 动态 API 配置选项
/// </summary>
public class DynamicApiOptions
{
    /// <summary>
    /// API 路由前缀，默认 "api"
    /// </summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// 是否启用动态 API，默认 true
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否移除 Service 后缀，默认 true
    /// 例如：UserService -> User
    /// </summary>
    public bool RemoveServiceSuffix { get; set; } = true;

    /// <summary>
    /// 是否移除 AppService 后缀，默认 true
    /// 例如：UserAppService -> User
    /// </summary>
    public bool RemoveAppServiceSuffix { get; set; } = true;

    /// <summary>
    /// 是否使用小写路由，默认 true
    /// 例如：UserService -> /api/user
    /// </summary>
    public bool UseLowercaseRoutes { get; set; } = true;

    /// <summary>
    /// 是否使用复数形式，默认 false
    /// 例如：User -> users
    /// </summary>
    public bool UsePluralizeRoutes { get; set; } = false;
}
