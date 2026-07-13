using CodeMaster.Application.Dtos.System;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.System;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 职位服务接口
/// </summary>
public interface ISysPostService : IApplicationService
{
    // 使用默认权限规则：system:post:view
    Task<SysPostDto?> GetByIdAsync(long id);

    // 使用默认权限规则：system:post:list
    Task<PagedResultDto<SysPostDto>> GetPagedListAsync([FromQuery] SysPostQueryDto query);

    // 使用默认权限规则：system:post:create
    Task<long> CreateAsync([FromBody] CreateSysPostDto dto);

    // 使用默认权限规则：system:post:update
    Task<int> UpdateAsync(long id, [FromBody] UpdateSysPostDto dto);

    // 使用默认权限规则：system:post:delete
    Task<int> DeleteAsync(long id);

    // 使用默认权限规则：system:post:deletebatch
    Task<int> DeleteBatchAsync(List<long> ids);

    // 使用默认权限规则：system:post:list
    Task<List<SysPostDto>> GetAllList();
}

/// <summary>
/// 职位服务实现
/// </summary>
public class SysPostService
    : CrudApplicationService<SysPost, SysPostDto, SysPostDto, SysPostQueryDto, CreateSysPostDto, UpdateSysPostDto>,
      ISysPostService
{
    public SysPostService(IRepository<SysPost> repository) : base(repository)
    {
    }

    /// <summary>
    /// 创建过滤查询（重写基类方法，实现自定义查询条件）
    /// 类似 ABP vNext 的 CreateFilteredQuery
    /// </summary>
    protected override Task<ISugarQueryable<SysPost>> CreateFilteredQueryAsync(SysPostQueryDto input)
    {
        var queryable = (ISugarQueryable<SysPost>)Repository.GetQueryable();

        queryable = queryable.WhereIF(!string.IsNullOrEmpty(input.PostName), p => p.PostName.Contains(input.PostName));

        return Task.FromResult(queryable);
    }

    /// <summary>
    /// 获取所有职位列表（用于下拉选择）
    /// </summary>
    public async Task<List<SysPostDto>> GetAllList()
    {
        return await GetListAsync(new SysPostQueryDto { PageNum = 1, PageSize = int.MaxValue });
    }
}
