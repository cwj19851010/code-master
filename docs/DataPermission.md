# 数据权限使用指南

## 概述

CodeMaster 实现了完整的数据权限系统，支持以下权限范围：

1. **全部数据权限** (DataScope = 1) - 可以查看所有数据
2. **自定义数据权限** (DataScope = 2) - 可以查看指定部门的数据
3. **本部门数据权限** (DataScope = 3) - 只能查看本部门的数据
4. **本部门及以下数据权限** (DataScope = 4) - 可以查看本部门及下属部门的数据
5. **仅本人数据权限** (DataScope = 5) - 只能查看自己创建的数据

## 实体配置

### 方式一：继承 DataPermissionEntityBase

```csharp
using CodeMaster.Core.Entities;

public class MyEntity : DataPermissionEntityBase
{
    public string Name { get; set; }
    // DeptId 和 CreateUserId 已在基类中定义
}
```

### 方式二：实现 IDataPermission 接口

```csharp
using CodeMaster.Core.Entities;

public class MyEntity : EntityBase, IDataPermission
{
    public string Name { get; set; }

    public long? DeptId { get; set; }
    public long? CreateUserId { get; set; }
}
```

## Service 中使用数据权限

### 示例：在查询中应用数据权限

```csharp
public class MyService
{
    private readonly ISqlSugarClient _db;
    private readonly IDataPermissionService _dataPermissionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<List<MyEntity>> GetListAsync()
    {
        // 1. 获取当前用户信息
        var userId = GetCurrentUserId();
        var dataScope = GetCurrentUserDataScope();
        var deptId = GetCurrentUserDeptId();

        // 2. 构建查询
        var query = _db.Queryable<MyEntity>();

        // 3. 应用数据权限过滤
        if (dataScope == 4) // 本部门及以下
        {
            var deptIds = await _dataPermissionService.GetChildDeptIdsAsync(deptId.Value);
            query = query.Where(e => deptIds.Contains(e.DeptId ?? 0));
        }
        else
        {
            var permissionExpr = _dataPermissionService
                .BuildDataPermissionExpression<MyEntity>(userId, dataScope, deptId);
            query = query.Where(permissionExpr);
        }

        // 4. 执行查询
        return await query.ToListAsync();
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier);
        return long.Parse(userIdClaim?.Value ?? "0");
    }

    private int GetCurrentUserDataScope()
    {
        var dataScopeClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("DataScope");
        return int.Parse(dataScopeClaim?.Value ?? "1");
    }

    private long? GetCurrentUserDeptId()
    {
        var deptIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("DeptId");
        return string.IsNullOrEmpty(deptIdClaim?.Value)
            ? null
            : long.Parse(deptIdClaim.Value);
    }
}
```

## 数据权限范围说明

| DataScope | 名称 | 说明 | 示例 |
|-----------|------|------|------|
| 1 | 全部数据 | 可以查看所有数据 | 超级管理员 |
| 2 | 自定义数据 | 可以查看指定部门的数据 | 跨部门管��员 |
| 3 | 本部门数据 | 只能查看本部门的数据 | 部门经理 |
| 4 | 本部门及以下 | 可以查看本部门及下属部门的数据 | 分管领导 |
| 5 | 仅本人数据 | 只能查看自己创建的数据 | 普通员工 |

## 注意事项

1. **实体必须实现 IDataPermission 接口**才能启用数据权限过滤
2. **DeptId 和 CreateUserId 字段**必须正确设置
3. **DataScope = 4** 时需要使用 `GetChildDeptIdsAsync` 获取子部门列表
4. **系统实体**（User、Role、Menu、Dept）通常不需要数据权限过滤
5. **业务实体**应该继承 `DataPermissionEntityBase` 或实现 `IDataPermission`

## 数据库字段

启用数据权限的实体需要以下字段：

```sql
dept_id BIGINT NULL,           -- 部门ID
create_user_id BIGINT NULL     -- 创建人ID
```

这些字段已在 `EntityBase` 和 `DataPermissionEntityBase` 中定义。
