using Microsoft.EntityFrameworkCore;
using CodeMaster.Domain.Entities.System;
using CodeMaster.Migrator.Persistence.EfCore;

namespace CodeMaster.Migrator;

public static class AddSubPageMenus
{
    public static async Task ExecuteAsync(CodeMasterDbContext dbContext)
    {
        Console.WriteLine("开始添加子页面菜单...");

        // 查找父菜单
        var deptMenu = await dbContext.Set<SysMenu>().FirstOrDefaultAsync(m => m.MenuName == "部门管理");
        var dictMenu = await dbContext.Set<SysMenu>().FirstOrDefaultAsync(m => m.MenuName == "字典管理");
        var langMenu = await dbContext.Set<SysMenu>().FirstOrDefaultAsync(m => m.MenuName == "语言管理");

        if (deptMenu == null || dictMenu == null || langMenu == null)
        {
            Console.WriteLine("错误：未找到父菜单");
            return;
        }

        // 部门管理子页面
        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = deptMenu.Id,
            MenuName = "部门新增",
            TitleKey = "dept_add",
            Path = "add",
            Component = "system/dept/add",
            MenuType = "C",
            Perms = "system:dept:create",
            Icon = "",
            OrderNum = 1,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "部门新增页面"
        });

        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = deptMenu.Id,
            MenuName = "部门编辑",
            TitleKey = "dept_edit",
            Path = "edit",
            Component = "system/dept/edit",
            MenuType = "C",
            Perms = "system:dept:update",
            Icon = "",
            OrderNum = 2,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "部门编辑页面"
        });

        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = deptMenu.Id,
            MenuName = "部门详情",
            TitleKey = "dept_detail",
            Path = "detail",
            Component = "system/dept/detail",
            MenuType = "C",
            Perms = "system:dept:query",
            Icon = "",
            OrderNum = 3,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "部门详情页面"
        });

        // 字典管理子页面 - 字典类型
        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = dictMenu.Id,
            MenuName = "字典类型新增",
            TitleKey = "dict_type_add",
            Path = "type/add",
            Component = "system/dict/type/add",
            MenuType = "C",
            Perms = "system:dict:create",
            Icon = "",
            OrderNum = 1,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "字典类型新增页面"
        });

        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = dictMenu.Id,
            MenuName = "字典类型编辑",
            TitleKey = "dict_type_edit",
            Path = "type/edit",
            Component = "system/dict/type/edit",
            MenuType = "C",
            Perms = "system:dict:update",
            Icon = "",
            OrderNum = 2,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "字典类型编辑页面"
        });

        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = dictMenu.Id,
            MenuName = "字典类型详情",
            TitleKey = "dict_type_detail",
            Path = "type/detail",
            Component = "system/dict/type/detail",
            MenuType = "C",
            Perms = "system:dict:query",
            Icon = "",
            OrderNum = 3,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "字典类型详情页面"
        });

        // 字典管理子页面 - 字典数据
        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = dictMenu.Id,
            MenuName = "字典数据新增",
            TitleKey = "dict_data_add",
            Path = "data/add",
            Component = "system/dict/data/add",
            MenuType = "C",
            Perms = "system:dict:create",
            Icon = "",
            OrderNum = 4,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "字典数据新增页面"
        });

        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = dictMenu.Id,
            MenuName = "字典数据编辑",
            TitleKey = "dict_data_edit",
            Path = "data/edit",
            Component = "system/dict/data/edit",
            MenuType = "C",
            Perms = "system:dict:update",
            Icon = "",
            OrderNum = 5,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "字典数据编辑页面"
        });

        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = dictMenu.Id,
            MenuName = "字典数据详情",
            TitleKey = "dict_data_detail",
            Path = "data/detail",
            Component = "system/dict/data/detail",
            MenuType = "C",
            Perms = "system:dict:query",
            Icon = "",
            OrderNum = 6,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "字典数据详情页面"
        });

        // 语言管理子页面
        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = langMenu.Id,
            MenuName = "语言新增",
            TitleKey = "lang_add",
            Path = "add",
            Component = "system/lang/add",
            MenuType = "C",
            Perms = "system:lang:create",
            Icon = "",
            OrderNum = 1,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "语言新增页面"
        });

        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = langMenu.Id,
            MenuName = "语言编辑",
            TitleKey = "lang_edit",
            Path = "edit",
            Component = "system/lang/edit",
            MenuType = "C",
            Perms = "system:lang:update",
            Icon = "",
            OrderNum = 2,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "语言编辑页面"
        });

        await AddMenuIfNotExistsAsync(dbContext, new SysMenu
        {
            ParentId = langMenu.Id,
            MenuName = "语言详情",
            TitleKey = "lang_detail",
            Path = "detail",
            Component = "system/lang/detail",
            MenuType = "C",
            Perms = "system:lang:query",
            Icon = "",
            OrderNum = 3,
            Visible = 0,
            Status = 0,
            IsCache = 0,
            MenuScope = 2,
            Remark = "语言详情页面"
        });

        await dbContext.SaveChangesAsync();
        Console.WriteLine("子页面菜单添加完成！");
    }

    private static async Task AddMenuIfNotExistsAsync(CodeMasterDbContext dbContext, SysMenu menu)
    {
        var exists = await dbContext.Set<SysMenu>()
            .AnyAsync(m => m.ParentId == menu.ParentId && m.Path == menu.Path);

        if (!exists)
        {
            await dbContext.Set<SysMenu>().AddAsync(menu);
            Console.WriteLine($"  添加菜单: {menu.MenuName}");
        }
        else
        {
            Console.WriteLine($"  跳过已存在的菜单: {menu.MenuName}");
        }
    }
}
