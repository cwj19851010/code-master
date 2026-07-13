using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.Monitor;

/// <summary>
/// 登录日志查询DTO
/// </summary>
public class SysLoginLogQueryDto : PagedQueryDto
{
    /// <summary>
    /// 用户账号
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 登录IP地址
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// 登录状态（0成功 1失败）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? BeginTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 登录日志DTO
/// </summary>
public class SysLoginLogDto : EntityDto
{
    /// <summary>
    /// 用户账号
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 登录IP地址
    /// </summary>
    public string LoginIp { get; set; } = string.Empty;

    /// <summary>
    /// 登录地点
    /// </summary>
    public string? LoginLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 登录状态
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 提示消息
    /// </summary>
    public string? Msg { get; set; }

    /// <summary>
    /// 访问时间
    /// </summary>
    public DateTime LoginTime { get; set; }
}
