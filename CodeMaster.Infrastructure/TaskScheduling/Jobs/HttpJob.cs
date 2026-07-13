using CodeMaster.Domain.Entities.Monitor;
using Quartz;
using SqlSugar;

namespace CodeMaster.Infrastructure.TaskScheduling.Jobs;

/// <summary>
/// HTTP request task.
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
        var log = await ExecuteJob(context, async () => await Run(context));
        await SaveTaskLog(context, log);
    }

    private async Task<string> Run(IJobExecutionContext context)
    {
        var invokeTarget = context.JobDetail.JobDataMap.GetString("InvokeTarget");
        if (string.IsNullOrWhiteSpace(invokeTarget))
        {
            throw new Exception("HTTP request URL cannot be empty.");
        }

        // Format: GET:http://example.com or POST:http://example.com|{"key":"value"}
        var parts = invokeTarget.Split('|', 2);
        var methodAndUrl = parts[0];
        var separatorIndex = methodAndUrl.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == methodAndUrl.Length - 1)
        {
            throw new Exception("Invalid HTTP task format. Use GET:url or POST:url|body.");
        }

        var method = methodAndUrl[..separatorIndex].Trim().ToUpperInvariant();
        var url = methodAndUrl[(separatorIndex + 1)..].Trim();
        var body = parts.Length > 1 ? parts[1] : string.Empty;

        if (method == "POST")
        {
            using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            return result;
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            url += (url.Contains('?') ? "&" : "?") + body;
        }

        var getResponse = await _httpClient.GetAsync(url);
        var getResult = await getResponse.Content.ReadAsStringAsync();
        getResponse.EnsureSuccessStatusCode();
        return getResult;
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
