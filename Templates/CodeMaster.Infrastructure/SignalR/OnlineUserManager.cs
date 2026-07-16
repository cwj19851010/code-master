using System.Collections.Concurrent;

namespace CodeMaster.Infrastructure.SignalR;

/// <summary>
/// 在线用户管理器实现（基于内存）
/// </summary>
public class OnlineUserManager : IOnlineUserManager
{
    // ConnectionId -> OnlineUserInfo
    private readonly ConcurrentDictionary<string, OnlineUserInfo> _connectionUsers = new();

    // UserId -> ConnectionId
    private readonly ConcurrentDictionary<long, string> _userConnections = new();

    /// <summary>
    /// 添加在线用户
    /// </summary>
    public Task AddUserAsync(string connectionId, long userId, string userName)
    {
        var now = DateTime.UtcNow;
        var userInfo = new OnlineUserInfo
        {
            UserId = userId,
            UserName = userName,
            ConnectionId = connectionId,
            ConnectedAt = now,
            LastActiveTime = now
        };

        _connectionUsers[connectionId] = userInfo;
        _userConnections[userId] = connectionId;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 移除在线用户
    /// </summary>
    public Task RemoveUserAsync(string connectionId)
    {
        if (_connectionUsers.TryRemove(connectionId, out var userInfo))
        {
            _userConnections.TryRemove(userInfo.UserId, out _);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取用户的连接ID
    /// </summary>
    public string? GetConnectionId(long userId)
    {
        return _userConnections.TryGetValue(userId, out var connectionId) ? connectionId : null;
    }

    /// <summary>
    /// 获取所有在线用户
    /// </summary>
    public Task<List<OnlineUserInfo>> GetOnlineUsersAsync()
    {
        return Task.FromResult(_connectionUsers.Values.ToList());
    }

    /// <summary>
    /// 检查用户是否在线
    /// </summary>
    public bool IsUserOnline(long userId)
    {
        return _userConnections.ContainsKey(userId);
    }

    /// <summary>
    /// 获取在线用户数量
    /// </summary>
    public int GetOnlineUserCount()
    {
        return _connectionUsers.Count;
    }
}
