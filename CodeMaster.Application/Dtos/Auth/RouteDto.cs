namespace CodeMaster.Application.Dtos.Auth;

/// <summary>
/// 路由DTO
/// </summary>
public class RouteDto
{
    /// <summary>
    /// 路由路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 路由名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 组件路径
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 重定向
    /// </summary>
    public string? Redirect { get; set; }

    /// <summary>
    /// 路由元信息
    /// </summary>
    public RouteMetaDto Meta { get; set; } = new();

    /// <summary>
    /// 子路由
    /// </summary>
    public List<RouteDto>? Children { get; set; }
}

/// <summary>
/// 路由元信息DTO
/// </summary>
public class RouteMetaDto
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 是否隐藏
    /// </summary>
    public bool Hidden { get; set; } = false;

    /// <summary>
    /// 是否缓存
    /// </summary>
    public bool NoCache { get; set; } = false;

    /// <summary>
    /// 权限标识
    /// </summary>
    public string? Permission { get; set; }

    /// <summary>
    /// 是否固定在标签页
    /// </summary>
    public bool Affix { get; set; } = false;
}
