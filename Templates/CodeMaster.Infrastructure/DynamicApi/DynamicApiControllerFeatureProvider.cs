using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace CodeMaster.Infrastructure.DynamicApi;

/// <summary>
/// 动态 API Controller Feature Provider
/// 自动将 Application Service 注册为 Controller
/// </summary>
public class DynamicApiControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly Assembly _applicationAssembly;

    public DynamicApiControllerFeatureProvider(Assembly applicationAssembly)
    {
        _applicationAssembly = applicationAssembly;
    }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        // 扫描 Application 程序集中的所有 Service
        var serviceTypes = _applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && IsApplicationService(t))
            .ToList();

        foreach (var serviceType in serviceTypes)
        {
            // 将 Service 注册为 Controller
            if (!feature.Controllers.Contains(serviceType.GetTypeInfo()))
            {
                feature.Controllers.Add(serviceType.GetTypeInfo());
            }
        }
    }

    private bool IsApplicationService(Type type)
    {
        // 检查是否实现了 IApplicationService 接口
        return typeof(Core.Services.IApplicationService).IsAssignableFrom(type);
    }
}
