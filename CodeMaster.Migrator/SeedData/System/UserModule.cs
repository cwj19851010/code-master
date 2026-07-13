using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.System;

/// <summary>
/// 用户管理模块
/// </summary>
public class UserModule : ISeedModule
{
    public string ModuleName => "用户";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "user" && m.Component == "system/user/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找系统管理目录
        var systemMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "system" && m.MenuType == "M");

        if (systemMenu == null)
        {
            Console.WriteLine("  - 警告：未找到系统管理目录，跳过用户管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: systemMenu.Id,
            menuName: "用户",
            titleKey: "user",
            path: "user",
            component: "system/user/index",
            icon: "User",
            orderNum: 1,
            permsPrefix: "system:user"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加用户管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        // 用户数据由 BaseDataModule 创建
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "user", new() { { "zh-CN", "用户" }, { "en-US", "User" } } },
            { "username", new() { { "zh-CN", "用户名" }, { "en-US", "Username" } } },
            { "nickname", new() { { "zh-CN", "昵称" }, { "en-US", "Nickname" } } },
            { "email", new() { { "zh-CN", "邮箱" }, { "en-US", "Email" } } },
            { "phone", new() { { "zh-CN", "手机号" }, { "en-US", "Phone" } } },
            { "gender", new() { { "zh-CN", "性别" }, { "en-US", "Gender" } } },
            { "gender_male", new() { { "zh-CN", "男" }, { "en-US", "Male" } } },
            { "gender_female", new() { { "zh-CN", "女" }, { "en-US", "Female" } } },
            { "gender_unknown", new() { { "zh-CN", "未知" }, { "en-US", "Unknown" } } },
            { "dept", new() { { "zh-CN", "部门" }, { "en-US", "Department" } } },
            { "post", new() { { "zh-CN", "职位" }, { "en-US", "Post" } } },
            { "role", new() { { "zh-CN", "角色" }, { "en-US", "Role" } } },
        };
    }
}
