using Quartz;
using CodeMaster.Domain.Entities.System;
using SqlSugar;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using CodeMaster.Domain.Entities.Monitor;

namespace CodeMaster.Infrastructure.TaskScheduling.Jobs;

/// <summary>
/// Assembly程序集任务
/// </summary>
public class AssemblyJob : JobBase, IJob
{
    private readonly ISqlSugarClient _db;
    private readonly IServiceProvider _serviceProvider;

    public AssemblyJob(ISqlSugarClient db, IServiceProvider serviceProvider)
    {
        _db = db;
        _serviceProvider = serviceProvider;
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
            throw new Exception("调用目标不能为空");
        }

        // 解析调用目标: AssemblyName.ClassName.MethodName(params)
        var parts = invokeTarget.Split('(');
        var methodPath = parts[0];
        var parameters = parts.Length > 1 ? parts[1].TrimEnd(')') : string.Empty;

        var pathParts = methodPath.Split('.');
        if (pathParts.Length < 3)
        {
            throw new Exception("调用目标格式错误，正确格式: AssemblyName.ClassName.MethodName");
        }

        var assemblyName = pathParts[0];
        var className = string.Join(".", pathParts.Take(pathParts.Length - 1));
        var methodName = pathParts[^1];

        // 加载程序集
        var assembly = Assembly.Load(new AssemblyName(assemblyName));
        var type = assembly.GetType(className);

        if (type == null)
        {
            throw new Exception($"未找到类型: {className}");
        }

        // 创建实例（支持依赖注入）
        var instance = ActivatorUtilities.CreateInstance(_serviceProvider, type);
        var method = type.GetMethod(methodName);

        if (method == null)
        {
            throw new Exception($"未找到方法: {methodName}");
        }

        // 执行方法
        object? result;
        if (method.ReturnType == typeof(Task))
        {
            await (Task)method.Invoke(instance, null)!;
            result = "执行成功";
        }
        else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var task = (Task)method.Invoke(instance, null)!;
            await task;
            var resultProperty = task.GetType().GetProperty("Result");
            result = resultProperty?.GetValue(task);
        }
        else
        {
            result = method.Invoke(instance, null);
        }

        var message = result?.ToString() ?? "执行成功";

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
