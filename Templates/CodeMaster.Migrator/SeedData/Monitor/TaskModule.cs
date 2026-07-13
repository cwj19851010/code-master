using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.Monitor;

/// <summary>
/// 任务管理模块
/// </summary>
public class TaskModule : ISeedModule
{
    public string ModuleName => "任务管理";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "task" && m.Component == "monitor/task/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找监控管理目录
        var monitorMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "/monitor" && m.MenuType == "M");

        if (monitorMenu == null)
        {
            Console.WriteLine("  - 警告：未找到监控管理目录，跳过任务管理菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: monitorMenu.Id,
            menuName: "任务管理",
            titleKey: "task",
            path: "task",
            component: "monitor/task/index",
            icon: "Timer",
            orderNum: 4,
            permsPrefix: "monitor:task"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加任务管理菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "task", new() { { "zh-CN", "任务管理" }, { "en-US", "Task Management" } } },
            { "task_name", new() { { "zh-CN", "任务名称" }, { "en-US", "Task Name" } } },
            { "task_group", new() { { "zh-CN", "任务分组" }, { "en-US", "Task Group" } } },
            { "task_type", new() { { "zh-CN", "任务类型" }, { "en-US", "Task Type" } } },
            { "cron_expression", new() { { "zh-CN", "Cron表达式" }, { "en-US", "Cron Expression" } } },
            { "assembly_name", new() { { "zh-CN", "程序集名称" }, { "en-US", "Assembly Name" } } },
            { "class_name", new() { { "zh-CN", "类名" }, { "en-US", "Class Name" } } },
            { "request_url", new() { { "zh-CN", "请求地址" }, { "en-US", "Request URL" } } },
            { "request_method", new() { { "zh-CN", "请求方式" }, { "en-US", "Request Method" } } },
            { "sql_text", new() { { "zh-CN", "SQL语句" }, { "en-US", "SQL Text" } } },
            { "start_task", new() { { "zh-CN", "启动任务" }, { "en-US", "Start Task" } } },
            { "pause_task", new() { { "zh-CN", "暂停任务" }, { "en-US", "Pause Task" } } },
            { "run_task", new() { { "zh-CN", "执行任务" }, { "en-US", "Run Task" } } },
        };
    }
}
