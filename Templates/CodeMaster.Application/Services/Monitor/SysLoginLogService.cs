using CodeMaster.Application.Dtos.Monitor;
using CodeMaster.Core.Authorization;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.Monitor;
using SqlSugar;

namespace CodeMaster.Application.Services.Monitor;

/// <summary>
/// 登录日志服务接口
/// </summary>
public interface ISysLoginLogService : IApplicationService
{
    Task<PagedResultDto<SysLoginLogDto>> GetPagedListAsync(SysLoginLogQueryDto query);
    Task<SysLoginLogDto?> GetByIdAsync(long id);
    Task<long> InsertLoginLogAsync(SysLoginLog loginLog);
    Task<int> DeleteBatchAsync(long[] ids);
    Task<int> ClearAsync();
}

/// <summary>
/// 登录日志服务实现（只读服务 + 自定义方法）
/// </summary>
public class SysLoginLogService
    : ReadOnlyApplicationService<SysLoginLog, SysLoginLogDto, SysLoginLogDto, SysLoginLogQueryDto>,
      ISysLoginLogService
{
    private readonly IRepository<SysLoginLog> _repository;

    public SysLoginLogService(IRepository<SysLoginLog> repository) : base(repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 创建过滤查询（重写基类方法）
    /// </summary>
    protected override Task<ISugarQueryable<SysLoginLog>> CreateFilteredQueryAsync(SysLoginLogQueryDto input)
    {
        var queryable = (ISugarQueryable<SysLoginLog>)Repository.GetQueryable();

        queryable = queryable
            .WhereIF(!string.IsNullOrEmpty(input.UserName), log => log.UserName.Contains(input.UserName))
            .WhereIF(!string.IsNullOrEmpty(input.LoginIp), log => log.LoginIp.Contains(input.LoginIp))
            .WhereIF(input.Status != null, log => log.Status == input.Status.Value)
            .WhereIF(input.BeginTime != null, log => log.LoginTime >= input.BeginTime.Value)
            .WhereIF(input.EndTime != null, log => log.LoginTime <= input.EndTime.Value);

        return Task.FromResult(queryable);
    }

    /// <summary>
    /// 应用排序（重写基类方法）
    /// </summary>
    protected override ISugarQueryable<SysLoginLog> ApplySorting(ISugarQueryable<SysLoginLog> queryable, SysLoginLogQueryDto input)
    {
        return queryable.OrderByDescending(log => log.LoginTime);
    }

    /// <summary>
    /// 根据ID获取登录日志详情（重写以添加到动态API）
    /// </summary>
    [Permission("monitor:loginlog:view")]
    public override async Task<SysLoginLogDto?> GetByIdAsync(long id)
    {
        return await base.GetByIdAsync(id);
    }

    /// <summary>
    /// 插入登录日志（系统内部调用，无需权限）
    /// </summary>
    public async Task<long> InsertLoginLogAsync(SysLoginLog loginLog)
    {
        return await _repository.InsertAsync(loginLog);
    }

    /// <summary>
    /// 批量删除（使用默认权限：monitor:loginlog:deletebatch）
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
    /// 清空日志（使用默认权限：monitor:loginlog:clear）
    /// </summary>
    public async Task<int> ClearAsync()
    {
        return await _repository.DeleteAsync(log => true);
    }
}
