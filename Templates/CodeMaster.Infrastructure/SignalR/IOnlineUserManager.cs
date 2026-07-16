namespace CodeMaster.Infrastructure.SignalR;

/// <summary>
/// 在线用户管理器接口
/// </summary>
public interface IOnlineUserManager
{
    /// <summary>
    /// 添加在线用户
    /// </summary>
    Task AddUserAsync(string connectionId, long userId, string userName);

    /// <summary>
    /// 移除在线用户
    /// </summary>
    Task RemoveUserAsync(string connectionId);

    /// <summary>
    /// 获取用户的连接ID
    /// </summary>
    string? GetConnectionId(long userId);

    /// <summary>
    /// 获取所有在线用户
    /// </summary>
    Task<List<OnlineUserInfo>> GetOnlineUsersAsync();

    /// <summary>
    /// 检查用户是否在线
    /// </summary>
    bool IsUserOnline(long userId);

    /// <summary>
    /// 获取在线用户数量
    /// </summary>
    int GetOnlineUserCount();
}

/// <summary>
/// 在线用户信息
/// </summary>
public class OnlineUserInfo
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActiveTime { get; set; }
}
