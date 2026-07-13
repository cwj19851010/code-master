using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.CodeGen;

/// <summary>
/// 实体管理模块
/// </summary>
public class ModuleEntityModule : ISeedModule
{
    public string ModuleName => "代码管理-实体管理";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "moduleEntity" && m.Component == "codegen/moduleEntity/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找代码管理目录
        var codegenMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/codegen" && m.MenuType == "M");

        if (codegenMenu == null)
        {
            Console.WriteLine("  - 警告：未找到代码管理目录，跳过实体管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: codegenMenu.Id,
            menuName: "代码管理-实体管理",
            titleKey: "entity",
            path: "moduleEntity",
            component: "codegen/moduleEntity/index",
            icon: "Document",
            orderNum: 3,
            permsPrefix: "codegen:moduleEntity"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加实体管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "entity", new() { { "zh-CN", "实体管理" }, { "en-US", "Entity" } } },
            { "entity_name", new() { { "zh-CN", "实体名称" }, { "en-US", "Entity Name" } } },
            { "table_name", new() { { "zh-CN", "表名" }, { "en-US", "Table Name" } } },
        };
    }
}
