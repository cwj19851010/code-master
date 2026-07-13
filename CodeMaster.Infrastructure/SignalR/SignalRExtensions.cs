using Microsoft.Extensions.DependencyInjection;

namespace CodeMaster.Infrastructure.SignalR;

/// <summary>
/// SignalR 服务注册扩展
/// </summary>
public static class SignalRExtensions
{
    /// <summary>
    /// 添加 SignalR 服务
    /// </summary>
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        // 配置 SignalR
        services.AddSignalR();

        Console.WriteLine("[AddSignalRServices] SignalR 服务已注册");

        return services;
    }
}
