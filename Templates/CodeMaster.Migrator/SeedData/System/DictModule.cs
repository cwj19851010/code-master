using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.System;

/// <summary>
/// 字典管理模块
/// </summary>
public class DictModule : ISeedModule
{
    public string ModuleName => "字典管理";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "dict" && m.Component == "system/dict/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找系统管理目录
        var systemMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/system" && m.MenuType == "M");

        if (systemMenu == null)
        {
            Console.WriteLine("  - 警告：未找到系统管理目录，跳过字典管理菜单");
            return;
        }

        var menuIds = new List<long>();

        // 1. 字典管理列表页
        var listMenu = new Domain.Entities.System.SysMenu
        {
            Id = Yitter.IdGenerator.YitIdHelper.NextId(),
            ParentId = systemMenu.Id,
            MenuName = "字典管理",
            TitleKey = "dict",
            Path = "dict",
            Component = "system/dict/index",
            MenuType = "C",
            Visible = 0,
            Status = 0,
            Icon = "Collection",
            OrderNum = 7,
            Perms = "system:dict:list",
            CreateTime = DateTime.UtcNow
        };
        await dbContext.Set<Domain.Entities.System.SysMenu>().AddAsync(listMenu);
        menuIds.Add(listMenu.Id);

        // 2. 字典类型子页面
        var typePages = new[]
        {
            new { Path = "type/add", Name = "添加字典类型", Perms = "system:dict:type:create" },
            new { Path = "type/edit", Name = "编辑字典类型", Perms = "system:dict:type:update" },
            new { Path = "type/detail", Name = "字典类型详情", Perms = "system:dict:type:view" }
        };

        foreach (var page in typePages)
        {
            var menu = new Domain.Entities.System.SysMenu
            {
                Id = Yitter.IdGenerator.YitIdHelper.NextId(),
                ParentId = listMenu.Id,
                MenuName = page.Name,
                Path = page.Path,
                Component = $"system/dict/{page.Path}",
                MenuType = "C",
                Visible = 1, // 隐藏
                Status = 0,
                Perms = page.Perms,
                CreateTime = DateTime.UtcNow
            };
            await dbContext.Set<Domain.Entities.System.SysMenu>().AddAsync(menu);
            menuIds.Add(menu.Id);
        }

        // 3. 字典数据子页面
        var dataPages = new[]
        {
            new { Path = "data/add", Name = "添加字典数据", Perms = "system:dict:data:create" },
            new { Path = "data/edit", Name = "编辑字典数据", Perms = "system:dict:data:update" },
            new { Path = "data/detail", Name = "字典数据详情", Perms = "system:dict:data:view" }
        };

        foreach (var page in dataPages)
        {
            var menu = new Domain.Entities.System.SysMenu
            {
                Id = Yitter.IdGenerator.YitIdHelper.NextId(),
                ParentId = listMenu.Id,
                MenuName = page.Name,
                Path = page.Path,
                Component = $"system/dict/{page.Path}",
                MenuType = "C",
                Visible = 1, // 隐藏
                Status = 0,
                Perms = page.Perms,
                CreateTime = DateTime.UtcNow
            };
            await dbContext.Set<Domain.Entities.System.SysMenu>().AddAsync(menu);
            menuIds.Add(menu.Id);
        }

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加字典管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        // 字典数据由其他模块创建
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "dict", new() { { "zh-CN", "字典" }, { "en-US", "Dictionary" } } },
            { "dictName", new() { { "zh-CN", "字典名称" }, { "en-US", "Dictionary Name" } } },
            { "dictType", new() { { "zh-CN", "字典类型" }, { "en-US", "Dictionary Type" } } },
            { "dict_name", new() { { "zh-CN", "字典名称" }, { "en-US", "dict name" } } },
            { "dict_type", new() { { "zh-CN", "字典类型" }, { "en-US", "dict type" } } },
            { "dict_label", new() { { "zh-CN", "字典标签" }, { "en-US", "Dictionary Label" } } },
            { "dict_value", new() { { "zh-CN", "字典键值" }, { "en-US", "Dictionary Value" } } },
            { "add_dict_type", new() { { "zh-CN", "添加字典类型" }, { "en-US", "Add Dict Type" } } },
            { "add_dict_data", new() { { "zh-CN", "添加字典数据" }, { "en-US", "Add Dict Data" } } },
        };
    }
}
