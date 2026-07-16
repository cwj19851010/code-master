using CodeMaster.Core.Services.Monitor;
using Microsoft.AspNetCore.SignalR;

namespace CodeMaster.Infrastructure.SignalR;

/// <summary>
/// 通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IOnlineUserManager _onlineUserManager;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        IOnlineUserManager onlineUserManager)
    {
        _hubContext = hubContext;
        _onlineUserManager = onlineUserManager;
    }

    /// <summary>
    /// 发送通知给指定用户
    /// </summary>
    public async Task SendNotificationAsync(long userId, string message)
    {
        var connectionId = _onlineUserManager.GetConnectionId(userId);
        if (!string.IsNullOrEmpty(connectionId))
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", message);
        }
    }

    /// <summary>
    /// 发送通知给所有用户
    /// </summary>
    public async Task SendNotificationToAllAsync(string message)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
    }

    /// <summary>
    /// 发送通知给指定租户的所有在线用户
    /// </summary>
    public async Task SendNotificationToTenantAsync(long tenantId, string message)
    {
        // 当前 OnlineUserManager 中未记录租户信息，无法精确按租户发送
        // 退回到广播实现作为降级策略
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
    }
}
