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
    where TGetListInput : PagedQueryDto
{
    protected new readonly IRepository<TEntity> Repository;

    protected CrudApplicationService(IRepository<TEntity> repository)
        : base(repository)
    {
        Repository = repository;
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    public virtual async Task<long> CreateAsync(TCreateInput input)
    {
        var entity = input.Adapt<TEntity>();
        return await Repository.InsertAsync(entity);
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
        return await Repository.UpdateAsync(entity);
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public virtual async Task<int> DeleteAsync(long id)
    {
        return await Repository.DeleteAsync(id);
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
        return count;
    }
}
