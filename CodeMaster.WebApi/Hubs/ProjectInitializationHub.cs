using Microsoft.AspNetCore.SignalR;

namespace CodeMaster.WebApi.Hubs;

/// <summary>
/// 项目初始化进度推送 Hub
/// </summary>
public class ProjectInitializationHub : Hub
{
    /// <summary>
    /// 发送初始化进度消息
    /// </summary>
    public async Task SendProgress(string projectId, string step, string message, int progress)
    {
        await Clients.All.SendAsync("ReceiveProgress", projectId, step, message, progress);
    }
}
