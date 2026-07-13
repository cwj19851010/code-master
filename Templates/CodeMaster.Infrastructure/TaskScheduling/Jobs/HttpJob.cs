using Quartz;
using CodeMaster.Domain.Entities.System;
using SqlSugar;
using CodeMaster.Domain.Entities.Monitor;

namespace CodeMaster.Infrastructure.TaskScheduling.Jobs;

/// <summary>
/// HTTP请求任务
/// </summary>
public class HttpJob : JobBase, IJob
{
    private readonly ISqlSugarClient _db;
    private readonly HttpClient _httpClient;

    public HttpJob(ISqlSugarClient db, HttpClient httpClient)
    {
        _db = db;
        _httpClient = httpClient;
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
            throw new Exception("HTTP请求地址不能为空");
        }

        // 解析调用目标: GET:http://example.com 或 POST:http://example.com|{"key":"value"}
        var parts = invokeTarget.Split('|');
        var methodAndUrl = parts[0].Split(':');

        if (methodAndUrl.Length < 2)
        {
            throw new Exception("HTTP请求格式错误，正确格式: GET:url 或 POST:url|body");
        }

        var method = methodAndUrl[0].ToUpper();
        var url = methodAndUrl[1];
        var body = parts.Length > 1 ? parts[1] : string.Empty;

        string result;

        if (method == "POST")
        {
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            result = await response.Content.ReadAsStringAsync();
        }
        else
        {
            if (!string.IsNullOrEmpty(body))
            {
                url += (url.Contains("?") ? "&" : "?") + body;
            }
            var response = await _httpClient.GetAsync(url);
            result = await response.Content.ReadAsStringAsync();
        }

        // 记录日志
        await SaveTaskLog(context, taskId, 0, result);

        return result;
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
