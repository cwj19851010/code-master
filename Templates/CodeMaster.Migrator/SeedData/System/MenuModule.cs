using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.System;

/// <summary>
/// 菜单管理模块
/// </summary>
public class MenuModule : ISeedModule
{
    public string ModuleName => "菜单管理";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "menu" && m.Component == "system/menu/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找系统管理目录
        var systemMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/system" && m.MenuType == "M");

        if (systemMenu == null)
        {
            Console.WriteLine("  - 警告：未找到系统管理目录，跳过菜单管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: systemMenu.Id,
            menuName: "菜单管理",
            titleKey: "menu",
            path: "menu",
            component: "system/menu/index",
            icon: "Menu",
            orderNum: 4,
            permsPrefix: "system:menu"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加菜单管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        // 菜单数据由其他模块创建
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "menu", new() { { "zh-CN", "菜单管理" }, { "en-US", "Menu" } } },
            { "menu_name", new() { { "zh-CN", "菜单名称" }, { "en-US", "Menu Name" } } },
            { "menu_type", new() { { "zh-CN", "菜单类型" }, { "en-US", "Menu Type" } } },
            { "menu_type_dir", new() { { "zh-CN", "目录" }, { "en-US", "Directory" } } },
            { "menu_type_menu", new() { { "zh-CN", "菜单" }, { "en-US", "Menu" } } },
            { "menu_type_button", new() { { "zh-CN", "按钮" }, { "en-US", "Button" } } },
        };
    }
}
