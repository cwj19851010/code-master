using System.Reflection;
using CodeMaster.Core.DynamicApi;
using Microsoft.Extensions.DependencyInjection;

namespace CodeMaster.Infrastructure.DynamicApi;

/// <summary>
/// 动态 API 扩展方法
/// </summary>
public static class DynamicApiExtensions
{
    /// <summary>
    /// 添加动态 API 支持
    /// </summary>
    public static IMvcBuilder AddDynamicApi(this IMvcBuilder mvcBuilder, Assembly applicationAssembly, Action<DynamicApiOptions>? configure = null)
    {
        // 配置选项
        var services = mvcBuilder.Services;
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<DynamicApiOptions>(options => { });
        }

        // 添加 Feature Provider
        mvcBuilder.ConfigureApplicationPartManager(manager =>
        {
            manager.FeatureProviders.Add(new DynamicApiControllerFeatureProvider(applicationAssembly));
        });

        // 添加约定
        mvcBuilder.AddMvcOptions(options =>
        {
            var serviceProvider = services.BuildServiceProvider();
            var convention = new DynamicApiControllerConvention(
                serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DynamicApiOptions>>());
            options.Conventions.Add(convention);
        });

        return mvcBuilder;
    }

    /// <summary>
    /// 自动注册 Application Service
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, Assembly applicationAssembly)
    {
        // 扫描所有实现了 IApplicationService 的类
        var serviceTypes = applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(Core.Services.IApplicationService).IsAssignableFrom(t))
            .ToList();

        Console.WriteLine($"[AddApplicationServices] 扫描到 {serviceTypes.Count} 个服务:");

        foreach (var serviceType in serviceTypes)
        {
            // 查找对应的接口（优先查找直接实现的接口，且继承自 IApplicationService）
            var interfaceType = serviceType.GetInterfaces()
                .FirstOrDefault(i =>
                    i.Name == $"I{serviceType.Name}" &&
                    i.GetInterfaces().Any(ii => ii.Name == "IApplicationService"));

            if (interfaceType != null)
            {
                // 注册接口和实现
                services.AddScoped(interfaceType, serviceType);
                Console.WriteLine($"  - 注册服务: {interfaceType.Name} -> {serviceType.Name}");
            }
            else
            {
                Console.WriteLine($"  - 未找到接口: {serviceType.Name}，接口列表: {string.Join(", ", serviceType.GetInterfaces().Select(i => i.Name))}");
            }

            // 也注册实现类本身（用于动态 API）
            services.AddScoped(serviceType);
        }

        return services;
    }

    /// <summary>
    /// 自动注册仓储
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // 注册泛型仓储
        services.AddScoped(typeof(Core.Repositories.IReadOnlyRepository<>), typeof(Infrastructure.Persistence.Repositories.ReadOnlyRepository<>));
        services.AddScoped(typeof(Core.Repositories.IRepository<>), typeof(Infrastructure.Persistence.Repositories.Repository<>));

        Console.WriteLine("[AddRepositories] 已注册泛型仓储:");
        Console.WriteLine("  - IReadOnlyRepository<> -> ReadOnlyRepository<>");
        Console.WriteLine("  - IRepository<> -> Repository<>");

        return services;
    }
}
