using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.Monitor;

/// <summary>
/// 登录日志模块
/// </summary>
public class LoginLogModule : ISeedModule
{
    public string ModuleName => "登录日志";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "loginlog" && m.Component == "monitor/loginlog/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找监控管理目录
        var monitorMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "monitor" && m.MenuType == "M");

        if (monitorMenu == null)
        {
            Console.WriteLine("  - 警告：未找到监控管理目录，跳过登录日志菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: monitorMenu.Id,
            menuName: "登录日志",
            titleKey: "loginlog",
            path: "loginlog",
            component: "monitor/loginlog/index",
            icon: "Key",
            orderNum: 2,
            permsPrefix: "system:loginlog"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加登录日志菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "loginlog", new() { { "zh-CN", "登录日志" }, { "en-US", "Login Log" } } },
            { "login_name", new() { { "zh-CN", "登录账号" }, { "en-US", "Login Name" } } },
            { "login_time", new() { { "zh-CN", "登录时间" }, { "en-US", "Login Time" } } },
            { "login_ip", new() { { "zh-CN", "登录IP" }, { "en-US", "Login IP" } } },
            { "login_location", new() { { "zh-CN", "登录地点" }, { "en-US", "Login Location" } } },
            { "browser", new() { { "zh-CN", "浏览器" }, { "en-US", "Browser" } } },
            { "os", new() { { "zh-CN", "操作系统" }, { "en-US", "OS" } } },
            { "login_status", new() { { "zh-CN", "登录状态" }, { "en-US", "Login Status" } } },
            { "login_message", new() { { "zh-CN", "提示消息" }, { "en-US", "Message" } } },
        };
    }
}
