using CodeMaster.Application.Dtos.Monitor;

namespace CodeMaster.Application.Services.Monitor;

/// <summary>
/// 在线用户服务接口
/// </summary>
public interface IOnlineUserService
{
    /// <summary>
    /// 添加在线用户
    /// </summary>
    Task AddOnlineUserAsync(string connectionId, long userId, string userName);

    /// <summary>
    /// 移除在线用户
    /// </summary>
    Task RemoveOnlineUserAsync(string connectionId);

    /// <summary>
    /// 获取所有在线用户
    /// </summary>
    Task<List<OnlineUserDto>> GetOnlineUsersAsync();

    /// <summary>
    /// 强制用户下线
    /// </summary>
    Task<bool> ForceOfflineAsync(long userId);

    /// <summary>
    /// 根据用户ID获取连接ID
    /// </summary>
    string? GetConnectionId(long userId);
}
