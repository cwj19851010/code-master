using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using CodeMaster.Core.DynamicApi;
using CodeMaster.Core.Authorization;

namespace CodeMaster.Infrastructure.DynamicApi;

/// <summary>
/// 动态 API 约定
/// 自动将 Application Service 转换为 API Controller
/// </summary>
public class DynamicApiControllerConvention : IApplicationModelConvention
{
    private readonly DynamicApiOptions _options;

    public DynamicApiControllerConvention(IOptions<DynamicApiOptions> options)
    {
        _options = options.Value;
    }

    public void Apply(ApplicationModel application)
    {
        if (!_options.IsEnabled)
            return;

        foreach (var controller in application.Controllers)
        {
            var type = controller.ControllerType.AsType();

            // 检查是否是 Application Service
            if (!IsApplicationService(type))
                continue;

            // 配置控制器
            ConfigureController(controller);

            // 配置 Actions
            var actionsToRemove = new List<ActionModel>();
            foreach (var action in controller.Actions)
            {
                if (!ConfigureAction(action))
                {
                    actionsToRemove.Add(action);
                }
                else
                {
                    // 配置参数绑定
                    ConfigureParameters(action);
                }
            }

            // 移除被标记为不生成 API 的 Action
            foreach (var action in actionsToRemove)
            {
                controller.Actions.Remove(action);
            }
        }
    }

    private bool IsApplicationService(Type type)
    {
        // 检查是否实现了 IApplicationService 接口
        if (!typeof(Core.Services.IApplicationService).IsAssignableFrom(type))
            return false;

        // 检查类级别的 [NonController] 特性（ASP.NET Core 自带）
        if (type.GetCustomAttribute<NonControllerAttribute>() != null)
            return false;

        return true;
    }

    private void ConfigureController(ControllerModel controller)
    {
        var type = controller.ControllerType.AsType();

        // 检查类级别的 [NonController] 特性（ASP.NET Core 自带）
        if (type.GetCustomAttribute<NonControllerAttribute>() != null)
        {
            controller.Actions.Clear();
            return;
        }

        var controllerName = GetControllerName(controller.ControllerType.Name);
        var moduleName = GetModuleName(controller.ControllerType.Namespace);

        // 设置路由：/api/{module}/{controller}
        var route = $"{_options.RoutePrefix}/{moduleName}/{controllerName}";

        if (_options.UseLowercaseRoutes)
            route = route.ToLowerInvariant();

        controller.Selectors.Clear();
        controller.Selectors.Add(new SelectorModel
        {
            AttributeRouteModel = new AttributeRouteModel
            {
                Template = route
            }
        });
    }

    private bool ConfigureAction(ActionModel action)
    {
        var method = action.ActionMethod;

        // 检查是否标记了 [NonAction] 特性（ASP.NET Core 自带）
        if (method.GetCustomAttribute<NonActionAttribute>() != null)
        {
            return false;
        }

        // 检查 DynamicApi 特性
        var dynamicApiAttr = method.GetCustomAttribute<DynamicApiAttribute>();
        if (dynamicApiAttr != null && !dynamicApiAttr.IsEnabled)
        {
            return false;
        }

        // 1. 检查是否已有 MVC 的 HTTP Method 特性（HttpGet、HttpPost 等）
        var existingHttpMethodAttr = method.GetCustomAttributes()
            .FirstOrDefault(a => a is HttpGetAttribute || a is HttpPostAttribute ||
                                 a is HttpPutAttribute || a is HttpDeleteAttribute ||
                                 a is HttpPatchAttribute);

        // 如果方法上已经有 HTTP Method 特性，完全跳过，不做任何修改
        // 让 ASP.NET Core 和 Swagger 使用原始的特性信息
        if (existingHttpMethodAttr != null)
        {
            Console.WriteLine($"[DynamicApi] {action.Controller.ControllerName}.{method.Name} -> 跳过（已有 HTTP Method 特性）");
            return true;
        }

        // 2. 如果没有 HTTP Method 特性，使用动态 API 约定
        var route = dynamicApiAttr?.Route ?? GetActionRoute(method);
        var httpMethod = dynamicApiAttr?.HttpMethod ?? GetHttpVerb(method);

        // 关键修复：清空所有现有的 Selectors，避免产生多个 Action 描述符
        action.Selectors.Clear();

        // 根据 HTTP Method 创建对应的路由特性
        HttpMethodAttribute httpMethodAttribute = httpMethod.ToUpper() switch
        {
            "GET" => new HttpGetAttribute(route),
            "POST" => new HttpPostAttribute(route),
            "PUT" => new HttpPutAttribute(route),
            "DELETE" => new HttpDeleteAttribute(route),
            "PATCH" => new HttpPatchAttribute(route),
            _ => new HttpPostAttribute(route)
        };

        // 设置路由顺序：有参数的路由优先级更低（Order 更大）
        // 这样 getpagedlist 会优先于 getbyid/{id} 匹配
        var parameterCount = route.Count(c => c == '{');
        httpMethodAttribute.Order = parameterCount;

        // 创建唯一的 selector
        var selector = new SelectorModel
        {
            AttributeRouteModel = new AttributeRouteModel(httpMethodAttribute)
        };

        // 添加 EndpointMetadata，让 Swagger 能够识别 HTTP Method
        selector.EndpointMetadata.Add(httpMethodAttribute);

        // 关键修复：只添加一个 HttpMethodMetadata，避免重复
        var httpMethodMetadata = new Microsoft.AspNetCore.Routing.HttpMethodMetadata(
            new[] { httpMethod.ToUpper() });
        selector.EndpointMetadata.Add(httpMethodMetadata);

        // 只添加一个 selector
        action.Selectors.Add(selector);

        Console.WriteLine($"[DynamicApi] {action.Controller.ControllerName}.{method.Name} -> {httpMethod} {route}");

        // 3. 处理权限特性
        ConfigurePermission(action, method);

        return true;
    }

    private void ConfigureParameters(ActionModel action)
    {
        // 获取 HTTP Method
        var method = action.ActionMethod;
        var verb = GetHttpVerb(method);

        foreach (var parameter in action.Parameters)
        {
            var parameterInfo = parameter.ParameterInfo;
            var parameterType = parameterInfo.ParameterType;

            // 检查参数上是否有绑定特性
            var fromBodyAttr = parameterInfo.GetCustomAttribute<FromBodyAttribute>();
            var fromQueryAttr = parameterInfo.GetCustomAttribute<FromQueryAttribute>();
            var fromRouteAttr = parameterInfo.GetCustomAttribute<FromRouteAttribute>();
            var fromFormAttr = parameterInfo.GetCustomAttribute<FromFormAttribute>();
            var fromHeaderAttr = parameterInfo.GetCustomAttribute<FromHeaderAttribute>();

            // 如果已有绑定特性，使用特性指定的绑定源
            if (fromBodyAttr != null)
            {
                parameter.BindingInfo = Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo.GetBindingInfo(new[] { fromBodyAttr });
                continue;
            }
            if (fromQueryAttr != null)
            {
                parameter.BindingInfo = Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo.GetBindingInfo(new[] { fromQueryAttr });
                continue;
            }
            if (fromRouteAttr != null)
            {
                parameter.BindingInfo = Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo.GetBindingInfo(new[] { fromRouteAttr });
                continue;
            }
            if (fromFormAttr != null)
            {
                parameter.BindingInfo = Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo.GetBindingInfo(new[] { fromFormAttr });
                continue;
            }
            if (fromHeaderAttr != null)
            {
                parameter.BindingInfo = Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo.GetBindingInfo(new[] { fromHeaderAttr });
                continue;
            }

            // 如果参数是复杂类型（非简单类型）
            if (IsComplexType(parameterType))
            {
                // POST/PUT/PATCH 请求：复杂对象从 Body 绑定
                if (verb == "POST" || verb == "PUT" || verb == "PATCH")
                {
                    Console.WriteLine($"[DynamicApi] 为 {action.Controller.ControllerName}.{method.Name}.{parameter.Name} 添加 FromBody 绑定");
                    parameter.BindingInfo = Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
                }
                // GET/DELETE 请求：复杂对象从 Query 绑定
                // ASP.NET Core 会自动将查询字符串的键值对映射到对象的属性
                else if (verb == "GET" || verb == "DELETE")
                {
                    Console.WriteLine($"[DynamicApi] 为 {action.Controller.ControllerName}.{method.Name}.{parameter.Name} 添加 FromQuery 绑定");
                    parameter.BindingInfo = Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo.GetBindingInfo(new[] { new FromQueryAttribute() });
                }
            }
            else
            {
                // 简单类型参数的处理
                // 如果参数有默认值或者是 string 类型，不添加到路由中，而是从 Query 绑定
                if (parameterInfo.HasDefaultValue || parameterType == typeof(string))
                {
                    // POST/PUT/PATCH 请求：从 Query 绑定（避免 Body 只能有一个参数的限制）
                    // GET/DELETE 请求：从 Query 绑定
                    parameter.BindingInfo = Microsoft.AspNetCore.Mvc.ModelBinding.BindingInfo.GetBindingInfo(new[] { new FromQueryAttribute() });
                }
                // 否则，简单类型参数已经添加到路由中，从 Route 绑定（默认行为）
            }
        }
    }

    private bool IsComplexType(Type type)
    {
        // 简单类型：基本类型、字符串、DateTime、Guid 等
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) ||
            type == typeof(decimal) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
            type == typeof(Guid))
        {
            return false;
        }

        // 可空类型
        if (Nullable.GetUnderlyingType(type) != null)
        {
            return IsComplexType(Nullable.GetUnderlyingType(type)!);
        }

        // 其他都是复杂类型
        return true;
    }

    private string GetControllerName(string typeName)
    {
        var name = typeName;

        // 移除 Service 后缀
        if (_options.RemoveServiceSuffix && name.EndsWith("Service"))
            name = name.Substring(0, name.Length - 7);

        // 移除 AppService 后缀
        if (_options.RemoveAppServiceSuffix && name.EndsWith("AppService"))
            name = name.Substring(0, name.Length - 10);

        // 移除 I 前缀（接口）
        if (name.StartsWith("I") && name.Length > 1 && char.IsUpper(name[1]))
            name = name.Substring(1);

        // 移除 Sys 前缀（统一路由风格）
        if (name.StartsWith("Sys") && name.Length > 3 && char.IsUpper(name[3]))
            name = name.Substring(3);

        return name;
    }

    private string GetModuleName(string? namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName))
            return "app";

        // CodeMaster.Application.Services.System -> system
        var parts = namespaceName.Split('.');
        if (parts.Length >= 4)
            return parts[3];

        return "app";
    }

    private string GetHttpVerb(MethodInfo method)
    {
        var methodName = method.Name;

        // 根据方法名推断 HTTP 动词
        if (methodName.StartsWith("Get"))
            return "GET";
        if (methodName.StartsWith("Create") || methodName.StartsWith("Add") || methodName.StartsWith("Insert"))
            return "POST";
        if (methodName.StartsWith("Update") || methodName.StartsWith("Edit") || methodName.StartsWith("Modify"))
            return "PUT";
        if (methodName.StartsWith("Delete") || methodName.StartsWith("Remove"))
            return "DELETE";

        // 默认 POST
        return "POST";
    }

    private string GetActionRoute(MethodInfo method)
    {
        var methodName = method.Name;

        // 只去掉 Async 后缀，保留其他关键字（Get、Create、Update、Delete 等）
        if (methodName.EndsWith("Async"))
        {
            methodName = methodName.Substring(0, methodName.Length - 5);
        }

        // 去掉 Async 后原样输出（小写）
        var route = _options.UseLowercaseRoutes ? methodName.ToLowerInvariant() : methodName;

        // 为简单类型参数添加路由参数
        var parameters = method.GetParameters();
        foreach (var param in parameters)
        {
            // 只为简单类型添加路由参数（id、code等）
            if (!IsComplexType(param.ParameterType))
            {
                // 检查参数是否有 FromRoute、FromQuery、FromBody 等特性
                var hasBindingAttr = param.GetCustomAttribute<FromRouteAttribute>() != null ||
                                    param.GetCustomAttribute<FromQueryAttribute>() != null ||
                                    param.GetCustomAttribute<FromBodyAttribute>() != null ||
                                    param.GetCustomAttribute<FromFormAttribute>() != null ||
                                    param.GetCustomAttribute<FromHeaderAttribute>() != null;

                // 检查参数是否有默认值
                var hasDefaultValue = param.HasDefaultValue;

                // 检查参数类型是否是 string（string 类型可空，容易产生歧义）
                var isStringType = param.ParameterType == typeof(string);

                // 如果没有绑定特性，且没有默认值，且不是 string 类型，才添加到路由中
                // string 类型和有默认值的参数会导致 Swagger 产生歧义，应该从 Query 或 Body 绑定
                if (!hasBindingAttr && !hasDefaultValue && !isStringType)
                {
                    route += $"/{{{param.Name}}}";
                }
            }
        }

        return route;
    }

    /// <summary>
    /// 配置权限
    /// </summary>
    private void ConfigurePermission(ActionModel action, MethodInfo method)
    {
        var controllerType = action.Controller.ControllerType.AsType();

        // 1. 检查方法级别的 AllowAnonymous
        var allowAnonymousAttr = method.GetCustomAttribute<AllowAnonymousAttribute>();
        if (allowAnonymousAttr != null)
        {
            if (!action.Filters.Any(f => f is Microsoft.AspNetCore.Mvc.Authorization.AllowAnonymousFilter))
            {
                action.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AllowAnonymousFilter());
            }
            return;
        }

        // 2. 检查类级别的 AllowAnonymous
        var classAllowAnonymousAttr = controllerType.GetCustomAttribute<AllowAnonymousAttribute>();
        if (classAllowAnonymousAttr != null)
        {
            // 类级别允许匿名，方法没有任何特性，则允许匿名
            var methodPermissionAttr = method.GetCustomAttribute<PermissionAttribute>();
            var methodAuthorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();

            if (methodPermissionAttr == null && methodAuthorizeAttr == null)
            {
                if (!action.Filters.Any(f => f is Microsoft.AspNetCore.Mvc.Authorization.AllowAnonymousFilter))
                {
                    action.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AllowAnonymousFilter());
                }
                return;
            }
        }

        // 3. 检查方法级别的 Permission 特性
        var permissionAttr = method.GetCustomAttribute<PermissionAttribute>();
        if (permissionAttr != null)
        {
            var policy = new AuthorizeAttribute { Policy = $"Permission:{permissionAttr.PermissionCode}" };
            var authorizeData = new[] { policy };
            action.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(authorizeData));
            return;
        }

        // 4. 检查方法级别的 Authorize 特性
        var authorizeAttr = method.GetCustomAttribute<AuthorizeAttribute>();
        if (authorizeAttr != null)
        {
            if (!action.Filters.Any(f => f is Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter))
            {
                var authorizeData = new[] { authorizeAttr };
                action.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(authorizeData));
            }
            return;
        }

        // 5. 检查类级别的 DynamicApiPermission 特性
        var dynamicApiPermissionAttr = controllerType.GetCustomAttribute<DynamicApiPermissionAttribute>();

        // 如果没有特性，或者 RequirePermission = true，则生成默认权限
        if (dynamicApiPermissionAttr == null || dynamicApiPermissionAttr.RequirePermission)
        {
            // 生成默认权限代码
            var permissionCode = GenerateDefaultPermission(controllerType, method);
            var policy = new AuthorizeAttribute { Policy = $"Permission:{permissionCode}" };
            var authorizeData = new[] { policy };
            action.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(authorizeData));
        }
        else
        {
            // RequirePermission = false，只需要登录即可访问
            if (!action.Filters.Any(f => f is Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter))
            {
                var authorizeData = new[] { new AuthorizeAttribute() };
                action.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(authorizeData));
            }
        }
    }

    /// <summary>
    /// 生成默认权限代码
    /// 规则：{module}:{controller}:{action}
    /// </summary>
    private string GenerateDefaultPermission(Type controllerType, MethodInfo method)
    {
        // 获取模块名（命名空间第4部分，小写）
        var moduleName = GetModuleName(controllerType.Namespace);
        if (_options.UseLowercaseRoutes)
            moduleName = moduleName.ToLowerInvariant();

        // 获取控制器名（去除 Sys 前缀和 Service 后缀，小写）
        var controllerName = GetControllerName(controllerType.Name);
        if (_options.UseLowercaseRoutes)
            controllerName = controllerName.ToLowerInvariant();

        // 获取操作名（根据方法名推断）
        var actionName = GetPermissionAction(method);

        return $"{moduleName}:{controllerName}:{actionName}";
    }

    /// <summary>
    /// 根据方法名推断权限操作
    /// </summary>
    private string GetPermissionAction(MethodInfo method)
    {
        var methodName = method.Name;

        // 去除 Async 后缀
        if (methodName.EndsWith("Async"))
            methodName = methodName.Substring(0, methodName.Length - 5);

        // 特殊方法名映射
        if (methodName.Equals("GetById", StringComparison.OrdinalIgnoreCase) ||
            methodName.Equals("GetByIdAsync", StringComparison.OrdinalIgnoreCase))
            return "view";

        if (methodName.Equals("GetList", StringComparison.OrdinalIgnoreCase) ||
            methodName.Equals("GetPagedList", StringComparison.OrdinalIgnoreCase) ||
            methodName.Equals("GetListAsync", StringComparison.OrdinalIgnoreCase) ||
            methodName.Equals("GetPagedListAsync", StringComparison.OrdinalIgnoreCase))
            return "list";

        if (methodName.Equals("Create", StringComparison.OrdinalIgnoreCase) ||
            methodName.Equals("CreateAsync", StringComparison.OrdinalIgnoreCase))
            return "create";

        if (methodName.Equals("Update", StringComparison.OrdinalIgnoreCase) ||
            methodName.Equals("UpdateAsync", StringComparison.OrdinalIgnoreCase))
            return "update";

        if (methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
            return "delete";

        // 根据方法名开头推断
        if (methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
            return "get";

        if (methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ||
            methodName.StartsWith("Edit", StringComparison.OrdinalIgnoreCase) ||
            methodName.StartsWith("Modify", StringComparison.OrdinalIgnoreCase))
            return "update";

        // 默认返回 post
        return "post";
    }
}
