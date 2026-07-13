using CodeMaster.Domain.Entities.System;
using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Yitter.IdGenerator;

namespace CodeMaster.Migrator.SeedData.Core;

/// <summary>
/// 语言模块：语言基础数据（zh-CN, en-US）
/// </summary>
public class LangModule : ISeedModule
{
    public string ModuleName => "语言管理";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<SysMenu>()
            .AnyAsync(m => m.Path == "lang" && m.Component == "system/lang/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找系统管理目录
        var systemMenu = await dbContext.Set<SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/system" && m.MenuType == "M");

        if (systemMenu == null)
        {
            Console.WriteLine("  - 警告：未找到系统管理目录，跳过语言管理菜单");
            return;
        }

        // 1. 语言管理列表页
        var langListMenu = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            ParentId = systemMenu.Id,
            MenuName = "语言管理",
            TitleKey = "lang",
            Path = "lang",
            Component = "system/lang/index",
            MenuType = "C",
            Visible = 0,
            Status = 0,
            IsCache = 1, // 默认缓存
            Icon = "Reading",
            OrderNum = 8,
            Perms = "system:lang:list",
            CreateTime = DateTime.UtcNow
        };
        await dbContext.Set<SysMenu>().AddAsync(langListMenu);

        // 2. 子页面（隐藏路由）
        var subPages = new[]
        {
            new { Path = "add", Name = "新增语言", Perms = "system:lang:create" },
            new { Path = "edit", Name = "编辑语言", Perms = "system:lang:update" },
            new { Path = "detail", Name = "语言详情", Perms = "system:lang:view" }
        };

        var menuIds = new List<long> { langListMenu.Id };

        foreach (var page in subPages)
        {
            var menu = new SysMenu
            {
                Id = YitIdHelper.NextId(),
                ParentId = langListMenu.Id,
                MenuName = page.Name,
                Path = page.Path,
                Component = $"system/lang/{page.Path}",
                MenuType = "C",
                Visible = 1, // 隐藏
                Status = 0,
                IsCache = 1, // 默认缓存
                Perms = page.Perms,
                CreateTime = DateTime.UtcNow
            };
            await dbContext.Set<SysMenu>().AddAsync(menu);
            menuIds.Add(menu.Id);
        }

        // 3. 文本管理子页面（隐藏路由）
        var textSubPages = new[]
        {
            new { Path = "text/add", Name = "新增文本", Perms = "system:lang:text:create" },
            new { Path = "text/edit", Name = "编辑文本", Perms = "system:lang:text:update" },
            new { Path = "text/detail", Name = "文本详情", Perms = "system:lang:text:view" }
        };

        foreach (var page in textSubPages)
        {
            var menu = new SysMenu
            {
                Id = YitIdHelper.NextId(),
                ParentId = langListMenu.Id,
                MenuName = page.Name,
                Path = page.Path,
                Component = $"system/lang/{page.Path}",
                MenuType = "C",
                Visible = 1, // 隐藏
                Status = 0,
                IsCache = 1, // 默认缓存
                Perms = page.Perms,
                CreateTime = DateTime.UtcNow
            };
            await dbContext.Set<SysMenu>().AddAsync(menu);
            menuIds.Add(menu.Id);
        }

        // 4. 关联角色权限
        foreach (var menuId in menuIds)
        {
            await dbContext.Set<SysRoleMenu>().AddAsync(new SysRoleMenu
            {
                RoleId = adminRoleId,
                MenuId = menuId
            });
        }

        Console.WriteLine($"  - 添加语言管理菜单（{menuIds.Count}个）");
    }

    public async Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        var now = DateTime.UtcNow;

        // 1. 中文
        var hasZhCN = await dbContext.Set<SysLang>().AnyAsync(l => l.LangCode == "zh-CN");
        if (!hasZhCN)
        {
            var isFirst = !await dbContext.Set<SysLang>().AnyAsync();
            await dbContext.Set<SysLang>().AddAsync(new SysLang
            {
                Id = YitIdHelper.NextId(),
                LangCode = "zh-CN",
                LangName = "简体中文",
                IsDefault = isFirst ? 1 : 0,
                IsEnabled = 1,
                Sort = 1,
                CreateTime = now,
                Remark = "默认中文"
            });
            Console.WriteLine("  - 创建语言: zh-CN");
        }

        // 2. 英文
        var hasEnUS = await dbContext.Set<SysLang>().AnyAsync(l => l.LangCode == "en-US");
        if (!hasEnUS)
        {
            await dbContext.Set<SysLang>().AddAsync(new SysLang
            {
                Id = YitIdHelper.NextId(),
                LangCode = "en-US",
                LangName = "English",
                IsDefault = 0,
                IsEnabled = 1,
                Sort = 2,
                CreateTime = now,
                Remark = "Default English"
            });
            Console.WriteLine("  - 创建语言: en-US");
        }

        await dbContext.SaveChangesAsync();
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "lang", new() { { "zh-CN", "语言" }, { "en-US", "Language" } } },
            { "lang_code", new() { { "zh-CN", "语言代码" }, { "en-US", "Language Code" } } },
            { "lang_name", new() { { "zh-CN", "语言名称" }, { "en-US", "Language Name" } } },
            { "is_default", new() { { "zh-CN", "默认语言" }, { "en-US", "Default" } } },
            { "is_enabled", new() { { "zh-CN", "启用状态" }, { "en-US", "Enabled" } } },
            { "text", new() { { "zh-CN", "文本" }, { "en-US", "Text" } } },
            { "key", new() { { "zh-CN", "键" }, { "en-US", "Key" } } },
            { "category", new() { { "zh-CN", "分类" }, { "en-US", "Category" } } },
        };
    }
}
