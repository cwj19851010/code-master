using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using Quartz.Spi;
using System.Collections.Specialized;
using System.Reflection;
using CodeMaster.Infrastructure.TaskScheduling.Jobs;
using CodeMaster.Domain.Entities.Monitor;

namespace CodeMaster.Infrastructure.TaskScheduling;

/// <summary>
/// 任务调度服务器
/// </summary>
public class TaskSchedulerServer : ITaskSchedulerServer
{
    private Task<IScheduler> _scheduler;
    private readonly IJobFactory _jobFactory;

    public TaskSchedulerServer(IJobFactory jobFactory)
    {
        _scheduler = GetTaskSchedulerAsync();
        _jobFactory = jobFactory;
    }

    /// <summary>
    /// 获取任务调度器
    /// </summary>
    private Task<IScheduler> GetTaskSchedulerAsync()
    {
        if (_scheduler != null)
        {
            return _scheduler;
        }

        NameValueCollection collection = new NameValueCollection
        {
            { "quartz.serializer.type", "json" }
        };

        StdSchedulerFactory factory = new StdSchedulerFactory(collection);
        return _scheduler = factory.GetScheduler();
    }

    /// <summary>
    /// 启动任务调度器
    /// </summary>
    public async Task<bool> StartTaskScheduleAsync()
    {
        try
        {
            _scheduler.Result.JobFactory = _jobFactory;
            if (_scheduler.Result.IsStarted)
            {
                return true;
            }

            await _scheduler.Result.Start();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动任务调度器失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 停止任务调度器
    /// </summary>
    public async Task<bool> StopTaskScheduleAsync()
    {
        try
        {
            if (_scheduler.Result.IsShutdown)
            {
                return true;
            }

            await _scheduler.Result.Shutdown();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"停止任务调度器失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 添加任务
    /// </summary>
    public async Task<(bool Success, string Message)> AddTaskScheduleAsync(SysTask task)
    {
        try
        {
            JobKey jobKey = new JobKey(task.Id.ToString(), task.JobGroup);
            if (await _scheduler.Result.CheckExists(jobKey))
            {
                return (false, $"任务已存在: {task.TaskName}");
            }

            if (task.EndTime <= DateTime.UtcNow)
            {
                return (false, "结束时间不能小于当前时间");
            }

            task.BeginTime = task.BeginTime ?? DateTime.UtcNow;
            task.EndTime = task.EndTime ?? DateTime.MaxValue.AddDays(-1);

            // 根据任务类型创建Job
            Type jobType = task.TaskType switch
            {
                0 => typeof(AssemblyJob), // Assembly任务
                1 => typeof(HttpJob),     // HTTP任务
                2 => typeof(SqlJob),      // SQL任务
                _ => throw new Exception($"不支持的任务类型: {task.TaskType}")
            };

            // 开启调度器
            if (!_scheduler.Result.IsStarted)
            {
                await StartTaskScheduleAsync();
            }

            // 创建任务
            IJobDetail job = JobBuilder.Create(jobType)
                .WithIdentity(task.Id.ToString(), task.JobGroup)
                .Build();

            job.JobDataMap.Add("TaskId", task.Id);
            job.JobDataMap.Add("TaskName", task.TaskName);
            job.JobDataMap.Add("InvokeTarget", task.InvokeTarget);

            // 创建触发器
            ITrigger trigger;
            if (!string.IsNullOrEmpty(task.CronExpression))
            {
                trigger = CreateCronTrigger(task);
                ((CronTriggerImpl)trigger).MisfireInstruction = MisfireInstruction.CronTrigger.DoNothing;
            }
            else
            {
                trigger = CreateSimpleTrigger(task);
                ((SimpleTriggerImpl)trigger).MisfireInstruction = MisfireInstruction.SimpleTrigger.RescheduleNowWithExistingRepeatCount;
            }

            // 绑定任务和触发器
            await _scheduler.Result.ScheduleJob(job, trigger);
            await _scheduler.Result.ResumeTrigger(trigger.Key);

            return (true, $"启动任务成功: {task.TaskName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"添加任务失败: {ex.Message}");
            return (false, $"启动任务失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 暂停任务
    /// </summary>
    public async Task<(bool Success, string Message)> PauseTaskScheduleAsync(SysTask task)
    {
        try
        {
            JobKey jobKey = new JobKey(task.Id.ToString(), task.JobGroup);
            if (await _scheduler.Result.CheckExists(jobKey))
            {
                await _scheduler.Result.PauseJob(jobKey);
            }
            return (true, $"暂停任务成功: {task.TaskName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"暂停任务失败: {ex.Message}");
            return (false, $"暂停任务失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 恢复任务
    /// </summary>
    public async Task<(bool Success, string Message)> ResumeTaskScheduleAsync(SysTask task)
    {
        try
        {
            JobKey jobKey = new JobKey(task.Id.ToString(), task.JobGroup);
            if (!await _scheduler.Result.CheckExists(jobKey))
            {
                return (false, $"未找到任务: {task.TaskName}");
            }
            await _scheduler.Result.ResumeJob(jobKey);
            return (true, $"恢复任务成功: {task.TaskName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"恢复任务失败: {ex.Message}");
            return (false, $"恢复任务失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteTaskScheduleAsync(SysTask task)
    {
        try
        {
            JobKey jobKey = new JobKey(task.Id.ToString(), task.JobGroup);
            await _scheduler.Result.DeleteJob(jobKey);
            return (true, $"删除任务成功: {task.TaskName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除任务失败: {ex.Message}");
            return (false, $"删除任务失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 立即运行任务
    /// </summary>
    public async Task<(bool Success, string Message)> RunTaskScheduleAsync(SysTask task)
    {
        try
        {
            JobKey jobKey = new JobKey(task.Id.ToString(), task.JobGroup);
            var jobKeys = _scheduler.Result.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(task.JobGroup)).Result.ToList();

            if (jobKeys == null || jobKeys.Count == 0)
            {
                await AddTaskScheduleAsync(task);
            }

            var triggers = await _scheduler.Result.GetTriggersOfJob(jobKey);
            if (triggers.Count <= 0)
            {
                return (false, $"未找到触发器: {task.TaskName}");
            }

            await _scheduler.Result.TriggerJob(jobKey);
            return (true, $"运行任务成功: {task.TaskName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"运行任务失败: {ex.Message}");
            return (false, $"运行任务失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新任务
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateTaskScheduleAsync(SysTask task)
    {
        try
        {
            JobKey jobKey = new JobKey(task.Id.ToString(), task.JobGroup);
            if (await _scheduler.Result.CheckExists(jobKey))
            {
                await _scheduler.Result.DeleteJob(jobKey);
            }
            return (true, "修改任务成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"修改任务失败: {ex.Message}");
            return (false, $"修改任务失败: {ex.Message}");
        }
    }

    #region 创建触发器帮助方法

    /// <summary>
    /// 创建SimpleTrigger触发器
    /// </summary>
    private ITrigger CreateSimpleTrigger(SysTask task)
    {
        if (task.RunTimes > 0)
        {
            return TriggerBuilder.Create()
                .WithIdentity(task.Id.ToString(), task.JobGroup)
                .StartAt(task.BeginTime!.Value)
                .EndAt(task.EndTime!.Value)
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(task.IntervalSecond)
                    .WithRepeatCount(task.RunTimes))
                .ForJob(task.Id.ToString(), task.JobGroup)
                .Build();
        }
        else
        {
            return TriggerBuilder.Create()
                .WithIdentity(task.Id.ToString(), task.JobGroup)
                .StartAt(task.BeginTime!.Value)
                .EndAt(task.EndTime!.Value)
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(task.IntervalSecond)
                    .RepeatForever())
                .ForJob(task.Id.ToString(), task.JobGroup)
                .Build();
        }
    }

    /// <summary>
    /// 创建CronTrigger触发器
    /// </summary>
    private ITrigger CreateCronTrigger(SysTask task)
    {
        return TriggerBuilder.Create()
            .WithIdentity(task.Id.ToString(), task.JobGroup)
            .StartAt(task.BeginTime!.Value)
            .EndAt(task.EndTime!.Value)
            .WithCronSchedule(task.CronExpression!)
            .ForJob(task.Id.ToString(), task.JobGroup)
            .Build();
    }

    #endregion
}

/// <summary>
/// 任务调度服务器接口
/// </summary>
public interface ITaskSchedulerServer
{
    Task<bool> StartTaskScheduleAsync();
    Task<bool> StopTaskScheduleAsync();
    Task<(bool Success, string Message)> AddTaskScheduleAsync(SysTask task);
    Task<(bool Success, string Message)> PauseTaskScheduleAsync(SysTask task);
    Task<(bool Success, string Message)> ResumeTaskScheduleAsync(SysTask task);
    Task<(bool Success, string Message)> DeleteTaskScheduleAsync(SysTask task);
    Task<(bool Success, string Message)> RunTaskScheduleAsync(SysTask task);
    Task<(bool Success, string Message)> UpdateTaskScheduleAsync(SysTask task);
}
