namespace CodeMaster.Core.Services.Monitor;

/// <summary>
/// 通知服务接口
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 发送通知给指定用户
    /// </summary>
    Task SendNotificationAsync(long userId, string message);

    /// <summary>
    /// 发送通知给所有用户
    /// </summary>
    Task SendNotificationToAllAsync(string message);

    /// <summary>
    /// 发送通知给指定租户的所有在线用户
    /// </summary>
    Task SendNotificationToTenantAsync(long tenantId, string message);
}
