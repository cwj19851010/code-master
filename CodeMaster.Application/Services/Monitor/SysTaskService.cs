using CodeMaster.Application.Dtos.Monitor;
using CodeMaster.Core.Authorization;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.DynamicApi;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.Monitor;
using CodeMaster.Infrastructure.TaskScheduling;
using Mapster;
using SqlSugar;

namespace CodeMaster.Application.Services.Monitor;

/// <summary>
/// 任务服务接口
/// </summary>
public interface ISysTaskService : IApplicationService
{
    Task<SysTaskDto?> GetByIdAsync(long id);
    Task<PagedResultDto<SysTaskDto>> GetPagedListAsync(SysTaskQueryDto query);
    Task<long> CreateAsync(CreateSysTaskDto dto);
    Task<int> UpdateAsync(long id, UpdateSysTaskDto dto);
    Task<int> DeleteAsync(long id);
    Task<bool> StartAsync(long id);
    Task<bool> PauseAsync(long id);
    Task<bool> ResumeAsync(long id);
    Task<bool> RunOnceAsync(long id);
}

/// <summary>
/// 任务服务实现
/// </summary>
public class SysTaskService : ISysTaskService
{
    private readonly IRepository<SysTask> _taskRepository;
    private readonly ISqlSugarClient _db;
    private readonly ITaskSchedulerServer _schedulerServer;

    public SysTaskService(
        IRepository<SysTask> taskRepository,
        ISqlSugarClient db,
        ITaskSchedulerServer schedulerServer)
    {
        _taskRepository = taskRepository;
        _db = db;
        _schedulerServer = schedulerServer;
    }

    [Permission("system:task:view")]
    public async Task<SysTaskDto?> GetByIdAsync(long id)
    {
        var entity = await _taskRepository.GetByIdAsync(id);
        return entity?.Adapt<SysTaskDto>();
    }

    [Permission("system:task:list")]
    public async Task<PagedResultDto<SysTaskDto>> GetPagedListAsync(SysTaskQueryDto query)
    {
        RefAsync<int> total = 0;
        var items = await _db.Queryable<SysTask>()
            .WhereIF(!string.IsNullOrEmpty(query.TaskName), t => t.TaskName.Contains(query.TaskName!))
            .WhereIF(!string.IsNullOrEmpty(query.JobGroup), t => t.JobGroup == query.JobGroup)
            .WhereIF(query.TaskType != null, t => t.TaskType == query.TaskType!.Value)
            .WhereIF(query.Status != null, t => t.Status == query.Status!.Value)
            .OrderBy(t => t.CreateTime, OrderByType.Desc)
            .ToPageListAsync(query.PageNum, query.PageSize, total);

        return new PagedResultDto<SysTaskDto>
        {
            Items = items.Adapt<List<SysTaskDto>>(),
            Total = total.Value,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    [Permission("system:task:create")]
    public async Task<long> CreateAsync(CreateSysTaskDto dto)
    {
        var task = dto.Adapt<SysTask>();
        task.CreateTime = DateTime.UtcNow;
        task.RunTimes = 0;

        var id = await _taskRepository.InsertAsync(task);

        // 如果状态为启动，则添加到调度器
        if (task.Status == 0)
        {
            task.Id = id;
            await _schedulerServer.AddTaskScheduleAsync(task);
        }

        return id;
    }

    [Permission("system:task:update")]
    public async Task<int> UpdateAsync(long id, UpdateSysTaskDto dto)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new Exception($"任务ID {id} 不存在");
        }

        // 先从调度器中删除
        await _schedulerServer.DeleteTaskScheduleAsync(task);

        dto.Adapt(task);
        task.UpdateTime = DateTime.UtcNow;

        var result = await _taskRepository.UpdateAsync(task);

        // 如果状态为启动，则重新添加到调度器
        if (task.Status == 0)
        {
            await _schedulerServer.AddTaskScheduleAsync(task);
        }

        return result;
    }

    [Permission("system:task:delete")]
    public async Task<int> DeleteAsync(long id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task != null)
        {
            // 先从调度器中删除
            await _schedulerServer.DeleteTaskScheduleAsync(task);
        }

        return await _taskRepository.DeleteAsync(id);
    }

    [Permission("system:task:start")]
    [DynamicApi(HttpMethod = "POST", Route = "start/{id}")]
    public async Task<bool> StartAsync(long id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new Exception($"任务ID {id} 不存在");
        }

        var result = await _schedulerServer.AddTaskScheduleAsync(task);
        if (result.Success)
        {
            task.Status = 0;
            await _taskRepository.UpdateAsync(task);
        }

        return result.Success;
    }

    [Permission("system:task:pause")]
    [DynamicApi(HttpMethod = "POST", Route = "pause/{id}")]
    public async Task<bool> PauseAsync(long id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new Exception($"任务ID {id} 不存在");
        }

        var result = await _schedulerServer.PauseTaskScheduleAsync(task);
        if (result.Success)
        {
            task.Status = 1;
            await _taskRepository.UpdateAsync(task);
        }

        return result.Success;
    }

    [Permission("system:task:resume")]
    [DynamicApi(HttpMethod = "POST", Route = "resume/{id}")]
    public async Task<bool> ResumeAsync(long id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new Exception($"任务ID {id} 不存在");
        }

        var result = await _schedulerServer.ResumeTaskScheduleAsync(task);
        if (result.Success)
        {
            task.Status = 0;
            await _taskRepository.UpdateAsync(task);
        }

        return result.Success;
    }

    [Permission("system:task:run")]
    [DynamicApi(HttpMethod = "POST", Route = "run/{id}")]
    public async Task<bool> RunOnceAsync(long id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new Exception($"任务ID {id} 不存在");
        }

        var result = await _schedulerServer.RunTaskScheduleAsync(task);
        return result.Success;
    }
}
