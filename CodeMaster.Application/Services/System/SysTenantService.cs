using CodeMaster.Application.Dtos.System;
using CodeMaster.Core.Dtos;
using CodeMaster.Domain.Entities.System;
using SqlSugar;
using Yitter.IdGenerator;
using Mapster;
using CodeMaster.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using CodeMaster.Core.Authorization;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 租户管理服务实现
/// </summary>
public class SysTenantService : ISysTenantService
{
    private readonly ISqlSugarClient _db;

    public SysTenantService(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 根据ID获取租户
    /// </summary>
    [Permission("system:tenant:view")]
    public async Task<SysTenantDto?> GetByIdAsync(long id)
    {
        var tenant = await _db.Queryable<SysTenant>()
            .Where(t => t.Id == id)
            .FirstAsync();

        if (tenant == null) return null;

        // 使用 Mapster 映射
        return tenant.Adapt<SysTenantDto>();
    }

    /// <summary>
    /// 分页查询租户
    /// </summary>
    [Permission("system:tenant:list")]
    public async Task<PagedResultDto<SysTenantDto>> GetPagedListAsync(SysTenantQueryDto query)
    {
        var queryable = _db.Queryable<SysTenant>()
            .WhereIF(!string.IsNullOrEmpty(query.TenantCode), t => t.TenantCode.Contains(query.TenantCode!))
            .WhereIF(!string.IsNullOrEmpty(query.TenantName), t => t.TenantName.Contains(query.TenantName!))
            .WhereIF(query.IsolationType.HasValue, t => t.IsolationType == query.IsolationType!.Value)
            .WhereIF(query.Status.HasValue, t => t.Status == query.Status!.Value)
            .OrderBy(t => t.CreateTime, OrderByType.Desc);

        var total = await queryable.CountAsync();
        var items = await queryable
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 使用 Mapster 批量映射
        return new PagedResultDto<SysTenantDto>
        {
            Items = items.Adapt<List<SysTenantDto>>(),
            Total = total,
            PageNum = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    [Permission("system:tenant:create")]
    public async Task<SysTenantDto> CreateAsync(CreateSysTenantDto dto)
    {
        // 检查租户编码是否已存在
        var exists = await _db.Queryable<SysTenant>()
            .Where(t => t.TenantCode == dto.TenantCode)
            .AnyAsync();

        if (exists)
        {
            throw new Exception($"租户编码 {dto.TenantCode} 已存在");
        }

        // 使用事务创建租户及其管理员
        try
        {
            _db.Ado.BeginTran();

            // 1. 创建租户
            var tenant = dto.Adapt<SysTenant>();
            tenant.Id = YitIdHelper.NextId();
            tenant.CreateTime = DateTime.UtcNow;
            tenant.TenantId = 0; // 租户本身属于宿主数据

            await _db.Insertable(tenant).ExecuteCommandAsync();

            // 2. 为租户创建管理员角色
            var adminRole = new SysRole
            {
                Id = YitIdHelper.NextId(),
                RoleName = $"{dto.TenantName}-管理员",
                RoleKey = $"{dto.TenantCode}_admin",
                RoleSort = 1,
                Status = 0,
                IsTenantAdmin = true,
                TenantId = tenant.Id, // 角色属于该租户
                CreateTime = DateTime.UtcNow,
                Remark = "租户管理员角色"
            };

            await _db.Insertable(adminRole).ExecuteCommandAsync();

            // 3. 为租户创建管理员用户
            var adminUser = new SysUser
            {
                Id = YitIdHelper.NextId(),
                UserName = $"{dto.TenantCode}_admin",
                NickName = $"{dto.TenantName}管理员",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"), // 默认密码
                Email = $"{dto.TenantCode}_admin@{dto.TenantCode}.com",
                Status = 0,
                TenantId = tenant.Id, // 用户属于该租户
                CreateTime = DateTime.UtcNow,
                Remark = "租户管理员账号"
            };

            await _db.Insertable(adminUser).ExecuteCommandAsync();

            // 4. 关联用户和角色
            var userRole = new SysUserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            };

            await _db.Insertable(userRole).ExecuteCommandAsync();

            // 5. 为管理员角色分配所有租户可见的菜单（MenuScope = 1 或 2）
            var tenantMenus = await _db.Queryable<SysMenu>()
                .Where(m => m.MenuScope == 1 || m.MenuScope == 2) // 租户专用 + 共享菜单
                .Select(m => m.Id)
                .ToListAsync();

            if (tenantMenus.Any())
            {
                var roleMenus = tenantMenus.Select(menuId => new SysRoleMenu
                {
                    RoleId = adminRole.Id,
                    MenuId = menuId
                }).ToList();

                await _db.Insertable(roleMenus).ExecuteCommandAsync();
            }

            _db.Ado.CommitTran();

            return tenant.Adapt<SysTenantDto>();
        }
        catch (Exception ex)
        {
            _db.Ado.RollbackTran();
            throw new Exception($"创建租户失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    [Permission("system:tenant:update")]
    public async Task<bool> UpdateAsync(UpdateSysTenantDto dto)
    {
        var tenant = await _db.Queryable<SysTenant>()
            .Where(t => t.Id == dto.Id)
            .FirstAsync();

        if (tenant == null)
        {
            throw new Exception("租户不存在");
        }

        // 检查租户编码是否被其他租户使用
        var exists = await _db.Queryable<SysTenant>()
            .Where(t => t.TenantCode == dto.TenantCode && t.Id != dto.Id)
            .AnyAsync();

        if (exists)
        {
            throw new Exception($"租户编码 {dto.TenantCode} 已被其他租户使用");
        }

        // 使用 Mapster 更新现有对象
        dto.Adapt(tenant);
        tenant.UpdateTime = DateTime.UtcNow;

        return await _db.Updateable(tenant).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 删除租户
    /// </summary>
    [Permission("system:tenant:delete")]
    public async Task<bool> DeleteAsync(long id)
    {
        return await _db.SoftDeleteAsync<SysTenant>(id) > 0;
    }

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    public async Task<bool> TestConnectionAsync(string connectionString, int dbType)
    {
        try
        {
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = (DbType)dbType,
                IsAutoCloseConnection = true
            });

            // 尝试执行一个简单的查询
            await db.Ado.GetDataTableAsync("SELECT 1");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
