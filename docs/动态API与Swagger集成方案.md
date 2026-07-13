# 动态 API 与 Swagger 集成方案

## 问题背景

在实现动态 API 功能时，遇到了 Swagger 生成文档失败的问题：

```
Swashbuckle.AspNetCore.SwaggerGen.SwaggerGeneratorException:
Ambiguous HTTP method for action - CodeMaster.Application.Services.System.SysDeptService.GetByIdAsync
Actions require an explicit HttpMethod binding for Swagger/OpenAPI 3.0
```

## 问题原因

动态 API 约定（`IApplicationModelConvention`）在运行时修改了 `ActionModel`，创建了新的 `SelectorModel` 和路由信息。虽然添加了 `HttpMethodAttribute` 到 `EndpointMetadata`，但 Swagger 的 `ApiExplorer` 仍然无法正确识别 HTTP Method。

## 解决方案（参考 ABP vNext）

### 核心思路

**如果方法上已经有 HTTP Method 特性，动态 API 约定完全跳过该方法，不做任何修改**，让 ASP.NET Core 和 Swagger 使用原始的特性信息。

### 实现代码

```csharp
private bool ConfigureAction(ActionModel action)
{
    var method = action.ActionMethod;

    // 1. 检查是否已有 MVC 的 HTTP Method 特性
    var existingHttpMethodAttr = method.GetCustomAttributes()
        .FirstOrDefault(a => a is HttpGetAttribute || a is HttpPostAttribute ||
                             a is HttpPutAttribute || a is HttpDeleteAttribute ||
                             a is HttpPatchAttribute);

    // 如果已有特性，完全跳过，不做任何修改
    if (existingHttpMethodAttr != null)
    {
        Console.WriteLine($"[DynamicApi] {action.Controller.ControllerName}.{method.Name} -> 跳过（已有 HTTP Method 特性）");
        return true;
    }

    // 2. 如果没有特性，使用动态 API 约定
    var route = dynamicApiAttr?.Route ?? GetActionRoute(method);
    var httpMethod = dynamicApiAttr?.HttpMethod ?? GetHttpVerb(method);

    // ... 创建动态路由和 HTTP Method
}
```

### 使用方式

在 Service 接口和实现类上添加 HTTP Method 特性：

```csharp
public interface ISysUserService : IApplicationService
{
    [HttpGet("{id}")]
    Task<SysUserDto?> GetByIdAsync(long id);

    [HttpGet("page")]
    Task<PagedResultDto<SysUserDto>> GetPagedListAsync(SysUserQueryDto query);

    [HttpPost]
    Task<long> CreateAsync(CreateSysUserDto dto);

    [HttpPut("update/{id}")]
    Task<int> UpdateAsync(long id, UpdateSysUserDto dto);

    [HttpDelete("delete/{id}")]
    Task<int> DeleteAsync(long id);
}

public class SysUserService : ISysUserService
{
    [HttpGet("{id}")]
    public async Task<SysUserDto?> GetByIdAsync(long id) { ... }

    [HttpGet("page")]
    public async Task<PagedResultDto<SysUserDto>> GetPagedListAsync(SysUserQueryDto query) { ... }

    // ... 其他方法
}
```

## 优势

1. **Swagger 完全兼容**：Swagger 可以直接从方法特性读取 HTTP Method 信息
2. **路由明确**：开发者可以清楚地看到每个方法的 HTTP Method 和路由
3. **灵活性高**：可以自定义路由模板，不受动态 API 约定的限制
4. **调试友好**：IDE 可以识别特性，提供更好的代码提示

## 路由约定

| 方法名 | HTTP Method | 路由模板 | 示例 |
|--------|-------------|----------|------|
| GetByIdAsync | GET | `{id}` | GET /api/system/sysuser/1 |
| GetPagedListAsync | GET | `page` | GET /api/system/sysuser/page?PageNum=1&PageSize=10 |
| GetListAsync | GET | `list` | GET /api/system/sysdept/list |
| CreateAsync | POST | `` | POST /api/system/sysuser |
| UpdateAsync | PUT | `update/{id}` | PUT /api/system/sysuser/update/1 |
| DeleteAsync | DELETE | `delete/{id}` | DELETE /api/system/sysuser/delete/1 |

## 测试结果

- ✅ Swagger JSON 成功生成
- ✅ Swagger UI 正常访问
- ✅ 所有 API 端点正确识别
- ✅ HTTP Method 正确显示
- ✅ 路由参数正确绑定

## 参考资料

- ABP vNext 动态 API 实现：https://github.com/abpframework/abp
- ASP.NET Core Application Model：https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/application-model
- Swashbuckle.AspNetCore：https://github.com/domaindrivendev/Swashbuckle.AspNetCore
