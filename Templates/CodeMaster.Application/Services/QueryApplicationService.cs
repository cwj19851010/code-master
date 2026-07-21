using Mapster;
using SqlSugar;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;

namespace CodeMaster.Application.Services;

public abstract class QueryApplicationService<TEntity, TGetListOutputDto, TGetListInput>
    : IQueryApplicationService<TEntity, TGetListOutputDto, TGetListInput>
    where TEntity : class, IBaseEntity, new()
    where TGetListOutputDto : class
    where TGetListInput : PagedQueryDto
{
    protected readonly IReadOnlyRepository<TEntity> Repository;
    protected readonly IExcelService? ExcelService;
    protected readonly Core.Services.ICacheService? CacheService;

    protected QueryApplicationService(
        IReadOnlyRepository<TEntity> repository,
        IExcelService? excelService = null,
        Core.Services.ICacheService? cacheService = null)
    {
        Repository = repository;
        ExcelService = excelService;
        CacheService = cacheService;
    }

    protected virtual string CacheKeyPrefix => typeof(TEntity).Name.ToLowerInvariant();
    protected virtual bool EnableCache => CacheService != null;

    public virtual async Task<PagedResultDto<TGetListOutputDto>> GetPagedListAsync(TGetListInput input)
    {
        var queryable = ApplySorting(await CreateFilteredQueryAsync(input), input);
        var total = await queryable.CountAsync();
        var items = await queryable
            .Skip((input.PageNum - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToListAsync();

        return new PagedResultDto<TGetListOutputDto>
        {
            Items = items.Adapt<List<TGetListOutputDto>>(),
            Total = total,
            PageNum = input.PageNum,
            PageSize = input.PageSize
        };
    }

    public virtual async Task<List<TGetListOutputDto>> GetListAsync(TGetListInput input)
    {
        var queryable = ApplySorting(await CreateFilteredQueryAsync(input), input);
        return (await queryable.ToListAsync()).Adapt<List<TGetListOutputDto>>();
    }

    protected virtual Task<ISugarQueryable<TEntity>> CreateFilteredQueryAsync(TGetListInput input) =>
        Task.FromResult(Repository.GetQueryable());

    protected virtual ISugarQueryable<TEntity> ApplySorting(ISugarQueryable<TEntity> queryable, TGetListInput input) =>
        queryable;

    public virtual async Task<byte[]> ExportAsync(TGetListInput input)
    {
        if (ExcelService == null)
            throw new InvalidOperationException("Excel service is not configured.");
        return await ExcelService.ExportAsync(await GetListAsync(input));
    }

    protected virtual async Task InvalidateCacheAsync(long id)
    {
        if (!EnableCache) return;
        await CacheService!.RemoveAsync($"{CacheKeyPrefix}:{id}");
        await InvalidateListCacheAsync();
    }

    protected virtual async Task InvalidateListCacheAsync()
    {
        if (EnableCache)
            await CacheService!.RemoveByPatternAsync($"{CacheKeyPrefix}:list:*");
    }

    protected virtual async Task InvalidateAllCacheAsync()
    {
        if (EnableCache)
            await CacheService!.RemoveByPatternAsync($"{CacheKeyPrefix}:*");
    }
}
