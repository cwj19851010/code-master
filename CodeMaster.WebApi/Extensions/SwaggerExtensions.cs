using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace CodeMaster.WebApi.Extensions;

/// <summary>
/// Swagger 扩展方法
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// 为没有 HttpMethod 特性的 Action 自动设置 HTTP Method
    /// 解决 Swagger 报错：Ambiguous HTTP method for action
    /// </summary>
    public static void AutoHttpMethodIfActionNoBind(this IApplicationBuilder app, string defaultHttpMethod = "POST")
    {
        // 从容器中获取 IApiDescriptionGroupCollectionProvider 实例
        var apiDescriptionGroupCollectionProvider = app.ApplicationServices
            .GetRequiredService<IApiDescriptionGroupCollectionProvider>();

        var apiDescriptionGroupsItems = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items;

        // 遍历 ApiDescriptionGroups
        foreach (var apiDescriptionGroup in apiDescriptionGroupsItems)
        {
            foreach (var apiDescription in apiDescriptionGroup.Items)
            {
                // 如果 HttpMethod 为空，根据 Action 名称推断
                if (string.IsNullOrEmpty(apiDescription.HttpMethod))
                {
                    // 获取 Action 名称
                    var actionName = apiDescription.ActionDescriptor.RouteValues.TryGetValue("action", out var action)
                        ? action
                        : apiDescription.ActionDescriptor.DisplayName;

                    if (string.IsNullOrEmpty(actionName))
                        continue;

                    // 根据 Action 开头单词给定 HttpMethod 默认值
                    string methodName = defaultHttpMethod;

                    if (actionName.StartsWith("get", StringComparison.OrdinalIgnoreCase) ||
                        actionName.StartsWith("query", StringComparison.OrdinalIgnoreCase))
                    {
                        methodName = "GET";
                    }
                    else if (actionName.StartsWith("post", StringComparison.OrdinalIgnoreCase) ||
                             actionName.StartsWith("create", StringComparison.OrdinalIgnoreCase) ||
                             actionName.StartsWith("add", StringComparison.OrdinalIgnoreCase) ||
                             actionName.StartsWith("insert", StringComparison.OrdinalIgnoreCase))
                    {
                        methodName = "POST";
                    }
                    else if (actionName.StartsWith("put", StringComparison.OrdinalIgnoreCase) ||
                             actionName.StartsWith("update", StringComparison.OrdinalIgnoreCase) ||
                             actionName.StartsWith("edit", StringComparison.OrdinalIgnoreCase) ||
                             actionName.StartsWith("modify", StringComparison.OrdinalIgnoreCase))
                    {
                        methodName = "PUT";
                    }
                    else if (actionName.StartsWith("delete", StringComparison.OrdinalIgnoreCase) ||
                             actionName.StartsWith("remove", StringComparison.OrdinalIgnoreCase))
                    {
                        methodName = "DELETE";
                    }

                    apiDescription.HttpMethod = methodName;

                    Console.WriteLine($"[Swagger] Auto set HttpMethod: {actionName} -> {methodName}");
                }
            }
        }
    }
}
