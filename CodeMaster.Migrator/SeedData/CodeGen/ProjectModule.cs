using CodeMaster.Migrator.Persistence.EfCore;
using CodeMaster.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Yitter.IdGenerator;

namespace CodeMaster.Migrator.SeedData.CodeGen;

/// <summary>
/// 项目管理模块
/// </summary>
public class ProjectModule : ISeedModule
{
    public string ModuleName => "项目";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "project" && m.Component == "codegen/project/index" && m.MenuType == "C");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找代码管理目录
        var codegenMenu = await dbContext.Set<SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "codegen" && m.MenuType == "M");

        if (codegenMenu == null)
        {
            Console.WriteLine("  - 警告：未找到代码管理目录，跳过项目管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: codegenMenu.Id,
            menuName: "项目",
            titleKey: "project",
            path: "project",
            component: "codegen/project/index",
            icon: "Folder",
            orderNum: 1,
            permsPrefix: "codegen:project"
        );

        // 分配权限
        var listMenuId = menuIds[0];
        var actionMenus = new[]
        {
            new { Name = "Delete Project", Perms = "codegen:project:delete", OrderNum = 10 },
            new { Name = "Export Template", Perms = "codegen:project:export", OrderNum = 11 },
            new { Name = "Initialize Project", Perms = "codegen:project:initialize", OrderNum = 12 },
            new { Name = "Start Project", Perms = "codegen:project:start", OrderNum = 13 },
            new { Name = "Stop Project", Perms = "codegen:project:stop", OrderNum = 14 },
            new { Name = "Migrate Database", Perms = "codegen:project:migrate", OrderNum = 15 },
            new { Name = "Build Project", Perms = "codegen:project:build", OrderNum = 16 }
        };

        foreach (var action in actionMenus)
        {
            var menu = new SysMenu
            {
                Id = YitIdHelper.NextId(),
                ParentId = listMenuId,
                MenuName = action.Name,
                MenuType = "F",
                Visible = false,
                Status = 0,
                IsCache = false,
                OrderNum = action.OrderNum,
                Perms = action.Perms,
                MenuScope = 2,
                CreateTime = DateTime.UtcNow
            };

            await dbContext.Set<Domain.Entities.System.SysMenu>().AddAsync(menu);
            menuIds.Add(menu.Id);
        }

        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加项目管理菜单（{menuIds.Count}个）");
    }

    public async Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        await EnsureProjectMenusAsync(dbContext);
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "project", new() { { "zh-CN", "项目" }, { "en-US", "Project" } } },
            { "project_name", new() { { "zh-CN", "项目名称" }, { "en-US", "Project Name" } } },
            { "project_code", new() { { "zh-CN", "项目代码" }, { "en-US", "Project Code" } } },
            { "namespace", new() { { "zh-CN", "命名空间" }, { "en-US", "Namespace" } } },
            { "description", new() { { "zh-CN", "描述" }, { "en-US", "Description" } } },
        };
    }

    private static async Task EnsureProjectMenusAsync(CodeMasterDbContext dbContext)
    {
        var projectMenu = await dbContext.Set<SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "project"
                && m.Component == "codegen/project/index"
                && m.MenuType == "C");

        if (projectMenu == null)
        {
            return;
        }

        var changed = false;
        changed |= EnsureShared(projectMenu);

        var pageMenus = new[]
        {
            new
            {
                Path = "add",
                Name = "新增项目",
                Perms = "codegen:project:create",
                Component = "codegen/project/add",
                OrderNum = 1,
                IsCache = true
            },
            new
            {
                Path = "edit",
                Name = "编辑项目",
                Perms = "codegen:project:update",
                Component = "codegen/project/edit",
                OrderNum = 2,
                IsCache = false
            },
            new
            {
                Path = "detail",
                Name = "项目详情",
                Perms = "codegen:project:view",
                Component = "codegen/project/detail",
                OrderNum = 3,
                IsCache = false
            }
        };

        var projectMenuIds = new List<long> { projectMenu.Id };

        foreach (var page in pageMenus)
        {
            var menu = await dbContext.Set<SysMenu>()
                .FirstOrDefaultAsync(m => m.Perms == page.Perms);

            if (menu == null)
            {
                menu = new SysMenu
                {
                    Id = YitIdHelper.NextId(),
                    ParentId = projectMenu.Id,
                    MenuName = page.Name,
                    Path = page.Path,
                    Component = page.Component,
                    MenuType = "C",
                    Visible = false,
                    Status = 0,
                    IsCache = page.IsCache,
                    OrderNum = page.OrderNum,
                    Perms = page.Perms,
                    MenuScope = 2,
                    CreateTime = DateTime.UtcNow
                };
                await dbContext.Set<SysMenu>().AddAsync(menu);
                changed = true;
            }
            else
            {
                changed |= EnsureShared(menu);
                changed |= EnsureParent(menu, projectMenu.Id);
            }

            projectMenuIds.Add(menu.Id);
        }

        var actionMenus = new[]
        {
            new { Name = "Delete Project", Perms = "codegen:project:delete", OrderNum = 10 },
            new { Name = "Export Template", Perms = "codegen:project:export", OrderNum = 11 },
            new { Name = "Initialize Project", Perms = "codegen:project:initialize", OrderNum = 12 },
            new { Name = "Start Project", Perms = "codegen:project:start", OrderNum = 13 },
            new { Name = "Stop Project", Perms = "codegen:project:stop", OrderNum = 14 },
            new { Name = "Migrate Database", Perms = "codegen:project:migrate", OrderNum = 15 },
            new { Name = "Build Project", Perms = "codegen:project:build", OrderNum = 16 }
        };

        foreach (var action in actionMenus)
        {
            var menu = await dbContext.Set<SysMenu>()
                .FirstOrDefaultAsync(m => m.Perms == action.Perms);

            if (menu == null)
            {
                menu = new SysMenu
                {
                    Id = YitIdHelper.NextId(),
                    ParentId = projectMenu.Id,
                    MenuName = action.Name,
                    MenuType = "F",
                    Visible = false,
                    Status = 0,
                    IsCache = false,
                    OrderNum = action.OrderNum,
                    Perms = action.Perms,
                    MenuScope = 2,
                    CreateTime = DateTime.UtcNow
                };
                await dbContext.Set<SysMenu>().AddAsync(menu);
                changed = true;
            }
            else
            {
                changed |= EnsureShared(menu);
                changed |= EnsureParent(menu, projectMenu.Id);
            }

            projectMenuIds.Add(menu.Id);
        }

        var adminRole = await dbContext.Set<SysRole>()
            .FirstOrDefaultAsync(r => r.RoleKey == "admin");

        if (adminRole != null)
        {
            var existingMenuIds = await dbContext.Set<SysRoleMenu>()
                .Where(rm => rm.RoleId == adminRole.Id && projectMenuIds.Contains(rm.MenuId))
                .Select(rm => rm.MenuId)
                .ToListAsync();

            foreach (var menuId in projectMenuIds.Distinct().Except(existingMenuIds))
            {
                await dbContext.Set<SysRoleMenu>().AddAsync(new SysRoleMenu
                {
                    RoleId = adminRole.Id,
                    MenuId = menuId
                });
                changed = true;
            }
        }

        if (changed)
        {
            Console.WriteLine("  - 修复项目管理菜单按钮权限范围");
        }
    }

    private static bool EnsureShared(SysMenu menu)
    {
        if (menu.MenuScope == 2)
        {
            return false;
        }

        menu.MenuScope = 2;
        return true;
    }

    private static bool EnsureParent(SysMenu menu, long parentId)
    {
        if (menu.ParentId == parentId)
        {
            return false;
        }

        menu.ParentId = parentId;
        return true;
    }
}
