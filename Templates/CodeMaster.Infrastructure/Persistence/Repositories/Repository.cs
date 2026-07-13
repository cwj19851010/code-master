using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Data;
using CodeMaster.Core.MultiTenancy;
using SqlSugar;
using System.Linq.Expressions;
using Yitter.IdGenerator;

namespace CodeMaster.Infrastructure.Persistence.Repositories;

/// <summary>
/// SqlSugar读写仓储实现
/// </summary>
public class Repository<TEntity> : ReadOnlyRepository<TEntity>, IRepository<TEntity> where TEntity : class, IEntity<long>, new()
{
    private readonly IDataPermissionContext? _dataPermissionContext;
    private readonly ITenantContext? _tenantContext;

    public Repository(
        ISqlSugarClient db,
        IDataPermissionContext? dataPermissionContext = null,
        ITenantContext? tenantContext = null) : base(db)
    {
        _dataPermissionContext = dataPermissionContext;
        _tenantContext = tenantContext;
    }

    public virtual long Insert(TEntity entity)
    {
        // 生成雪花ID
        if (entity.Id == 0)
        {
            entity.Id = YitIdHelper.NextId();
        }

        // 自动填充部门信息
        FillTenantInfo(entity);
        FillDeptInfo(entity);

        return _db.Insertable(entity).ExecuteReturnSnowflakeId();
    }

    public virtual async Task<long> InsertAsync(TEntity entity)
    {
        // 生成雪花ID
        if (entity.Id == 0)
        {
            entity.Id = YitIdHelper.NextId();
        }

        // 自动填充部门信息
        FillTenantInfo(entity);
        FillDeptInfo(entity);

        return await _db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
    }

    public virtual int InsertRange(List<TEntity> entities)
    {
        // 批量生成雪花ID和填充部门信息
        foreach (var entity in entities)
        {
            if (entity.Id == 0)
            {
                entity.Id = YitIdHelper.NextId();
            }
            FillTenantInfo(entity);
            FillDeptInfo(entity);
        }

        return _db.Insertable(entities).ExecuteCommand();
    }

    public virtual async Task<int> InsertRangeAsync(List<TEntity> entities)
    {
        // 批量生成雪花ID和填充部门信息
        foreach (var entity in entities)
        {
            if (entity.Id == 0)
            {
                entity.Id = YitIdHelper.NextId();
            }
            FillTenantInfo(entity);
            FillDeptInfo(entity);
        }

        return await _db.Insertable(entities).ExecuteCommandAsync();
    }

    /// <summary>
    /// 自动填充部门信息（如果实体实现了 IDept 接口）
    /// </summary>
    private void FillDeptInfo(TEntity entity)
    {
        if (entity is IDept deptEntity && _dataPermissionContext != null)
        {
            // 只在创建时填充，如果已经有值则不覆盖
            if (deptEntity.DeptId == null)
            {
                deptEntity.DeptId = _dataPermissionContext.DeptId;
            }
            if (string.IsNullOrEmpty(deptEntity.DeptAncestors))
            {
                deptEntity.DeptAncestors = _dataPermissionContext.DeptAncestors;
            }
        }
    }

    private void FillTenantInfo(TEntity entity)
    {
        if (entity is not CodeMaster.Core.Entities.ITenant tenantEntity)
        {
            return;
        }

        var currentTenantId = _tenantContext?.CurrentTenantId;
        if (currentTenantId.HasValue && currentTenantId.Value > 0)
        {
            tenantEntity.TenantId = currentTenantId.Value;
            return;
        }

        if (tenantEntity.TenantId == 0)
        {
            tenantEntity.TenantId = currentTenantId ?? 0;
        }
    }

    public virtual int Update(TEntity entity)
    {
        return _db.Updateable(entity).ExecuteCommand();
    }

    public virtual async Task<int> UpdateAsync(TEntity entity)
    {
        return await _db.Updateable(entity).ExecuteCommandAsync();
    }

    public virtual int Delete(long id)
    {
        return _db.Deleteable<TEntity>().In(id).IsLogic().ExecuteCommand();
    }

    public virtual async Task<int> DeleteAsync(long id)
    {
        return await _db.Deleteable<TEntity>().In(id).IsLogic().ExecuteCommandAsync();
    }

    public virtual int Delete(Expression<Func<TEntity, bool>> where)
    {
        return _db.Deleteable<TEntity>().Where(where).IsLogic().ExecuteCommand();
    }

    public virtual async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> where)
    {
        return await _db.Deleteable<TEntity>().Where(where).IsLogic().ExecuteCommandAsync();
    }

}
