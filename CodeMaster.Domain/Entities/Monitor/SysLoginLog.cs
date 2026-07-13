using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Monitor;

/// <summary>
/// 登录日志表
/// </summary>
[SugarTable("sys_login_log")]
public class SysLoginLog : EntityBase
{
    /// <summary>
    /// 用户账号
    /// </summary>
    [SugarColumn(ColumnName = "user_name", Length = 50)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 登录IP地址
    /// </summary>
    [SugarColumn(ColumnName = "login_ip", Length = 50)]
    public string LoginIp { get; set; } = string.Empty;

    /// <summary>
    /// 登录地点
    /// </summary>
    [SugarColumn(ColumnName = "login_location", Length = 255, IsNullable = true)]
    public string? LoginLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    [SugarColumn(ColumnName = "browser", Length = 50, IsNullable = true)]
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [SugarColumn(ColumnName = "os", Length = 50, IsNullable = true)]
    public string? Os { get; set; }

    /// <summary>
    /// 登录状态（0成功 1失败）
    /// </summary>
    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }

    /// <summary>
    /// 提示消息
    /// </summary>
    [SugarColumn(ColumnName = "msg", Length = 255, IsNullable = true)]
    public string? Msg { get; set; }

    /// <summary>
    /// 访问时间
    /// </summary>
    [SugarColumn(ColumnName = "login_time")]
    public DateTime LoginTime { get; set; }
}
