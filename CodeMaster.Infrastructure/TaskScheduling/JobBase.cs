using Quartz;
using System.Diagnostics;
using CodeMaster.Domain.Entities.Monitor;

namespace CodeMaster.Infrastructure.TaskScheduling;

/// <summary>
/// Job基类，提供任务执行和日志记录功能
/// </summary>
public abstract class JobBase
{
    /// <summary>
    /// 执行指定任务
    /// </summary>
    /// <param name="context">作业上下文</param>
    /// <param name="job">业务逻辑方法</param>
    public async Task<SysTaskLog> ExecuteJob(IJobExecutionContext context, Func<Task> job)
    {
        double elapsed = 0;
        int status = 0;
        string logMsg;

        try
        {
            // 记录Job时间
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // 执行任务
            await job();

            stopwatch.Stop();
            elapsed = stopwatch.Elapsed.TotalMilliseconds;
            logMsg = "执行成功";
        }
        catch (Exception ex)
        {
            status = 1;
            logMsg = $"执行失败: {ex.Message}";
            Console.WriteLine($"任务执行出错: {logMsg}");
        }

        var logModel = new SysTaskLog
        {
            Elapsed = elapsed,
            Status = status,
            JobMessage = logMsg
        };

        return logModel;
    }

    /// <summary>
    /// 执行指定任务（接收返回结果）
    /// </summary>
    /// <param name="context">作业上下文</param>
    /// <param name="job">业务逻辑方法</param>
    public async Task<SysTaskLog> ExecuteJob(IJobExecutionContext context, Func<Task<string>> job)
    {
        double elapsed = 0;
        int status = 0;
        string logMsg;

        try
        {
            // 记录Job时间
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // 执行任务，并返回结果
            string result = await job();

            stopwatch.Stop();
            elapsed = stopwatch.Elapsed.TotalMilliseconds;
            logMsg = result.Length <= 2000 ? result : result.Substring(0, 2000);
        }
        catch (Exception ex)
        {
            status = 1;
            logMsg = $"执行失败: {ex.Message}";
            Console.WriteLine($"任务执行出错: {logMsg}");
        }

        var logModel = new SysTaskLog
        {
            Elapsed = elapsed,
            Status = status,
            JobMessage = logMsg
        };

        return logModel;
    }
}
