using CodeMaster.Domain.Entities.System;
using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Yitter.IdGenerator;

namespace CodeMaster.Migrator.SeedData;

/// <summary>
/// 菜单构建辅助类
/// </summary>
public static class MenuBuilder
{
    /// <summary>
    /// 创建标准的 CRUD 菜单结构（目录 -> 列表页 -> add/edit/detail 子页面）
    /// </summary>
    public static async Task<List<long>> CreateStandardMenuAsync(
        CodeMasterDbContext dbContext,
        long parentId,
        string menuName,
        string titleKey,
        string path,
        string component,
        string icon,
        int orderNum,
        string permsPrefix,
        int menuScope = 2)
    {
        var menuIds = new List<long>();

        // 1. 列表页
        var listMenu = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            ParentId = parentId,
            MenuName = menuName,
            TitleKey = titleKey,
            Path = path,
            Component = component,
            MenuType = "C",
            Visible = true,
            Status = 0,
            IsCache = true, // 默认缓存
            Icon = icon,
            OrderNum = orderNum,
            Perms = $"{permsPrefix}:list",
            MenuScope = menuScope,
            CreateTime = DateTime.UtcNow
        };
        await dbContext.Set<SysMenu>().AddAsync(listMenu);
        menuIds.Add(listMenu.Id);

        // 2. 子页面（add/edit/detail）
        // 移除菜单名称中的"管理"后缀，避免出现"新增用户管理"这样的冗余
        var cleanMenuName = menuName.Replace("管理", "");
        var subPages = new[]
        {
            new { Path = "add", Name = $"新增{cleanMenuName}", Perms = $"{permsPrefix}:create" },
            new { Path = "edit", Name = $"编辑{cleanMenuName}", Perms = $"{permsPrefix}:update" },
            new { Path = "detail", Name = $"{cleanMenuName}详情", Perms = $"{permsPrefix}:view" }
        };

        foreach (var page in subPages)
        {
            var menu = new SysMenu
            {
                Id = YitIdHelper.NextId(),
                ParentId = listMenu.Id,
                MenuName = page.Name,
                Path = page.Path,
                Component = $"{component.Replace("/index", "")}/{page.Path}",
                MenuType = "C",
                Visible = false, // 隐藏
                Status = 0,
                IsCache = page.Path != "edit" && page.Path != "detail",
                Perms = page.Perms,
                MenuScope = menuScope,
                CreateTime = DateTime.UtcNow
            };
            await dbContext.Set<SysMenu>().AddAsync(menu);
            menuIds.Add(menu.Id);
        }

        return menuIds;
    }

    /// <summary>
    /// 创建目录菜单
    /// </summary>
    public static async Task<long> CreateDirectoryAsync(
        CodeMasterDbContext dbContext,
        string menuName,
        string titleKey,
        string path,
        string icon,
        int orderNum,
        long? parentId = null,
        int menuScope = 2)
    {
        var menu = new SysMenu
        {
            Id = YitIdHelper.NextId(),
            ParentId = parentId,
            MenuName = menuName,
            TitleKey = titleKey,
            Path = path,
            Component = "Layout",
            MenuType = "M",
            Visible = true,
            Status = 0,
            Icon = icon,
            OrderNum = orderNum,
            MenuScope = menuScope,
            CreateTime = DateTime.UtcNow
        };
        await dbContext.Set<SysMenu>().AddAsync(menu);
        return menu.Id;
    }

    /// <summary>
    /// 为角色分配菜单权限
    /// </summary>
    public static async Task AssignMenusToRoleAsync(
        CodeMasterDbContext dbContext,
        long roleId,
        List<long> menuIds)
    {
        foreach (var menuId in menuIds)
        {
            var exists = await dbContext.Set<SysRoleMenu>()
                .AnyAsync(rm => rm.RoleId == roleId && rm.MenuId == menuId);

            if (!exists)
            {
                await dbContext.Set<SysRoleMenu>().AddAsync(new SysRoleMenu
                {
                    RoleId = roleId,
                    MenuId = menuId
                });
            }
        }
    }

    /// <summary>
    /// 添加翻译文本（幂等）
    /// </summary>
    public static async Task AddTranslationsAsync(
        CodeMasterDbContext dbContext,
        Dictionary<string, Dictionary<string, string>> translations,
        string category = "common")
    {
        var existingTexts = await dbContext.Set<SysLangText>()
            .Where(t => t.LangCode == "zh-CN" || t.LangCode == "en-US")
            .Select(t => new { t.LangCode, t.LangKey })
            .ToListAsync();

        var existingSet = new HashSet<string>(
            existingTexts.Select(t => $"{t.LangCode}:{t.LangKey}"),
            StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var addedCount = 0;

        foreach (var (key, values) in translations)
        {
            foreach (var (langCode, langValue) in values)
            {
                var compositeKey = $"{langCode}:{key}";
                if (!existingSet.Contains(compositeKey))
                {
                    await dbContext.Set<SysLangText>().AddAsync(new SysLangText
                    {
                        Id = YitIdHelper.NextId(),
                        LangCode = langCode,
                        LangKey = key,
                        LangValue = langValue,
                        Category = category,
                        CreateTime = now
                    });
                    addedCount++;
                }
            }
        }

        if (addedCount > 0)
        {
            Console.WriteLine($"  - 添加翻译文本: {addedCount} 条");
        }
    }
}
