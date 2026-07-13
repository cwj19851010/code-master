# Mapster 对象映射工具使用指南

## 为什么选择 Mapster？

### 性能对比
- **Mapster**: 最快，接近手写代码的性能
- **AutoMapper**: 比 Mapster 慢 4-6 倍
- **手写映射**: 性能最好，但代码冗长

### 基准测试结果
```
| Method      | Mean     | Allocated |
|------------ |---------:|----------:|
| Mapster     | 1.00x    | 1.00x     |
| AutoMapper  | 4.50x    | 3.20x     |
| Manual      | 0.95x    | 0.90x     |
```

## 安装

```bash
dotnet add package Mapster
dotnet add package Mapster.DependencyInjection
```

## 基本使用

### 1. 单个对象映射
```csharp
var user = new SysUser { Id = 1, UserName = "admin" };

// 映射到 DTO
var userDto = user.Adapt<SysUserDto>();
```

### 2. 集合映射
```csharp
var users = new List<SysUser> { ... };

// 批量映射
var userDtos = users.Adapt<List<SysUserDto>>();
```

### 3. 更新现有对象
```csharp
var updateDto = new UpdateSysUserDto { NickName = "新昵称" };
var existingUser = await _repository.GetByIdAsync(1);

// 将 DTO 的值更新到实体（只更新非 null 字段）
updateDto.Adapt(existingUser);
```

### 4. IQueryable 投影（性能优化）
```csharp
// 使用 ProjectToType 进行投影
// 只查询需要的字段，减少数据库传输
var userDtos = await _repository.AsQueryable()
    .Where(u => u.Status == 1)
    .ProjectToType<SysUserDto>()
    .ToListAsync();
```

## 全局配置

在 `MapsterConfig.cs` 中配置映射规则：

```csharp
public static class MapsterConfig
{
    public static void Configure()
    {
        // 全局配置
        TypeAdapterConfig.GlobalSettings.Default
            .IgnoreNullValues(true)      // 忽略 null 值
            .PreserveReference(true);    // 避免循环引用

        // 自定义映射规则
        TypeAdapterConfig<SysUser, SysUserDto>
            .NewConfig()
            .Map(dest => dest.Gender, src => src.Sex)  // 字段名不同
            .Ignore(dest => dest.Password);            // 忽略敏感字段
    }
}
```

在 `Program.cs` 中调用：
```csharp
MapsterConfig.Configure();
```

## 高级用法

### 1. 运行时自定义映射
```csharp
var userDto = user.Adapt<SysUserDto>(config =>
{
    config.Map(dest => dest.Gender, src => src.Sex == 1 ? "男" : "女");
    config.Ignore(dest => dest.Password);
});
```

### 2. 条件映射
```csharp
TypeAdapterConfig<SysUser, SysUserDto>
    .NewConfig()
    .Map(dest => dest.DeptName,
         src => src.DeptId.HasValue ? src.Dept.DeptName : "未分配",
         srcCond => srcCond.DeptId.HasValue);
```

### 3. 嵌套对象映射
```csharp
TypeAdapterConfig<SysUser, SysUserDto>
    .NewConfig()
    .Map(dest => dest.DeptName, src => src.Dept.DeptName)
    .Map(dest => dest.RoleNames, src => src.Roles.Select(r => r.RoleName));
```

### 4. 双向映射
```csharp
TypeAdapterConfig<SysUser, SysUserDto>.NewConfig().TwoWays();
```

### 5. 映射前后处理
```csharp
TypeAdapterConfig<SysUser, SysUserDto>
    .NewConfig()
    .BeforeMapping((src, dest) =>
    {
        // 映射前处理
        Console.WriteLine($"开始映射用户: {src.UserName}");
    })
    .AfterMapping((src, dest) =>
    {
        // 映射后处理
        dest.DisplayName = $"{dest.NickName}({dest.UserName})";
    });
```

## 实际应用示例

### Service 层使用
```csharp
public class SysUserService : ISysUserService
{
    private readonly IRepository<SysUser> _repository;

    public async Task<SysUserDto?> GetByIdAsync(long id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user?.Adapt<SysUserDto>();  // 简洁！
    }

    public async Task<PagedResultDto<SysUserDto>> GetPagedListAsync(SysUserQueryDto query)
    {
        var (items, total) = await _repository.GetPagedListAsync(...);

        // 批量映射
        var dtoList = items.Adapt<List<SysUserDto>>();

        return new PagedResultDto<SysUserDto>(dtoList, total, query.PageNum, query.PageSize);
    }

    public async Task<long> CreateAsync(CreateSysUserDto dto)
    {
        var user = dto.Adapt<SysUser>();  // DTO -> Entity
        return await _repository.InsertAsync(user);
    }

    public async Task<int> UpdateAsync(UpdateSysUserDto dto)
    {
        var user = await _repository.GetByIdAsync(dto.Id);
        dto.Adapt(user);  // 更新现有对象
        return await _repository.UpdateAsync(user);
    }
}
```

## 性能优化建议

### 1. 使用 ProjectToType（推荐）
```csharp
// ❌ 不推荐：查询所有字段再映射
var users = await _repository.GetListAsync();
var dtos = users.Adapt<List<SysUserDto>>();

// ✅ 推荐：只查询需要的字段
var dtos = await _repository.AsQueryable()
    .ProjectToType<SysUserDto>()
    .ToListAsync();
```

### 2. 预编译映射配置
```csharp
// 在启动时预编译
TypeAdapterConfig<SysUser, SysUserDto>.NewConfig().Compile();
```

### 3. 避免重复配置
```csharp
// ❌ 不推荐：每次都配置
var dto = user.Adapt<SysUserDto>(config => config.Map(...));

// ✅ 推荐：全局配置一次
TypeAdapterConfig<SysUser, SysUserDto>.NewConfig().Map(...);
var dto = user.Adapt<SysUserDto>();
```

## 常见问题

### Q: Mapster 会自动映射哪些字段？
A: 名称相同且类型兼容的字段会自动映射，包括：
- 相同名称和类型
- 可空类型 ↔ 非可空类型
- 数值类型之间的转换
- 字符串 ↔ 枚举

### Q: 如何处理复杂的映射逻辑？
A: 使用 `Map()` 方法自定义映射：
```csharp
TypeAdapterConfig<SysUser, SysUserDto>
    .NewConfig()
    .Map(dest => dest.FullName, src => $"{src.NickName}({src.UserName})")
    .Map(dest => dest.Age, src => DateTime.Now.Year - src.BirthDate.Year);
```

### Q: Mapster 支持依赖注入吗？
A: 支持！使用 `Mapster.DependencyInjection` 包：
```csharp
builder.Services.AddMapster();
```

## 总结

Mapster 的优势：
- ✅ **性能极佳**：接近手写代码的性能
- ✅ **API 简洁**：`.Adapt<T>()` 一行搞定
- ✅ **功能强大**：支持复杂映射、投影查询
- ✅ **易于使用**：零配置即可使用，需要时再配置
- ✅ **内存友好**：分配更少的内存

推荐在所有需要对象映射的场景使用 Mapster！
