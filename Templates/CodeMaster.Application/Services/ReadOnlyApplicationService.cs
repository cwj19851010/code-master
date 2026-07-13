using Mapster;
using SqlSugar;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;

namespace CodeMaster.Application.Services;

/// <summary>
/// 只读应用服务基类
/// </summary>
public abstract class ReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>
    : IReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>
    where TEntity : class, IEntity<long>, new()
    where TGetListInput : PagedQueryDto
{
    protected readonly IReadOnlyRepository<TEntity> Repository;

    protected ReadOnlyApplicationService(IReadOnlyRepository<TEntity> repository)
    {
        Repository = repository;
    }

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    public virtual async Task<TGetOutputDto?> GetByIdAsync(long id)
    {
        var entity = await Repository.GetByIdAsync(id);
        return entity == null ? default : entity.Adapt<TGetOutputDto>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public virtual async Task<PagedResultDto<TGetListOutputDto>> GetPagedListAsync(TGetListInput input)
    {
        // 创建可查询对象
        var queryable = await CreateFilteredQueryAsync(input);

        // 应用排序
        queryable = ApplySorting(queryable, input);

        // 获取总数
        var total = await queryable.CountAsync();

        // 应用分页
        var items = await queryable
            .Skip((input.PageNum - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync();

        // 映射到DTO
        var dtos = items.Adapt<List<TGetListOutputDto>>();

        return new PagedResultDto<TGetListOutputDto>
        {
            Items = dtos,
            Total = total,
            PageNum = input.PageNum,
            PageSize = input.PageSize
        };
    }

    /// <summary>
    /// 获取所有列表（不分页，支持查询条件）
    /// </summary>
    public virtual async Task<List<TGetListOutputDto>> GetListAsync(TGetListInput input)
    {
        // 创建可查询对象
        var queryable = await CreateFilteredQueryAsync(input);

        // 应用排序
        queryable = ApplySorting(queryable, input);

        // 获取所有数据
        var entities = await queryable.ToListAsync();

        return entities.Adapt<List<TGetListOutputDto>>();
    }

    /// <summary>
    /// 创建过滤查询（子类重写此方法实现自定义查询条件）
    /// 类似 ABP vNext 的 CreateFilteredQuery
    /// </summary>
    protected virtual Task<ISugarQueryable<TEntity>> CreateFilteredQueryAsync(TGetListInput input)
    {
        var queryable = (ISugarQueryable<TEntity>)Repository.GetQueryable();
        return Task.FromResult(queryable);
    }

    /// <summary>
    /// 应用排序（子类可重写）
    /// </summary>
    protected virtual ISugarQueryable<TEntity> ApplySorting(ISugarQueryable<TEntity> queryable, TGetListInput input)
    {
        // 子类可以重写此方法实现自定义排序
        return queryable;
    }
}
