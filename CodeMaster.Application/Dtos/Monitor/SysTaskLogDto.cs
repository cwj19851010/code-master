using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.Monitor;

/// <summary>
/// 任务日志查询DTO
/// </summary>
public class SysTaskLogQueryDto : PagedQueryDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public long? TaskId { get; set; }

    /// <summary>
    /// 任务名称
    /// </summary>
    public string? TaskName { get; set; }

    /// <summary>
    /// 执行状态 (0=成功 1=失败)
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
/// 任务日志DTO
/// </summary>
public class SysTaskLogDto : EntityDto
{
    public long TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string InvokeTarget { get; set; } = string.Empty;
    public string? JobMessage { get; set; }
    public int Status { get; set; }
    public double Elapsed { get; set; }
    public DateTime CreateTime { get; set; }
}
