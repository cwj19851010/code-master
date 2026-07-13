using CodeMaster.Migrator.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace CodeMaster.Migrator.SeedData.Monitor;

/// <summary>
/// 定时任务模块
/// </summary>
public class TaskModule : ISeedModule
{
    public string ModuleName => "定时任务";

    public async Task<bool> HasMenuAsync(CodeMasterDbContext dbContext)
    {
        return await dbContext.Set<Domain.Entities.System.SysMenu>()
            .AnyAsync(m => m.Path == "task" && m.Component == "monitor/task/index");
    }

    public async Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId)
    {
        // 查找系统监控目录
        var monitorMenu = await dbContext.Set<Domain.Entities.System.SysMenu>()
            .FirstOrDefaultAsync(m => m.Path == "monitor" && m.MenuType == "M");

        if (monitorMenu == null)
        {
            Console.WriteLine("  - 警告：未找到系统监控目录，跳过定时任务菜单");
            return;
        }

        // 创建标准 CRUD 菜单
        var menuIds = await MenuBuilder.CreateStandardMenuAsync(
            dbContext,
            parentId: monitorMenu.Id,
            menuName: "定时任务",
            titleKey: "task",
            path: "task",
            component: "monitor/task/index",
            icon: "Timer",
            orderNum: 4,
            permsPrefix: "monitor:task"
        );

        // 分配权限
        await MenuBuilder.AssignMenusToRoleAsync(dbContext, adminRoleId, menuIds);

        Console.WriteLine($"  - 添加定时任务菜单（{menuIds.Count}个）");
    }

    public Task AddInitialDataAsync(CodeMasterDbContext dbContext)
    {
        // 定时任务数据由用户创建
        return Task.CompletedTask;
    }

    public Dictionary<string, Dictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            { "task", new() { { "zh-CN", "定时任务" }, { "en-US", "Scheduled Task" } } },
            { "task_name", new() { { "zh-CN", "任务名称" }, { "en-US", "Task Name" } } },
            { "task_group", new() { { "zh-CN", "任务组" }, { "en-US", "Task Group" } } },
            { "cron_expression", new() { { "zh-CN", "Cron表达式" }, { "en-US", "Cron Expression" } } },
            { "task_type", new() { { "zh-CN", "任务类型" }, { "en-US", "Task Type" } } },
            { "assembly_name", new() { { "zh-CN", "程序集名称" }, { "en-US", "Assembly Name" } } },
            { "class_name", new() { { "zh-CN", "类名" }, { "en-US", "Class Name" } } },
            { "http_url", new() { { "zh-CN", "HTTP地址" }, { "en-US", "HTTP URL" } } },
            { "http_method", new() { { "zh-CN", "HTTP方法" }, { "en-US", "HTTP Method" } } },
            { "sql_text", new() { { "zh-CN", "SQL语句" }, { "en-US", "SQL Text" } } },
        };
    }
}
