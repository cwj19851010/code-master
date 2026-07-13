using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.Monitor;

/// <summary>
/// 任务查询DTO
/// </summary>
public class SysTaskQueryDto : PagedQueryDto
{
    /// <summary>
    /// 任务名称
    /// </summary>
    public string? TaskName { get; set; }

    /// <summary>
    /// 任务组
    /// </summary>
    public string? JobGroup { get; set; }

    /// <summary>
    /// 任务类型 (0=Assembly 1=HTTP 2=SQL)
    /// </summary>
    public int? TaskType { get; set; }

    /// <summary>
    /// 任务状态 (0=正常 1=暂停)
    /// </summary>
    public int? Status { get; set; }
}

/// <summary>
/// 任务DTO
/// </summary>
public class SysTaskDto : EntityDto
{
    public string TaskName { get; set; } = string.Empty;
    public string JobGroup { get; set; } = string.Empty;
    public int TaskType { get; set; }
    public string InvokeTarget { get; set; } = string.Empty;
    public string? CronExpression { get; set; }
    public int IntervalSecond { get; set; }
    public int RunTimes { get; set; }
    public DateTime? BeginTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? LastRunTime { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
}

/// <summary>
/// 创建任务DTO
/// </summary>
public class CreateSysTaskDto
{
    public string TaskName { get; set; } = string.Empty;
    public string JobGroup { get; set; } = "DEFAULT";
    public int TaskType { get; set; }
    public string InvokeTarget { get; set; } = string.Empty;
    public string? CronExpression { get; set; }
    public int IntervalSecond { get; set; } = 60;
    public int RunTimes { get; set; } = 0;
    public DateTime? BeginTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Status { get; set; } = 0;
    public string? Remark { get; set; }
}

/// <summary>
/// 更新任务DTO
/// </summary>
public class UpdateSysTaskDto
{
    public long TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string JobGroup { get; set; } = "DEFAULT";
    public int TaskType { get; set; }
    public string InvokeTarget { get; set; } = string.Empty;
    public string? CronExpression { get; set; }
    public int IntervalSecond { get; set; } = 60;
    public int RunTimes { get; set; } = 0;
    public DateTime? BeginTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
}
