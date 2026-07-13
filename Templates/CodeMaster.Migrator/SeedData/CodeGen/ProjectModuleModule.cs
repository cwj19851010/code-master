using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.CodeGen;

/// <summary>
/// 模块管理模块
/// </summary>
public class ProjectModuleModule : ISeedModule
{
    public string ModuleName => "代码管理-模块管理";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "projectModule" && m.Component == "codegen/projectModule/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找代码管理目录
        var codegenMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/codegen" && m.MenuType == "M");

        if (codegenMenu == null)
        {
            Console.WriteLine("  - 警告：未找到代码管理目录，跳过模块管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: codegenMenu.Id,
            menuName: "代码管理-模块管理",
            titleKey: "module",
            path: "projectModule",
            component: "codegen/projectModule/index",
            icon: "Grid",
            orderNum: 2,
            permsPrefix: "codegen:projectModule"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加模块管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "module", new() { { "zh-CN", "模块管理" }, { "en-US", "Module" } } },
            { "module_name", new() { { "zh-CN", "模块名称" }, { "en-US", "Module Name" } } },
            { "module_code", new() { { "zh-CN", "模块代码" }, { "en-US", "Module Code" } } },
        };
    }
}
