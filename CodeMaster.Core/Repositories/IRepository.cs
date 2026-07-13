using System.Linq.Expressions;
using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Repositories;

/// <summary>
/// 读写仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IRepository<TEntity> : IReadOnlyRepository<TEntity> where TEntity : class, IEntity<long>
{
    /// <summary>
    /// 插入实体
    /// </summary>
    long Insert(TEntity entity);

    /// <summary>
    /// 插入实体（异步）
    /// </summary>
    Task<long> InsertAsync(TEntity entity);

    /// <summary>
    /// 批量插入
    /// </summary>
    int InsertRange(List<TEntity> entities);

    /// <summary>
    /// 批量插入（异步）
    /// </summary>
    Task<int> InsertRangeAsync(List<TEntity> entities);

    /// <summary>
    /// 更新实体
    /// </summary>
    int Update(TEntity entity);

    /// <summary>
    /// 更新实体（异步）
    /// </summary>
    Task<int> UpdateAsync(TEntity entity);

    /// <summary>
    /// 根据ID删除
    /// </summary>
    int Delete(long id);

    /// <summary>
    /// 根据ID删除（异步）
    /// </summary>
    Task<int> DeleteAsync(long id);

    /// <summary>
    /// 根据条件删除
    /// </summary>
    int Delete(Expression<Func<TEntity, bool>> where);

    /// <summary>
    /// 根据条件删除（异步）
    /// </summary>
    Task<int> DeleteAsync(Expression<Func<TEntity, bool>> where);
}
