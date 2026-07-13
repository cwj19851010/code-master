using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.System;

/// <summary>
/// 角色管理模块
/// </summary>
public class RoleModule : ISeedModule
{
    public string ModuleName => "角色管理";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "role" && m.Component == "system/role/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找系统管理目录
        var systemMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/system" && m.MenuType == "M");

        if (systemMenu == null)
        {
            Console.WriteLine("  - 警告：未找到系统管理目录，跳过角色管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: systemMenu.Id,
            menuName: "角色管理",
            titleKey: "role",
            path: "role",
            component: "system/role/index",
            icon: "UserFilled",
            orderNum: 2,
            permsPrefix: "system:role"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加角色管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        // 角色数据由其他模块创建
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "role", new() { { "zh-CN", "角色管理" }, { "en-US", "Role" } } },
            { "role_name", new() { { "zh-CN", "角色名称" }, { "en-US", "Role Name" } } },
            { "role_key", new() { { "zh-CN", "角色标识" }, { "en-US", "Role Key" } } },
            { "role_sort", new() { { "zh-CN", "显示顺序" }, { "en-US", "Sort Order" } } },
        };
    }
}
