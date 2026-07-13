using CodeMaster.Domain.Entities.Monitor;
using Quartz;
using SqlSugar;

namespace CodeMaster.Infrastructure.TaskScheduling.Jobs;

/// <summary>
/// SQL execution task.
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
        var log = await ExecuteJob(context, async () => await Run(context));
        await SaveTaskLog(context, log);
    }

    private async Task<string> Run(IJobExecutionContext context)
    {
        var invokeTarget = context.JobDetail.JobDataMap.GetString("InvokeTarget");
        if (string.IsNullOrWhiteSpace(invokeTarget))
        {
            throw new Exception("SQL statement cannot be empty.");
        }

        var result = await _db.Ado.ExecuteCommandAsync(invokeTarget);
        return $"SQL executed successfully. Affected rows: {result}";
    }

    private async Task SaveTaskLog(IJobExecutionContext context, SysTaskLog log)
    {
        var taskId = context.JobDetail.JobDataMap.GetLong("TaskId");
        log.TaskId = taskId;
        log.TaskName = context.JobDetail.JobDataMap.GetString("TaskName") ?? "";
        log.InvokeTarget = context.JobDetail.JobDataMap.GetString("InvokeTarget") ?? "";
        log.JobMessage = log.JobMessage?.Length > 2000 ? log.JobMessage[..2000] : log.JobMessage;
        log.CreateTime = DateTime.UtcNow;

        await _db.Insertable(log).ExecuteCommandAsync();

        if (log.Status == 0)
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
