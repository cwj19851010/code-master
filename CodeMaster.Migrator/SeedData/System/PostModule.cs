using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.System;

/// <summary>
/// 职位管理模块
/// </summary>
public class PostModule : ISeedModule
{
    public string ModuleName => "职位";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "post" && m.Component == "system/post/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找系统管理目录
        var systemMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "system" && m.MenuType == "M");

        if (systemMenu == null)
        {
            Console.WriteLine("  - 警告：未找到系统管理目录，跳过职位管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: systemMenu.Id,
            menuName: "职位",
            titleKey: "post",
            path: "post",
            component: "system/post/index",
            icon: "Briefcase",
            orderNum: 6,
            permsPrefix: "system:post"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加职位管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        // 职位数据由其他模块创建
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "post", new() { { "zh-CN", "职位" }, { "en-US", "Post" } } },
            { "post_name", new() { { "zh-CN", "职位名称" }, { "en-US", "Post Name" } } },
            { "post_code", new() { { "zh-CN", "职位编码" }, { "en-US", "Post Code" } } },
            { "data_scope", new() { { "zh-CN", "数据权限" }, { "en-US", "Data Scope" } } },
        };
    }
}
