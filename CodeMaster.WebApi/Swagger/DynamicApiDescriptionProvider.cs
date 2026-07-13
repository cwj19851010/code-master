using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using CodeMaster.Core.Services;
using System.Reflection;

namespace CodeMaster.WebApi.Swagger;

/// <summary>
/// 动态 API 描述提供器
/// 确保动态生成的 API 有正确的 HTTP Method 信息
/// </summary>
public class DynamicApiDescriptionProvider : IApiDescriptionProvider
{
    // 在默认提供器之后执行
    public int Order => -900;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        // 不需要在执行前做任何事
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
        foreach (var apiDescription in context.Results)
        {
            var actionDescriptor = apiDescription.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
                continue;

            // 检查是否是 Application Service
            var serviceInterface = actionDescriptor.ControllerTypeInfo
                .GetInterfaces()
                .FirstOrDefault(i => typeof(IApplicationService).IsAssignableFrom(i) && i != typeof(IApplicationService));

            if (serviceInterface == null)
                continue;

            // 如果 HTTP Method 为空，从方法名推断并设置
            if (string.IsNullOrEmpty(apiDescription.HttpMethod))
            {
                var method = serviceInterface.GetMethod(actionDescriptor.MethodInfo.Name);
                if (method != null)
                {
                    var httpMethod = InferHttpMethod(method);

                    // 使用反射设置 HttpMethod（因为它是只读属性）
                    var httpMethodProperty = typeof(ApiDescription).GetProperty(nameof(ApiDescription.HttpMethod));
                    if (httpMethodProperty != null && httpMethodProperty.CanWrite)
                    {
                        httpMethodProperty.SetValue(apiDescription, httpMethod);
                    }
                }
            }
        }
    }

    private string InferHttpMethod(MethodInfo method)
    {
        var methodName = method.Name;

        // 去掉 Async 后缀
        if (methodName.EndsWith("Async"))
            methodName = methodName.Substring(0, methodName.Length - 5);

        // 根据方法名推断 HTTP Method
        if (methodName.StartsWith("Get") || methodName.StartsWith("Query") ||
            methodName.StartsWith("Find") || methodName.StartsWith("Search") ||
            methodName.StartsWith("List"))
            return "GET";

        if (methodName.StartsWith("Create") || methodName.StartsWith("Add") ||
            methodName.StartsWith("Insert") || methodName.StartsWith("Post"))
            return "POST";

        if (methodName.StartsWith("Update") || methodName.StartsWith("Modify") ||
            methodName.StartsWith("Edit") || methodName.StartsWith("Put"))
            return "PUT";

        if (methodName.StartsWith("Delete") || methodName.StartsWith("Remove"))
            return "DELETE";

        // 默认使用 POST
        return "POST";
    }
}
