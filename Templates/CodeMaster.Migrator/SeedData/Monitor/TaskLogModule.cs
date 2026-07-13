using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.Monitor;

/// <summary>
/// 任务日志模块
/// </summary>
public class TaskLogModule : ISeedModule
{
    public string ModuleName => "任务日志";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "tasklog" && m.Component == "monitor/tasklog/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找监控管理目录
        var monitorMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/monitor" && m.MenuType == "M");

        if (monitorMenu == null)
        {
            Console.WriteLine("  - 警告：未找到监控管理目录，跳过任务日志菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: monitorMenu.Id,
            menuName: "任务日志",
            titleKey: "tasklog",
            path: "tasklog",
            component: "monitor/tasklog/index",
            icon: "Tickets",
            orderNum: 5,
            permsPrefix: "monitor:tasklog"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加任务日志菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "tasklog", new() { { "zh-CN", "任务日志" }, { "en-US", "Task Log" } } },
            { "task_id", new() { { "zh-CN", "任务ID" }, { "en-US", "Task ID" } } },
            { "execute_time", new() { { "zh-CN", "执行时间" }, { "en-US", "Execute Time" } } },
            { "execute_duration", new() { { "zh-CN", "执行耗时" }, { "en-US", "Duration" } } },
            { "execute_status", new() { { "zh-CN", "执行状态" }, { "en-US", "Status" } } },
            { "execute_result", new() { { "zh-CN", "执行结果" }, { "en-US", "Result" } } },
            { "error_message", new() { { "zh-CN", "错误信息" }, { "en-US", "Error Message" } } },
        };
    }
}
