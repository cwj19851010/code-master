using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.Monitor;

/// <summary>
/// 操作日志模块
/// </summary>
public class OperLogModule : ISeedModule
{
    public string ModuleName => "操作日志";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "operlog" && m.Component == "monitor/operlog/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找监控管理目录
        var monitorMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "monitor" && m.MenuType == "M");

        if (monitorMenu == null)
        {
            Console.WriteLine("  - 警告：未找到监控管理目录，跳过操作日志菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: monitorMenu.Id,
            menuName: "操作日志",
            titleKey: "operlog",
            path: "operlog",
            component: "monitor/operlog/index",
            icon: "Document",
            orderNum: 1,
            permsPrefix: "system:operlog"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加操作日志菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "operlog", new() { { "zh-CN", "操作日志" }, { "en-US", "Operation Log" } } },
            { "operator", new() { { "zh-CN", "操作人员" }, { "en-US", "Operator" } } },
            { "operation", new() { { "zh-CN", "操作" }, { "en-US", "Operation" } } },
            { "method", new() { { "zh-CN", "请求方法" }, { "en-US", "Method" } } },
            { "request_url", new() { { "zh-CN", "请求地址" }, { "en-US", "Request URL" } } },
            { "ip_address", new() { { "zh-CN", "IP地址" }, { "en-US", "IP Address" } } },
            { "location", new() { { "zh-CN", "操作地点" }, { "en-US", "Location" } } },
            { "oper_time", new() { { "zh-CN", "操作时间" }, { "en-US", "Operation Time" } } },
        };
    }
}
