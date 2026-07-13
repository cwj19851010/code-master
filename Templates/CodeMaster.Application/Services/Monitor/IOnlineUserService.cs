using CodeMaster.Application.Dtos.Monitor;
using CodeMaster.Core.Services;

namespace CodeMaster.Application.Services.Monitor;

/// <summary>
/// 在线用户服务接口
/// </summary>
public interface IOnlineUserService : IApplicationService
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
    /// 获取在线用户列表
    /// </summary>
    Task<List<OnlineUserDto>> GetOnlineUsersAsync();

    /// <summary>
    /// 强制用户下线
    /// </summary>
    Task<bool> ForceOfflineAsync(long userId);

    /// <summary>
    /// 获取用户的连接ID
    /// </summary>
    string? GetConnectionId(long userId);
}
