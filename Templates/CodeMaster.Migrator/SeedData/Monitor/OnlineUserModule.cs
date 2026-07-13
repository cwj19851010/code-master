using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.Monitor;

/// <summary>
/// 在线用户模块
/// </summary>
public class OnlineUserModule : ISeedModule
{
    public string ModuleName => "在线用户";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "online" && m.Component == "monitor/online/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找监控管理目录
        var monitorMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/monitor" && m.MenuType == "M");

        if (monitorMenu == null)
        {
            Console.WriteLine("  - 警告：未找到监控管理目录，跳过在线用户菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: monitorMenu.Id,
            menuName: "在线用户",
            titleKey: "online",
            path: "online",
            component: "monitor/online/index",
            icon: "UserFilled",
            orderNum: 3,
            permsPrefix: "system:online"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加在线用户菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "online", new() { { "zh-CN", "在线用户" }, { "en-US", "Online User" } } },
            { "session_id", new() { { "zh-CN", "会话编号" }, { "en-US", "Session ID" } } },
            { "login_location", new() { { "zh-CN", "登录地点" }, { "en-US", "Login Location" } } },
            { "last_access_time", new() { { "zh-CN", "最后访问时间" }, { "en-US", "Last Access Time" } } },
            { "force_logout", new() { { "zh-CN", "强制退出" }, { "en-US", "Force Logout" } } },
        };
    }
}
