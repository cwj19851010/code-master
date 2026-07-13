using System.Reflection;
using CodeMaster.Domain.Entities.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SqlSugar;

namespace CodeMaster.Infrastructure.TaskScheduling.Jobs;

/// <summary>
/// Assembly method task.
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
        var log = await ExecuteJob(context, async () => await Run(context));
        await SaveTaskLog(context, log);
    }

    private async Task<string> Run(IJobExecutionContext context)
    {
        var invokeTarget = context.JobDetail.JobDataMap.GetString("InvokeTarget");
        if (string.IsNullOrWhiteSpace(invokeTarget))
        {
            throw new Exception("Invoke target cannot be empty.");
        }

        // Format: AssemblyName.Namespace.ClassName.MethodName()
        var parts = invokeTarget.Split('(', 2);
        var methodPath = parts[0];
        var pathParts = methodPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length < 3)
        {
            throw new Exception("Invalid invoke target format. Use AssemblyName.Namespace.ClassName.MethodName.");
        }

        var assemblyName = pathParts[0];
        var className = string.Join(".", pathParts.Take(pathParts.Length - 1));
        var methodName = pathParts[^1];

        var assembly = Assembly.Load(new AssemblyName(assemblyName));
        var type = assembly.GetType(className)
            ?? throw new Exception($"Type not found: {className}");

        var method = type.GetMethod(methodName)
            ?? throw new Exception($"Method not found: {methodName}");

        var instance = ActivatorUtilities.CreateInstance(_serviceProvider, type);
        object? result;
        if (method.ReturnType == typeof(Task))
        {
            await (Task)method.Invoke(instance, null)!;
            result = "Executed successfully.";
        }
        else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var task = (Task)method.Invoke(instance, null)!;
            await task;
            result = task.GetType().GetProperty("Result")?.GetValue(task);
        }
        else
        {
            result = method.Invoke(instance, null);
        }

        return result?.ToString() ?? "Executed successfully.";
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
