using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.System;

/// <summary>
/// 租户管理模块
/// </summary>
public class TenantModule : ISeedModule
{
    public string ModuleName => "租户管理";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "tenant" && m.Component == "system/tenant/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找系统管理目录
        var systemMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/system" && m.MenuType == "M");

        if (systemMenu == null)
        {
            Console.WriteLine("  - 警告：未找到系统管理目录，跳过租户管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: systemMenu.Id,
            menuName: "租户管理",
            titleKey: "tenant",
            path: "tenant",
            component: "system/tenant/index",
            icon: "School",
            orderNum: 5,
            permsPrefix: "system:tenant"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加租户管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        // 租户数据由其他模块创建
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "tenant", new() { { "zh-CN", "租户管理" }, { "en-US", "Tenant" } } },
            { "tenant_name", new() { { "zh-CN", "租户名称" }, { "en-US", "Tenant Name" } } },
            { "tenant_code", new() { { "zh-CN", "租户编码" }, { "en-US", "Tenant Code" } } },
            { "contact_name", new() { { "zh-CN", "联系人" }, { "en-US", "Contact Name" } } },
            { "contact_phone", new() { { "zh-CN", "联系电话" }, { "en-US", "Contact Phone" } } },
        };
    }
}
