using CodeMaster.Application.Dtos.Monitor;
using CodeMaster.Core.Authorization;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.Monitor;
using SqlSugar;

namespace CodeMaster.Application.Services.Monitor;

/// <summary>
/// 任务日志服务接口
/// </summary>
public interface ISysTaskLogService : IApplicationService
{
    Task<PagedResultDto<SysTaskLogDto>> GetPagedListAsync(SysTaskLogQueryDto query);
    Task<SysTaskLogDto?> GetByIdAsync(long id);
    Task<long> InsertTaskLogAsync(SysTaskLog taskLog);
    Task<int> DeleteBatchAsync(long[] ids);
    Task<int> ClearAsync();
}

/// <summary>
/// 任务日志服务实现（只读服务 + 自定义方法）
/// </summary>
public class SysTaskLogService
    : ReadOnlyApplicationService<SysTaskLog, SysTaskLogDto, SysTaskLogDto, SysTaskLogQueryDto>,
      ISysTaskLogService
{
    private readonly IRepository<SysTaskLog> _repository;

    public SysTaskLogService(IRepository<SysTaskLog> repository) : base(repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 创建过滤查询（重写基类方法）
    /// </summary>
    protected override Task<ISugarQueryable<SysTaskLog>> CreateFilteredQueryAsync(SysTaskLogQueryDto input)
    {
        var queryable = (ISugarQueryable<SysTaskLog>)Repository.GetQueryable();

        queryable = queryable
            .WhereIF(input.TaskId != null, log => log.TaskId == input.TaskId.Value)
            .WhereIF(!string.IsNullOrEmpty(input.TaskName), log => log.TaskName.Contains(input.TaskName))
            .WhereIF(input.Status != null, log => log.Status == input.Status.Value)
            .WhereIF(input.BeginTime != null, log => log.CreateTime >= input.BeginTime.Value)
            .WhereIF(input.EndTime != null, log => log.CreateTime <= input.EndTime.Value);

        return Task.FromResult(queryable);
    }

    /// <summary>
    /// 应用排序（重写基类方法）
    /// </summary>
    protected override ISugarQueryable<SysTaskLog> ApplySorting(ISugarQueryable<SysTaskLog> queryable, SysTaskLogQueryDto input)
    {
        return queryable.OrderByDescending(log => log.CreateTime);
    }

    /// <summary>
    /// 根据ID获取任务日志详情（重写以添加到动态API）
    /// </summary>
    [Permission("system:tasklog:view")]
    public override async Task<SysTaskLogDto?> GetByIdAsync(long id)
    {
        return await base.GetByIdAsync(id);
    }

    /// <summary>
    /// 插入任务日志（系统内部调用，无需权限）
    /// </summary>
    public async Task<long> InsertTaskLogAsync(SysTaskLog taskLog)
    {
        return await _repository.InsertAsync(taskLog);
    }

    /// <summary>
    /// 批量删除（使用默认权限：monitor:tasklog:deletebatch）
    /// </summary>
    public async Task<int> DeleteBatchAsync(long[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            return 0;
        }

        int count = 0;
        foreach (var id in ids)
        {
            count += await _repository.DeleteAsync(id);
        }
        return count;
    }

    /// <summary>
    /// 清空日志（使用默认权限：monitor:tasklog:clear）
    /// </summary>
    public async Task<int> ClearAsync()
    {
        return await _repository.DeleteAsync(log => true);
    }
}
