using CodeMaster.Domain.Entities.System;
using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Yitter.IdGenerator;

namespace CodeMaster.Migrator.SeedData.Core;

/// <summary>
/// 基础数据模块：租户、部门、职位、角色、用户
/// </summary>
public class BaseDataModule : ISeedModule
{
    public string ModuleName => "基础数据";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        // 基础数据模块不需要菜单
        return true;
    }

    public Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 基础数据模块不需要菜单
        return Task.CompletedTask;
    }

    public async Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        // 1. 创建默认租户
        var hasTenant = await dbContext.Set<SysTenant>().AnyAsync();
        if (!hasTenant)
        {
            var defaultTenant = new SysTenant
            {
                Id = YitIdHelper.NextId(),
                TenantName = "默认租户",
                TenantCode = "default",
                IsolationType = 1, // 逻辑隔离
                Status = 0,
                CreateTime = DateTime.UtcNow,
                Remark = "系统默认租户"
            };
            await dbContext.Set<SysTenant>().AddAsync(defaultTenant);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("  - 创建默认租户: 默认租户");
        }

        // 2. 创建默认部门
        var hasDept = await dbContext.Set<SysDept>().AnyAsync();
        if (!hasDept)
        {
            var tenant = await dbContext.Set<SysTenant>().FirstAsync();
            var defaultDept = new SysDept
            {
                Id = YitIdHelper.NextId(),
                Name = "总公司",
                ParentId = null,
                Ancestors = "0",
                TenantId = tenant.Id,
                CreateTime = DateTime.UtcNow
            };
            await dbContext.Set<SysDept>().AddAsync(defaultDept);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("  - 创建默认部门: 总公司");
        }

        // 3. 创建默认职位
        var hasPost = await dbContext.Set<SysPost>().AnyAsync();
        if (!hasPost)
        {
            var defaultPost = new SysPost
            {
                Id = YitIdHelper.NextId(),
                PostName = "默认职位",
                DataScope = 3,
                CreateTime = DateTime.UtcNow,
                Remark = "系统默认职位"
            };
            await dbContext.Set<SysPost>().AddAsync(defaultPost);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("  - 创建默认职位: 默认职位");
        }

        // 4. 创建默认角色
        var hasRole = await dbContext.Set<SysRole>().AnyAsync();
        if (!hasRole)
        {
            var adminRole = new SysRole
            {
                Id = YitIdHelper.NextId(),
                RoleName = "超级管理员",
                RoleKey = "admin",
                RoleSort = 1,
                Status = 0,
                CreateTime = DateTime.UtcNow,
                Remark = "超级管理员"
            };
            await dbContext.Set<SysRole>().AddAsync(adminRole);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("  - 创建默认角色: 超级管理员");
        }

        var hasMemberRole = await dbContext.Set<SysRole>().AnyAsync(r => r.RoleKey == "member");
        if (!hasMemberRole)
        {
            var memberRole = new SysRole
            {
                Id = YitIdHelper.NextId(),
                RoleName = "社区用户",
                RoleKey = "member",
                RoleSort = 99,
                Status = 0,
                DataScope = 5,
                CreateTime = DateTime.UtcNow,
                Remark = "官网注册和 GitHub 登录用户默认角色"
            };
            await dbContext.Set<SysRole>().AddAsync(memberRole);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("  - 创建默认角色: member");
        }

        // 5. 创建默认用户
        await EnsureTenantAdminRoleFlagsAsync(dbContext);

        var hasUser = await dbContext.Set<SysUser>().AnyAsync();
        if (!hasUser)
        {
            var dept = await dbContext.Set<SysDept>().FirstAsync();
            var post = await dbContext.Set<SysPost>().FirstAsync();
            var adminUser = new SysUser
            {
                Id = YitIdHelper.NextId(),
                UserName = "admin",
                NickName = "管理员",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Email = "admin@codemaster.com",
                PhoneNumber = "13800138000",
                Sex = 0,
                Status = 0,
                DeptId = dept.Id,
                PostId = post.Id,
                CreateTime = DateTime.UtcNow,
                Remark = "系统管理员"
            };
            await dbContext.Set<SysUser>().AddAsync(adminUser);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("  - 创建默认用户: admin/admin123");
        }

        // 6. 创建用户角色关联
        var hasUserRole = await dbContext.Set<SysUserRole>().AnyAsync();
        if (!hasUserRole)
        {
            var user = await dbContext.Set<SysUser>().FirstAsync();
            var role = await dbContext.Set<SysRole>().FirstAsync();
            var userRole = new SysUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };
            await dbContext.Set<SysUserRole>().AddAsync(userRole);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("  - 关联用户角色");
        }
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        // 基础数据模块不需要额外翻译（使用全局翻译）
        return new Dictionary<string, Dictionary<string, string>>();
    }

    private static async Task EnsureTenantAdminRoleFlagsAsync(CodeMasterDbContext dbContext)
    {
        var tenants = await dbContext.Set<SysTenant>()
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.TenantCode);

        var roles = await dbContext.Set<SysRole>()
            .Where(r => !r.IsTenantAdmin && r.TenantId > 0)
            .ToListAsync();

        var changed = false;
        foreach (var role in roles)
        {
            var isPublicOwner = role.RoleKey.Equals("tenant_owner", StringComparison.OrdinalIgnoreCase);
            var isSystemCreatedTenantAdmin = tenants.TryGetValue(role.TenantId, out var tenantCode)
                && role.RoleKey.Equals($"{tenantCode}_admin", StringComparison.OrdinalIgnoreCase);

            if (!isPublicOwner && !isSystemCreatedTenantAdmin)
            {
                continue;
            }

            role.IsTenantAdmin = true;
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync();
            Console.WriteLine("  - 修复租户管理员角色标识: IsTenantAdmin");
        }
    }
}
