using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CodeMaster.Infrastructure.SignalR;

/// <summary>
/// 通知Hub（统一的SignalR Hub）
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly IOnlineUserManager _onlineUserManager;

    public NotificationHub(IOnlineUserManager onlineUserManager)
    {
        _onlineUserManager = onlineUserManager;
    }

    /// <summary>
    /// 连接时触发
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out var userIdLong))
        {
            await _onlineUserManager.AddUserAsync(
                Context.ConnectionId,
                userIdLong,
                userName ?? "未知用户"
            );

            // 通知所有客户端有新用户上线
            await Clients.All.SendAsync("UserOnline", new
            {
                UserId = userIdLong,
                UserName = userName,
                ConnectedAt = DateTime.UtcNow
            });
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 断开连接时触发
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        await _onlineUserManager.RemoveUserAsync(Context.ConnectionId);

        // 通知所有客户端有用户下线
        if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out var userIdLong))
        {
            await Clients.All.SendAsync("UserOffline", new
            {
                UserId = userIdLong,
                UserName = userName,
                DisconnectedAt = DateTime.UtcNow
            });
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 加入组（客户端调用）
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// 离开组（客户端调用）
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
