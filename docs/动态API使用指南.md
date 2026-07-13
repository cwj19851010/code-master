# 动态 API 使用指南（完整版）

## 概述

CodeMaster 项目实现了类似 ABP vNext 的动态 API 功能，可以自动将 Application Service 转换为 RESTful API，无需手动编写 Controller。

## 核心特性

### 1. HTTP Method 自动映射

系统根据方法名自动推断 HTTP Method：

| 方法名前缀 | HTTP Method |
|-----------|-------------|
| Get*      | GET         |
| Create*, Add*, Insert* | POST |
| Update*, Edit*, Modify* | PUT |
| Delete*, Remove* | DELETE |
| 其他      | POST (默认) |

**支持 MVC 原生特性**：如果方法上已标记 `[HttpGet]`、`[HttpPost]` 等特性，则优先使用这些特性。

### 2. 路由自动生成

- **Controller 路由**: `/api/{module}/{controller}`
  - 示例: `SysUserService` → `/api/system/sysuser`

- **Action 路由**: 根据方法名自动映射（自动去掉 `Async` 后缀）
  - `GetByIdAsync` → `{id}` (完整路由: `/api/system/sysuser/{id}`)
  - `GetPagedListAsync` → `page` (完整路由: `/api/system/sysuser/page`)
  - `GetListAsync` → `list` (完整路由: `/api/system/sysuser/list`)
  - `CreateAsync` → `` (完整路由: `/api/system/sysuser`)
  - `UpdateAsync` → `` (完整路由: `/api/system/sysuser`)
  - `DeleteAsync` → `delete/{id}` (完整路由: `/api/system/sysuser/delete/{id}`)
  - `BatchDeleteAsync` → `batch` (完整路由: `/api/system/sysuser/batch`)

**注意**: `DeleteAsync` 使用 `delete/{id}` 路由而不是 `{id}`，避免与 `GetByIdAsync` 的路由冲突。

### 3. 自动去掉 Async 后缀

所有以 `Async` 结尾的方法名会自动去掉后缀：
- `GetUserAsync` → 路由为 `GetUser` 或 `getuser`（取决于配置）
- `CreateOrderAsync` → 路由为 `CreateOrder` 或 `createorder`

## 使用方式

### 1. 基本使用（自动生成 API）

只需让 Service 接口继承 `IApplicationService`：

```csharp
public interface ISysUserService : ICrudService<SysUserDto, long>, IApplicationService
{
    // 所有方法都会自动生成 API
    Task<PagedResult<SysUserDto>> GetPagedListAsync(SysUserQueryDto query);
    Task<SysUserDto> GetByIdAsync(long id);
    Task<long> CreateAsync(CreateSysUserDto dto);
    Task UpdateAsync(long id, UpdateSysUserDto dto);
    Task DeleteAsync(long id);
}
```

### 2. 使用 MVC 原生特性（推荐）

#### 2.1 HTTP Method 特性

直接使用 MVC 的 `[HttpGet]`、`[HttpPost]` 等特性：

```csharp
public interface ISysUserService : IApplicationService
{
    // GET /api/system/sysuser/page
    [HttpGet("page")]
    Task<PagedResult<SysUserDto>> GetPagedListAsync(SysUserQueryDto query);

    // GET /api/system/sysuser/{id}
    [HttpGet("{id}")]
    Task<SysUserDto> GetByIdAsync(long id);

    // POST /api/system/sysuser
    [HttpPost]
    Task<long> CreateAsync(CreateSysUserDto dto);

    // PUT /api/system/sysuser/{id}
    [HttpPut("{id}")]
    Task UpdateAsync(long id, UpdateSysUserDto dto);

    // DELETE /api/system/sysuser/{id}
    [HttpDelete("{id}")]
    Task DeleteAsync(long id);

    // PATCH /api/system/sysuser/{id}/status
    [HttpPatch("{id}/status")]
    Task UpdateStatusAsync(long id, int status);
}
```

#### 2.2 权限控制特性

使用 MVC 原生的 `[Authorize]` 和 `[AllowAnonymous]` 特性：

```csharp
public interface ISysUserService : IApplicationService
{
    // 需要认证才能访问
    [Authorize]
    [HttpGet("page")]
    Task<PagedResult<SysUserDto>> GetPagedListAsync(SysUserQueryDto query);

    // 需要特定权限
    [Authorize(Policy = "Permission:system:user:create")]
    [HttpPost]
    Task<long> CreateAsync(CreateSysUserDto dto);

    // 需要特定角色
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    Task DeleteAsync(long id);

    // 允许匿名访问
    [AllowAnonymous]
    [HttpGet("public")]
    Task<List<SysUserDto>> GetPublicUsersAsync();
}
```

### 3. 使用 DynamicApi 特性（可选）

如果不想使用 MVC 特性，也可以使用 `[DynamicApi]` 特性：

```csharp
public interface ISysUserService : IApplicationService
{
    // 自定义 HTTP Method 和路由
    [DynamicApi(HttpMethod = "PATCH", Route = "{id}/status")]
    Task UpdateStatusAsync(long id, int status);

    // 指定权限代码
    [DynamicApi(Permission = "system:user:export")]
    Task<byte[]> ExportAsync();
}
```

### 4. 禁用特定方法的 API 生成

#### 方式 1: 使用 `[NonDynamicApi]` 特性

```csharp
public interface ISysUserService : IApplicationService
{
    // 这个方法不会生成 API
    [NonDynamicApi]
    Task<bool> InternalMethodAsync();

    // 这个方法会生成 API
    Task<SysUserDto> GetByIdAsync(long id);
}
```

#### 方式 2: 使用 `[DynamicApi(IsEnabled = false)]`

```csharp
public interface ISysUserService : IApplicationService
{
    // 这个方法不会生成 API
    [DynamicApi(IsEnabled = false)]
    Task<bool> InternalMethodAsync();
}
```

### 5. 禁用整个 Service 的 API 生成

在类级别使用 `[NonDynamicApi]`：

```csharp
[NonDynamicApi]
public class InternalService : IApplicationService
{
    // 这个 Service 的所有方法都不会生成 API
    public Task DoSomethingAsync() { }
}
```

## 完整示例

### 示例 1: 使用 MVC 原生特性（推荐）

```csharp
namespace CodeMaster.Application.Services.System;

public interface ISysUserService : ICrudService<SysUserDto, long>, IApplicationService
{
    // GET /api/system/sysuser/page
    // 需要 system:user:list 权限
    [HttpGet("page")]
    [Authorize(Policy = "Permission:system:user:list")]
    Task<PagedResult<SysUserDto>> GetPagedListAsync(SysUserQueryDto query);

    // GET /api/system/sysuser/{id}
    // 需要 system:user:view 权限
    [HttpGet("{id}")]
    [Authorize(Policy = "Permission:system:user:view")]
    Task<SysUserDto> GetByIdAsync(long id);

    // POST /api/system/sysuser
    // 需要 system:user:create 权限
    [HttpPost]
    [Authorize(Policy = "Permission:system:user:create")]
    Task<long> CreateAsync(CreateSysUserDto dto);

    // PUT /api/system/sysuser/{id}
    // 需要 system:user:update 权限
    [HttpPut("{id}")]
    [Authorize(Policy = "Permission:system:user:update")]
    Task UpdateAsync(long id, UpdateSysUserDto dto);

    // DELETE /api/system/sysuser/{id}
    // 需要 system:user:delete 权限
    [HttpDelete("{id}")]
    [Authorize(Policy = "Permission:system:user:delete")]
    Task DeleteAsync(long id);

    // PATCH /api/system/sysuser/{id}/reset-password
    // 需要 system:user:reset-password 权限
    [HttpPatch("{id}/reset-password")]
    [Authorize(Policy = "Permission:system:user:reset-password")]
    Task ResetPasswordAsync(long id);

    // 允许匿名访问的公开接口
    [HttpGet("public")]
    [AllowAnonymous]
    Task<List<SysUserDto>> GetPublicUsersAsync();

    // 内部方法，不生成 API
    [NonDynamicApi]
    Task<bool> ValidateUserAsync(string username);
}
```

### 示例 2: 混合使用（自动推断 + MVC 特性）

```csharp
public interface ISysUserService : IApplicationService
{
    // 自动推断: GET /api/system/sysuser/page
    [Authorize(Policy = "Permission:system:user:list")]
    Task<PagedResult<SysUserDto>> GetPagedListAsync(SysUserQueryDto query);

    // 显式指定: GET /api/system/sysuser/{id}
    [HttpGet("{id}")]
    [Authorize(Policy = "Permission:system:user:view")]
    Task<SysUserDto> GetByIdAsync(long id);

    // 自动推断: POST /api/system/sysuser
    [Authorize(Policy = "Permission:system:user:create")]
    Task<long> CreateAsync(CreateSysUserDto dto);

    // 显式指定: PATCH /api/system/sysuser/batch-update
    [HttpPatch("batch-update")]
    [Authorize(Roles = "Admin")]
    Task BatchUpdateAsync(List<long> ids, UpdateSysUserDto dto);
}
```

## 特性优先级

当多个配置方式同时存在时，优先级如下：

1. **MVC 原生特性** (`[HttpGet]`, `[HttpPost]` 等) - 最高优先级
2. **DynamicApi 特性** (`[DynamicApi]`)
3. **自动推断**（根据方法名）- 最低优先级

权限特性优先级：

1. **AllowAnonymous** - 最高优先级（允许匿名访问）
2. **Authorize** - MVC 原生权限特性
3. **DynamicApi.Permission** - 自定义权限代码

## 配置选项

在 `Program.cs` 中配置动态 API：

```csharp
builder.Services.AddDynamicApi(options =>
{
    options.IsEnabled = true;                    // 是否启用动态 API
    options.RoutePrefix = "api";                 // 路由前缀
    options.UseLowercaseRoutes = true;           // 是否使用小写路由
    options.RemoveServiceSuffix = true;          // 移除 Service 后缀
    options.RemoveAppServiceSuffix = true;       // 移除 AppService 后缀
});
```

## 注意事项

1. **Service 必须实现 `IApplicationService` 接口**才会被自动扫描
2. **优先使用 MVC 原生特性**（`[HttpGet]`、`[Authorize]` 等），更标准、更灵活
3. **Async 后缀自动去除**：`GetUserAsync` 的路由会是 `GetUser` 而不是 `GetUserAsync`
4. **路由冲突**：确保不同方法生成的路由不会冲突
5. **权限验证**：使用 `[Authorize(Policy = "Permission:xxx")]` 配合权限系统实现

## 与手动 Controller 对比

### 手动方式（传统）

```csharp
[ApiController]
[Route("api/system/sysuser")]
public class SysUserController : ControllerBase
{
    private readonly ISysUserService _userService;

    [HttpGet("page")]
    [Authorize(Policy = "Permission:system:user:list")]
    public async Task<PagedResult<SysUserDto>> GetPagedList([FromQuery] SysUserQueryDto query)
    {
        return await _userService.GetPagedListAsync(query);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "Permission:system:user:view")]
    public async Task<SysUserDto> GetById(long id)
    {
        return await _userService.GetByIdAsync(id);
    }

    // ... 更多重复代码
}
```

### 动态 API 方式（推荐）

```csharp
public interface ISysUserService : IApplicationService
{
    [HttpGet("page")]
    [Authorize(Policy = "Permission:system:user:list")]
    Task<PagedResult<SysUserDto>> GetPagedListAsync(SysUserQueryDto query);

    [HttpGet("{id}")]
    [Authorize(Policy = "Permission:system:user:view")]
    Task<SysUserDto> GetByIdAsync(long id);
}
```

**优势**：
- 减少 80% 的样板代码
- 统一的路由和命名约定
- 更容易维护和重构
- 直接使用 MVC 标准特性
- Service 接口即 API 文档
