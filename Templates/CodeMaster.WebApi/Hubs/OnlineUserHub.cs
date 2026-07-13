using CodeMaster.Application.Services.Monitor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CodeMaster.WebApi.Hubs;

/// <summary>
/// 在线用户Hub
/// </summary>
[Authorize]
public class OnlineUserHub : Hub
{
    private readonly IOnlineUserService _onlineUserService;

    public OnlineUserHub(IOnlineUserService onlineUserService)
    {
        _onlineUserService = onlineUserService;
    }

    /// <summary>
    /// 连接时触发
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await _onlineUserService.AddOnlineUserAsync(
                Context.ConnectionId,
                long.Parse(userId),
                userName ?? "未知用户"
            );

            // 通知所有客户端有新用户上线
            await Clients.All.SendAsync("UserOnline", userId, userName);
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

        await _onlineUserService.RemoveOnlineUserAsync(Context.ConnectionId);

        // 通知所有客户端有用户下线
        if (!string.IsNullOrEmpty(userId))
        {
            await Clients.All.SendAsync("UserOffline", userId, userName);
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
