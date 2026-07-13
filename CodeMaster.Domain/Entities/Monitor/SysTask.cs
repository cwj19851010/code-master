using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Monitor;

/// <summary>
/// 定时任务实体
/// </summary>
[SugarTable("sys_tasks")]
public class SysTask : EntityBase
{
    /// <summary>
    /// 任务名称
    /// </summary>
    [SugarColumn(ColumnName = "task_name", Length = 100)]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 任务分组
    /// </summary>
    [SugarColumn(ColumnName = "job_group", Length = 50)]
    public string JobGroup { get; set; } = "DEFAULT";

    /// <summary>
    /// 任务类型 (0=Assembly 1=HTTP 2=SQL)
    /// </summary>
    [SugarColumn(ColumnName = "task_type")]
    public int TaskType { get; set; }

    /// <summary>
    /// 调用目标（根据任务类型不同，格式不同）
    /// </summary>
    [SugarColumn(ColumnName = "invoke_target", Length = 500)]
    public string InvokeTarget { get; set; } = string.Empty;

    /// <summary>
    /// Cron表达式
    /// </summary>
    [SugarColumn(ColumnName = "cron_expression", Length = 100, IsNullable = true)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// 执行间隔（秒）
    /// </summary>
    [SugarColumn(ColumnName = "interval_second")]
    public int IntervalSecond { get; set; } = 60;

    /// <summary>
    /// 执行次数（0表示无限次）
    /// </summary>
    [SugarColumn(ColumnName = "run_times")]
    public int RunTimes { get; set; } = 0;

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnName = "begin_time", IsNullable = true)]
    public DateTime? BeginTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnName = "end_time", IsNullable = true)]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 最后执行时间
    /// </summary>
    [SugarColumn(ColumnName = "last_run_time", IsNullable = true)]
    public DateTime? LastRunTime { get; set; }

    /// <summary>
    /// 任务状态 (0=正常 1=暂停)
    /// </summary>
    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; } = 0;
}
