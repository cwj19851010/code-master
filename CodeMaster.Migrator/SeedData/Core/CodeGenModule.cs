using CodeMaster.Domain.Entities.System;
using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Yitter.IdGenerator;

namespace CodeMaster.Migrator.SeedData.Core;

/// <summary>
/// 模板配置菜单种子数据
/// </summary>
public class CodeGenModule : ISeedModule
{
    public string ModuleName => "代码生成";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<SysMenu>()
            .AnyAsync(m => m.Path == "templateConfig" && m.MenuType == "C");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        var codegenDir = await dbContext.Set<SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "codegen" && m.MenuType == "M");
        if (codegenDir == null) return;

        // 模板配置页面
        var menu = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            MenuName = "模板配置",
            TitleKey = "templateconfig",
            ParentId = codegenDir.Id,
            OrderNum = 10,
            Path = "templateConfig",
            Component = "codegen/templateConfig/index",
            MenuType = "C",
            Visible = true,
            IsCache = true,
            Status = 0,
            Perms = "codegen:templateConfig:list",
            Icon = "Operation",
            CreateTime = DateTime.UtcNow
        };
        await dbContext.Set<SysMenu>().AddAsync(menu);
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, new List<long> { menu.Id });

        // 实体设计器页面
        var designerMenu = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            MenuName = "实体设计器",
            TitleKey = "entitydesigner",
            ParentId = codegenDir.Id,
            OrderNum = 5,
            Path = "entityDesigner",
            Component = "codegen/entityDesigner/index",
            MenuType = "C",
            Visible = false,
            IsCache = false,
            Status = 0,
            Perms = "codegen:entityDesigner:list",
            Icon = "Edit",
            CreateTime = DateTime.UtcNow
        };
        await dbContext.Set<SysMenu>().AddAsync(designerMenu);
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, new List<long> { designerMenu.Id });

        // 查询权限按钮
        var queryBtn = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            MenuName = "查询",
            ParentId = menu.Id,
            OrderNum = 1,
            MenuType = "F",
            Visible = true,
            Status = 0,
            Perms = "codegen:templateConfig:list",
            CreateTime = DateTime.UtcNow
        };
        await dbContext.Set<SysMenu>().AddAsync(queryBtn);
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, new List<long> { queryBtn.Id });
    }

    public async Task AddInitialDataAsync(CodeMasterDbContext dbContext) => await Task.CompletedTask;

    public Dictionary<string, Dictionary<string, string>> GetTranslations() => new()
    {
        { "templateconfig", new() { { "zh-CN", "模板配置" }, { "en-US", "Template Config" } } },
        { "entitydesigner", new() { { "zh-CN", "实体设计器" }, { "en-US", "Entity Designer" } } },
    };
}
