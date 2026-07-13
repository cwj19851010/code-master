using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Monitor;

/// <summary>
/// 任务执行日志实体
/// </summary>
[SugarTable("sys_task_logs")]
public class SysTaskLog : IEntity<long>
{
    /// <summary>
    /// 日志ID（主键，自增）
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "id", IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 任务ID
    /// </summary>
    [SugarColumn(ColumnName = "task_id")]
    public long TaskId { get; set; }

    /// <summary>
    /// 任务名称
    /// </summary>
    [SugarColumn(ColumnName = "task_name", Length = 100)]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 调用目标
    /// </summary>
    [SugarColumn(ColumnName = "invoke_target", Length = 500)]
    public string InvokeTarget { get; set; } = string.Empty;

    /// <summary>
    /// 执行消息
    /// </summary>
    [SugarColumn(ColumnName = "job_message", Length = 2000, IsNullable = true)]
    public string? JobMessage { get; set; }

    /// <summary>
    /// 状态：0=成功, 1=失败
    /// </summary>
    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "elapsed")]
    public double Elapsed { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
