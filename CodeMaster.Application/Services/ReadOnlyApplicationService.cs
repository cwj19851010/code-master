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
    where TGetOutputDto : class
    where TGetListOutputDto : class
    where TGetListInput : PagedQueryDto
{
    protected readonly IReadOnlyRepository<TEntity> Repository;
    protected readonly IExcelService? ExcelService;
    protected readonly Core.Services.ICacheService? CacheService;

    protected ReadOnlyApplicationService(
        IReadOnlyRepository<TEntity> repository,
        IExcelService? excelService = null,
        Core.Services.ICacheService? cacheService = null)
    {
        Repository = repository;
        ExcelService = excelService;
        CacheService = cacheService;
    }

    /// <summary>
    /// 获取缓存键前缀（子类可重写自定义缓存键）
    /// </summary>
    protected virtual string CacheKeyPrefix => typeof(TEntity).Name.ToLower();

    /// <summary>
    /// 是否启用缓存（子类可重写控制是否使用缓存）
    /// </summary>
    protected virtual bool EnableCache => CacheService != null;

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

    /// <summary>
    /// 导出数据到 Excel
    /// </summary>
    public virtual async Task<byte[]> ExportAsync(TGetListInput input)
    {
        // 获取所有数据（不分页）
        var data = await GetListAsync(input);

        // 使用 ExcelService 导出
        return await ExcelService.ExportAsync(data);
    }

    /// <summary>
    /// 清除单个实体缓存
    /// </summary>
    protected virtual async Task InvalidateCacheAsync(long id)
    {
        if (!EnableCache) return;

        await CacheService!.RemoveAsync($"{CacheKeyPrefix}:{id}");
        await InvalidateListCacheAsync();
    }

    /// <summary>
    /// 清除列表缓存
    /// </summary>
    protected virtual async Task InvalidateListCacheAsync()
    {
        if (!EnableCache) return;

        await CacheService!.RemoveByPatternAsync($"{CacheKeyPrefix}:list:*");
    }

    /// <summary>
    /// 清除所有相关缓存
    /// </summary>
    protected virtual async Task InvalidateAllCacheAsync()
    {
        if (!EnableCache) return;

        await CacheService!.RemoveByPatternAsync($"{CacheKeyPrefix}:*");
    }
}
