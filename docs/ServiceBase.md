# Service 基类使用指南

## 概述

`CrudServiceBase` 提供了完整的 CRUD 功能，并自动集成数据权限过滤。使用基类可以大大减少重复代码。

## 基本使用

### 1. 定义查询 DTO

```csharp
public class SysUserQueryDto : PagedQueryDto
{
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public int? Status { get; set; }
    public long? DeptId { get; set; }
}
```

### 2. 继承基类

```csharp
public class SysUserService : CrudServiceBase<
    SysUser,              // 实体类型
    CreateSysUserDto,     // 创建DTO
    UpdateSysUserDto,     // 更新DTO
    SysUserDto,           // 返回DTO
    SysUserQueryDto>      // 查询DTO
{
    public SysUserService(
        IRepository<SysUser> repository,
        ISqlSugarClient db,
        IDataPermissionService dataPermissionService,
        IHttpContextAccessor httpContextAccessor)
        : base(repository, db, dataPermissionService, httpContextAccessor)
    {
    }

    // 只需重写查询条件构建方法
    protected override ISugarQueryable<SysUser> BuildQueryConditions(
        ISugarQueryable<SysUser> query,
        SysUserQueryDto queryDto)
    {
        return query
            .WhereIF(!string.IsNullOrEmpty(queryDto.UserName),
                u => u.UserName.Contains(queryDto.UserName))
            .WhereIF(!string.IsNullOrEmpty(queryDto.PhoneNumber),
                u => u.PhoneNumber.Contains(queryDto.PhoneNumber))
            .WhereIF(queryDto.Status.HasValue,
                u => u.Status == queryDto.Status.Value)
            .WhereIF(queryDto.DeptId.HasValue,
                u => u.DeptId == queryDto.DeptId.Value);
    }

    // 如果需要特殊逻辑，可以重写方法
    public override async Task<long> CreateAsync(CreateSysUserDto dto)
    {
        // 密码加密
        var user = dto.Adapt<SysUser>();
        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.CreateUserId = GetCurrentUserId();

        return await _repository.InsertAsync(user);
    }
}
```

## 自动功能

### 1. 自动数据权限过滤

如果实体继承了 `DataPermissionEntityBase` 或实现了 `IDataPermission`，基类会自动应用数据权限过滤：

```csharp
// 用户查询时，自动根据 DataScope 过滤数据
var users = await userService.GetPagedListAsync(new SysUserQueryDto
{
    PageNum = 1,
    PageSize = 10
});
// 返回的数据已经根据当前用户的数据权限过滤
```

### 2. 自动设置 CreateUserId

创建实体时，基类会自动设置 `CreateUserId`：

```csharp
await userService.CreateAsync(new CreateSysUserDto
{
    UserName = "test",
    Password = "123456"
});
// CreateUserId 自动设置为当前登录用户ID
```

### 3. 通用 CRUD 方法

基类提供了完整的 CRUD 方法：

```csharp
// 查询
var user = await userService.GetByIdAsync(1);
var pagedList = await userService.GetPagedListAsync(queryDto);

// 创建
var id = await userService.CreateAsync(createDto);

// 更新
await userService.UpdateAsync(updateDto);

// 删除
await userService.DeleteAsync(1);
await userService.DeleteBatchAsync(new[] { 1L, 2L, 3L });
```

## 高级用法

### 1. 禁用数据权限过滤

如果某个查询不需要数据权限过滤，可以重写 `ApplyDataPermission` 方法：

```csharp
protected override ISugarQueryable<SysUser> ApplyDataPermission(
    ISugarQueryable<SysUser> query)
{
    // 不应用数据权限过滤
    return query;
}
```

### 2. 自定义数据权限逻辑

```csharp
protected override ISugarQueryable<SysUser> ApplyDataPermission(
    ISugarQueryable<SysUser> query)
{
    var userId = GetCurrentUserId();
    var dataScope = GetCurrentUserDataScope();

    // 自定义逻辑
    if (dataScope == 5) // 仅本人
    {
        return query.Where(u => u.CreateUserId == userId);
    }

    // 其他情况使用默认逻辑
    return base.ApplyDataPermission(query);
}
```

### 3. 添加额外的查询方法

```csharp
public class SysUserService : CrudServiceBase<...>
{
    // 添加自定义查询方法
    public async Task<List<SysUserDto>> GetUsersByDeptAsync(long deptId)
    {
        var query = _db.Queryable<SysUser>()
            .Where(u => u.DeptId == deptId);

        // 自动应用数据权限
        query = ApplyDataPermission(query);

        var users = await query.ToListAsync();
        return users.Adapt<List<SysUserDto>>();
    }

    // 添加统计方法
    public async Task<int> GetUserCountAsync()
    {
        var query = _db.Queryable<SysUser>();
        query = ApplyDataPermission(query);
        return await query.CountAsync();
    }
}
```

## 完整示例

### 重构前（手写所有代码）

```csharp
public class SysUserService
{
    private readonly IRepository<SysUser> _repository;
    private readonly ISqlSugarClient _db;

    public async Task<PagedResultDto<SysUserDto>> GetPagedListAsync(SysUserQueryDto query)
    {
        var queryable = _db.Queryable<SysUser>()
            .WhereIF(!string.IsNullOrEmpty(query.UserName), u => u.UserName.Contains(query.UserName))
            .WhereIF(!string.IsNullOrEmpty(query.PhoneNumber), u => u.PhoneNumber.Contains(query.PhoneNumber))
            .WhereIF(query.Status.HasValue, u => u.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .Skip((query.PageNum - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResultDto<SysUserDto>
        {
            Items = items.Adapt<List<SysUserDto>>(),
            Total = total,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    public async Task<long> CreateAsync(CreateSysUserDto dto)
    {
        var user = dto.Adapt<SysUser>();
        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        return await _repository.InsertAsync(user);
    }

    public async Task<int> UpdateAsync(UpdateSysUserDto dto)
    {
        var user = await _repository.GetByIdAsync(dto.Id);
        if (user == null) throw new Exception("用户不存在");
        dto.Adapt(user);
        return await _repository.UpdateAsync(user);
    }

    public async Task<int> DeleteAsync(long id)
    {
        return await _db.SoftDeleteAsync<SysUser>(id);
    }
}
```

### 重构后（使用基类）

```csharp
public class SysUserService : CrudServiceBase<
    SysUser, CreateSysUserDto, UpdateSysUserDto, SysUserDto, SysUserQueryDto>
{
    public SysUserService(
        IRepository<SysUser> repository,
        ISqlSugarClient db,
        IDataPermissionService dataPermissionService,
        IHttpContextAccessor httpContextAccessor)
        : base(repository, db, dataPermissionService, httpContextAccessor)
    {
    }

    protected override ISugarQueryable<SysUser> BuildQueryConditions(
        ISugarQueryable<SysUser> query,
        SysUserQueryDto queryDto)
    {
        return query
            .WhereIF(!string.IsNullOrEmpty(queryDto.UserName), u => u.UserName.Contains(queryDto.UserName))
            .WhereIF(!string.IsNullOrEmpty(queryDto.PhoneNumber), u => u.PhoneNumber.Contains(queryDto.PhoneNumber))
            .WhereIF(queryDto.Status.HasValue, u => u.Status == queryDto.Status.Value);
    }

    public override async Task<long> CreateAsync(CreateSysUserDto dto)
    {
        var user = dto.Adapt<SysUser>();
        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.CreateUserId = GetCurrentUserId();
        return await _repository.InsertAsync(user);
    }
}
```

**代码减少了 60%+，并且自动支持数据权限过滤！**

## 注意事项

1. **查询 DTO 必须继承 PagedQueryDto**
2. **实体必须继承 EntityBase**
3. **如果需要数据权限，实体应继承 DataPermissionEntityBase**
4. **子类可以完全重写任何方法**
5. **基类方法都是 virtual，可以按需重写**

## 依赖注入配置

在 `Program.cs` 中注册服务：

```csharp
// 注册数据权限服务
builder.Services.AddScoped<IDataPermissionService, DataPermissionService>();

// 注册 HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 注册业务服务
builder.Services.AddScoped<ISysUserService, SysUserService>();
```
