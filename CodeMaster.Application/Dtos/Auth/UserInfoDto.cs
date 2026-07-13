namespace CodeMaster.Application.Dtos.Auth;

/// <summary>
/// 用户信息DTO
/// </summary>
public class UserInfoDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    public string NickName { get; set; } = string.Empty;

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public long? DeptId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// 数据权限范围（1全部/2自定义/3本部门/4本部门及以下/5仅本人）
    /// </summary>
    public int DataScope { get; set; } = 1;

    public bool IsAdmin { get; set; }

    public bool IsHostAdmin { get; set; }

    public bool IsTenantAdmin { get; set; }
}

/// <summary>
/// 用户信息响应DTO（前端期望的格式）
/// </summary>
public class UserInfoResponseDto
{
    /// <summary>
    /// 用户基本信息
    /// </summary>
    public UserBasicDto User { get; set; } = new();

    /// <summary>
    /// 角色列表
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// 权限列表
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    public bool IsAdmin { get; set; }

    public bool IsHostAdmin { get; set; }

    public bool IsTenantAdmin { get; set; }
}

/// <summary>
/// 用户基本信息DTO
/// </summary>
public class UserBasicDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    public string NickName { get; set; } = string.Empty;

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public long? DeptId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    public string? DeptName { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsHostAdmin { get; set; }

    public bool IsTenantAdmin { get; set; }
}
