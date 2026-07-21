using CodeMaster.Core.Entities;
using CodeMaster.Core.Repositories;
using SqlSugar;
using System.Linq.Expressions;

namespace CodeMaster.Infrastructure.Persistence.Repositories;

/// <summary>
/// SqlSugar只读仓储实现
/// </summary>
public class ReadOnlyRepository<TEntity> : IReadOnlyRepository<TEntity> where TEntity : class, IBaseEntity, new()
{
    protected readonly ISqlSugarClient _db;

    public ReadOnlyRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public virtual ISugarQueryable<TEntity> GetQueryable()
    {
        return _db.Queryable<TEntity>();
    }

    public virtual IQueryable<TEntity> AsQueryable()
    {
        // 重要说明：
        // SqlSugar 的 ISugarQueryable<T> 并不实现标准的 System.Linq.IQueryable<T> 接口
        // 因此无法直接返回 IQueryable<T>
        //
        // 如果要实现真正的 ORM 解耦，有以下几种方案：
        // 1. 使用 EF Core 等实现了标准 IQueryable 的 ORM
        // 2. 创建适配器层，将 SqlSugar 的查询包装为 IQueryable（复杂且性能损失）
        // 3. 在 Repository 层提供足够的查询方法，不暴露 IQueryable
        //
        // 当前方案：抛出异常，提示使用 GetQueryable() 获取 SqlSugar 的 ISugarQueryable
        throw new NotSupportedException(
            "SqlSugar's ISugarQueryable<T> does not implement System.Linq.IQueryable<T>. " +
            "Use GetQueryable() to get SqlSugar's ISugarQueryable<T>, " +
            "or use specific Repository methods like GetListAsync(), GetPagedListAsync(), etc.");
    }

    public virtual TEntity? GetById(long id)
    {
        return _db.Queryable<TEntity>().InSingle(id);
    }

    public virtual async Task<TEntity?> GetByIdAsync(long id)
    {
        return await _db.Queryable<TEntity>().InSingleAsync(id);
    }

    public virtual List<TEntity> GetList(Expression<Func<TEntity, bool>>? where = null)
    {
        var query = _db.Queryable<TEntity>();
        if (where != null)
        {
            query = query.Where(where);
        }
        return query.ToList();
    }

    public virtual async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? where = null)
    {
        var query = _db.Queryable<TEntity>();
        if (where != null)
        {
            query = query.Where(where);
        }
        return await query.ToListAsync();
    }

    public virtual (List<TEntity> Items, int Total) GetPagedList(Expression<Func<TEntity, bool>>? where, int pageNum, int pageSize)
    {
        var query = _db.Queryable<TEntity>();
        if (where != null)
        {
            query = query.Where(where);
        }

        int total = 0;
        var items = query.ToPageList(pageNum, pageSize, ref total);
        return (items, total);
    }

    public virtual async Task<(List<TEntity> Items, int Total)> GetPagedListAsync(Expression<Func<TEntity, bool>>? where, int pageNum, int pageSize)
    {
        var query = _db.Queryable<TEntity>();
        if (where != null)
        {
            query = query.Where(where);
        }

        RefAsync<int> total = 0;
        var items = await query.ToPageListAsync(pageNum, pageSize, total);
        return (items, total);
    }

    public virtual bool Any(Expression<Func<TEntity, bool>> where)
    {
        return _db.Queryable<TEntity>().Any(where);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> where)
    {
        return await _db.Queryable<TEntity>().AnyAsync(where);
    }

    public virtual int Count(Expression<Func<TEntity, bool>>? where = null)
    {
        var query = _db.Queryable<TEntity>();
        if (where != null)
        {
            query = query.Where(where);
        }
        return query.Count();
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? where = null)
    {
        var query = _db.Queryable<TEntity>();
        if (where != null)
        {
            query = query.Where(where);
        }
        return await query.CountAsync();
    }
}
