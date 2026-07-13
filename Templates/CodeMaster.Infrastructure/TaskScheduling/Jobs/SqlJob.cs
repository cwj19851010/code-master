using Quartz;
using CodeMaster.Domain.Entities.System;
using SqlSugar;
using CodeMaster.Domain.Entities.Monitor;

namespace CodeMaster.Infrastructure.TaskScheduling.Jobs;

/// <summary>
/// SQL执行任务
/// </summary>
public class SqlJob : JobBase, IJob
{
    private readonly ISqlSugarClient _db;

    public SqlJob(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await ExecuteJob(context, async () => await Run(context));
    }

    private async Task<string> Run(IJobExecutionContext context)
    {
        var taskId = context.JobDetail.JobDataMap.GetLong("TaskId");
        var invokeTarget = context.JobDetail.JobDataMap.GetString("InvokeTarget");

        if (string.IsNullOrEmpty(invokeTarget))
        {
            throw new Exception("SQL语句不能为空");
        }

        // 执行SQL
        var result = await _db.Ado.ExecuteCommandAsync(invokeTarget);

        var message = $"SQL执行成功，影响行数: {result}";

        // 记录日志
        await SaveTaskLog(context, taskId, 0, message);

        return message;
    }

    private async Task SaveTaskLog(IJobExecutionContext context, long taskId, int status, string message)
    {
        var log = new SysTaskLog
        {
            TaskId = taskId,
            TaskName = context.JobDetail.JobDataMap.GetString("TaskName") ?? "",
            InvokeTarget = context.JobDetail.JobDataMap.GetString("InvokeTarget") ?? "",
            Status = status,
            JobMessage = message.Length > 2000 ? message.Substring(0, 2000) : message,
            CreateTime = DateTime.UtcNow
        };

        await _db.Insertable(log).ExecuteCommandAsync();

        // 更新任务执行次数
        if (status == 0)
        {
            await _db.Updateable<SysTask>()
                .SetColumns(t => new SysTask
                {
                    RunTimes = t.RunTimes + 1,
                    LastRunTime = DateTime.UtcNow
                })
                .Where(t => t.Id == taskId)
                .ExecuteCommandAsync();
        }
    }
}
