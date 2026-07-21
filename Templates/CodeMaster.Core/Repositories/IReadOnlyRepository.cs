using System.Linq.Expressions;
using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Core.Repositories;

/// <summary>
/// 只读仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IReadOnlyRepository<TEntity> where TEntity : class, IBaseEntity
{
    /// <summary>
    /// 获取可查询对象（返回动态类型，由具体ORM实现）
    /// </summary>
    ISugarQueryable<TEntity> GetQueryable();

    /// <summary>
    /// 获取可查询对象（IQueryable）
    /// </summary>
    IQueryable<TEntity> AsQueryable();

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    TEntity? GetById(long id);

    /// <summary>
    /// 根据ID获取实体（异步）
    /// </summary>
    Task<TEntity?> GetByIdAsync(long id);

    /// <summary>
    /// 获取列表
    /// </summary>
    List<TEntity> GetList(Expression<Func<TEntity, bool>>? where = null);

    /// <summary>
    /// 获取列表（异步）
    /// </summary>
    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? where = null);

    /// <summary>
    /// 获取分页列表
    /// </summary>
    (List<TEntity> Items, int Total) GetPagedList(Expression<Func<TEntity, bool>>? where, int pageNum, int pageSize);

    /// <summary>
    /// 获取分页列表（异步）
    /// </summary>
    Task<(List<TEntity> Items, int Total)> GetPagedListAsync(Expression<Func<TEntity, bool>>? where, int pageNum, int pageSize);

    /// <summary>
    /// 判断是否存在
    /// </summary>
    bool Any(Expression<Func<TEntity, bool>> where);

    /// <summary>
    /// 判断是否存在（异步）
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> where);

    /// <summary>
    /// 获取数量
    /// </summary>
    int Count(Expression<Func<TEntity, bool>>? where = null);

    /// <summary>
    /// 获取数量（异步）
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? where = null);
}
