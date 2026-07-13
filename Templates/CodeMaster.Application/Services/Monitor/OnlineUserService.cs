using CodeMaster.Application.Dtos.Monitor;
using CodeMaster.Infrastructure.SignalR;
using Mapster;
using Microsoft.AspNetCore.SignalR;

namespace CodeMaster.Application.Services.Monitor;

/// <summary>
/// 在线用户服务实现
/// </summary>
public class OnlineUserService : IOnlineUserService
{
    private readonly IOnlineUserManager _onlineUserManager;
    private readonly IHubContext<NotificationHub> _hubContext;

    public OnlineUserService(
        IOnlineUserManager onlineUserManager,
        IHubContext<NotificationHub> hubContext)
    {
        _onlineUserManager = onlineUserManager;
        _hubContext = hubContext;
    }

    public Task AddOnlineUserAsync(string connectionId, long userId, string userName)
    {
        return _onlineUserManager.AddUserAsync(connectionId, userId, userName);
    }

    public Task RemoveOnlineUserAsync(string connectionId)
    {
        return _onlineUserManager.RemoveUserAsync(connectionId);
    }

    public async Task<List<OnlineUserDto>> GetOnlineUsersAsync()
    {
        var users = await _onlineUserManager.GetOnlineUsersAsync();
        return users.Adapt<List<OnlineUserDto>>();
    }

    public async Task<bool> ForceOfflineAsync(long userId)
    {
        var connectionId = _onlineUserManager.GetConnectionId(userId);
        if (!string.IsNullOrEmpty(connectionId))
        {
            // 直接使用 HubContext 发送强制下线消息
            await _hubContext.Clients.Client(connectionId).SendAsync("ForceOffline", new
            {
                Message = "您已被管理员强制下线",
                Timestamp = DateTime.UtcNow
            });

            // 从在线用户列表中移除
            await _onlineUserManager.RemoveUserAsync(connectionId);
            return true;
        }
        return false;
    }

    public string? GetConnectionId(long userId)
    {
        return _onlineUserManager.GetConnectionId(userId);
    }
}
