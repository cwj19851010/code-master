using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.Monitor;

/// <summary>
/// 操作日志查询DTO
/// </summary>
public class SysOperLogQueryDto : PagedQueryDto
{
    /// <summary>
    /// 模块标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 操作人员
    /// </summary>
    public string? OperName { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public int? BusinessType { get; set; }

    /// <summary>
    /// 操作状态（0成功 1失败）
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
/// 操作日志DTO
/// </summary>
public class SysOperLogDto : EntityDto
{
    /// <summary>
    /// 模块标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 业务类型
    /// </summary>
    public int BusinessType { get; set; }

    /// <summary>
    /// 方法名称
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 请求方式
    /// </summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>
    /// 操作类别
    /// </summary>
    public int OperatorType { get; set; }

    /// <summary>
    /// 操作人员
    /// </summary>
    public string OperName { get; set; } = string.Empty;

    /// <summary>
    /// 请求URL
    /// </summary>
    public string OperUrl { get; set; } = string.Empty;

    /// <summary>
    /// 主机地址
    /// </summary>
    public string OperIp { get; set; } = string.Empty;

    /// <summary>
    /// 操作地点
    /// </summary>
    public string? OperLocation { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public string? OperParam { get; set; }

    /// <summary>
    /// 返回参数
    /// </summary>
    public string? JsonResult { get; set; }

    /// <summary>
    /// 操作状态
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMsg { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperTime { get; set; }

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    public long Elapsed { get; set; }
}
