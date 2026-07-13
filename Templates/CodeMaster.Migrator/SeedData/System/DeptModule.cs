using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.System;

/// <summary>
/// 部门管理模块
/// </summary>
public class DeptModule : ISeedModule
{
    public string ModuleName => "部门管理";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "dept" && m.Component == "system/dept/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找系统管理目录
        var systemMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/system" && m.MenuType == "M");

        if (systemMenu == null)
        {
            Console.WriteLine("  - 警告：未找到系统管理目录，跳过部门管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: systemMenu.Id,
            menuName: "部门管理",
            titleKey: "dept",
            path: "dept",
            component: "system/dept/index",
            icon: "OfficeBuilding",
            orderNum: 3,
            permsPrefix: "system:dept"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加部门管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        // 部门数据由其他模块创建
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "dept", new() { { "zh-CN", "部门管理" }, { "en-US", "Department" } } },
            { "dept_name", new() { { "zh-CN", "部门名称" }, { "en-US", "Department Name" } } },
            { "parent_dept", new() { { "zh-CN", "上级部门" }, { "en-US", "Parent Department" } } },
            { "leader", new() { { "zh-CN", "负责人" }, { "en-US", "Leader" } } },
        };
    }
}
