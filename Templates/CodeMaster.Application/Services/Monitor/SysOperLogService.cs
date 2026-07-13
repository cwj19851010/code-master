using CodeMaster.Application.Dtos.Monitor;
using CodeMaster.Core.Authorization;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.Monitor;
using SqlSugar;

namespace CodeMaster.Application.Services.Monitor;

/// <summary>
/// 操作日志服务接口
/// </summary>
public interface ISysOperLogService : IApplicationService
{
    Task<PagedResultDto<SysOperLogDto>> GetPagedListAsync(SysOperLogQueryDto query);
    Task<SysOperLogDto?> GetByIdAsync(long id);
    Task<long> InsertOperLogAsync(SysOperLog operLog);
    Task<int> DeleteBatchAsync(long[] ids);
    Task<int> ClearAsync();
}

/// <summary>
/// 操作日志服务实现（只读服务 + 自定义方法）
/// </summary>
public class SysOperLogService
    : ReadOnlyApplicationService<SysOperLog, SysOperLogDto, SysOperLogDto, SysOperLogQueryDto>,
      ISysOperLogService
{
    private readonly IRepository<SysOperLog> _repository;

    public SysOperLogService(IRepository<SysOperLog> repository) : base(repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 创建过滤查询（重写基类方法）
    /// </summary>
    protected override Task<ISugarQueryable<SysOperLog>> CreateFilteredQueryAsync(SysOperLogQueryDto input)
    {
        var queryable = (ISugarQueryable<SysOperLog>)Repository.GetQueryable();

        queryable = queryable
            .WhereIF(!string.IsNullOrEmpty(input.Title), log => log.Title.Contains(input.Title))
            .WhereIF(!string.IsNullOrEmpty(input.OperName), log => log.OperName.Contains(input.OperName))
            .WhereIF(input.BusinessType != null, log => log.BusinessType == input.BusinessType.Value)
            .WhereIF(input.Status != null, log => log.Status == input.Status.Value)
            .WhereIF(input.BeginTime != null, log => log.OperTime >= input.BeginTime.Value)
            .WhereIF(input.EndTime != null, log => log.OperTime <= input.EndTime.Value);

        return Task.FromResult(queryable);
    }

    /// <summary>
    /// 应用排序（重写基类方法）
    /// </summary>
    protected override ISugarQueryable<SysOperLog> ApplySorting(ISugarQueryable<SysOperLog> queryable, SysOperLogQueryDto input)
    {
        return queryable.OrderByDescending(log => log.OperTime);
    }

    /// <summary>
    /// 根据ID获取操作日志详情（重写以添加到动态API）
    /// </summary>
    [Permission("monitor:operlog:view")]
    public override async Task<SysOperLogDto?> GetByIdAsync(long id)
    {
        return await base.GetByIdAsync(id);
    }

    /// <summary>
    /// 插入操作日志（系统内部调用，无需权限）
    /// </summary>
    public async Task<long> InsertOperLogAsync(SysOperLog operLog)
    {
        return await _repository.InsertAsync(operLog);
    }

    /// <summary>
    /// 批量删除（使用默认权限：monitor:operlog:deletebatch）
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
    /// 清空日志（使用默认权限：monitor:operlog:clear）
    /// </summary>
    public async Task<int> ClearAsync()
    {
        return await _repository.DeleteAsync(log => true);
    }
}
