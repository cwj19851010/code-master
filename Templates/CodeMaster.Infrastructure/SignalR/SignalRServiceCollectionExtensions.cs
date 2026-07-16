using Microsoft.Extensions.DependencyInjection;

namespace CodeMaster.Infrastructure.SignalR;

/// <summary>
/// SignalR 服务注册扩展
/// </summary>
public static class SignalRServiceCollectionExtensions
{
    /// <summary>
    /// 添加 SignalR 通知服务
    /// </summary>
    public static IServiceCollection AddSignalRNotification(this IServiceCollection services)
    {
        // 注册 SignalR
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });

        // 注册在线用户管理器（单例）
        services.AddSingleton<IOnlineUserManager, OnlineUserManager>();

        return services;
    }
}
