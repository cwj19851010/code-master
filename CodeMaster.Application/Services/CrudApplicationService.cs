using Mapster;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;

namespace CodeMaster.Application.Services;

/// <summary>
/// CRUD应用服务基类
/// </summary>
public abstract class CrudApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput, TCreateInput, TUpdateInput>
    : ReadOnlyApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput>,
      ICrudApplicationService<TEntity, TGetOutputDto, TGetListOutputDto, TGetListInput, TCreateInput, TUpdateInput>
    where TEntity : class, IEntity<long>, new()
    where TGetOutputDto : class
    where TGetListOutputDto : class
    where TGetListInput : PagedQueryDto
    where TCreateInput : class, new()
    where TUpdateInput : class
{
    protected new readonly IRepository<TEntity> Repository;

    protected CrudApplicationService(
        IRepository<TEntity> repository,
        IExcelService? excelService = null,
        Core.Services.ICacheService? cacheService = null)
        : base(repository, excelService, cacheService)
    {
        Repository = repository;
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    public virtual async Task<long> CreateAsync(TCreateInput input)
    {
        var entity = input.Adapt<TEntity>();
        var id = await Repository.InsertAsync(entity);

        // 清除列表缓存
        await InvalidateListCacheAsync();

        return id;
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public virtual async Task<int> UpdateAsync(long id, TUpdateInput input)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception($"Entity with id {id} not found");
        }

        input.Adapt(entity);
        var result = await Repository.UpdateAsync(entity);

        // 清除相关缓存
        await InvalidateCacheAsync(id);

        return result;
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public virtual async Task<int> DeleteAsync(long id)
    {
        var result = await Repository.DeleteAsync(id);

        // 清除相关缓存
        await InvalidateCacheAsync(id);

        return result;
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    public virtual async Task<int> DeleteBatchAsync(List<long> ids)
    {
        int count = 0;
        foreach (var id in ids)
        {
            count += await Repository.DeleteAsync(id);
        }

        // 清除所有相关缓存
        await InvalidateAllCacheAsync();

        return count;
    }

    /// <summary>
    /// 从 Excel 导入数据
    /// </summary>
    public virtual async Task<int> ImportAsync(byte[] fileBytes)
    {
        // 使用 ExcelService 导入数据
        var data = await ExcelService.ImportAsync<TCreateInput>(fileBytes);

        // 批量创建
        int count = 0;
        foreach (var item in data)
        {
            await CreateAsync(item);
            count++;
        }

        // 清除所有相关缓存
        await InvalidateAllCacheAsync();

        return count;
    }
}
