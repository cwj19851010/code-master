using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Core.Extensions;

/// <summary>
/// SqlSugar 软删除扩展方法
/// </summary>
public static class SoftDeleteExtensions
{
    /// <summary>
    /// 软删除实体（不受软删除过滤器影响）
    /// </summary>
    public static async Task<int> SoftDeleteAsync<T>(this ISqlSugarClient db, long id) where T : class, ISoftDelete, IEntity<long>, new()
    {
        return await db.Updateable<T>()
            .SetColumns(it => new T
            {
                IsDeleted = true,
                DeleteTime = DateTime.UtcNow
            })
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 批量软删除实体（不受软删除过滤器影响）
    /// </summary>
    public static async Task<int> SoftDeleteAsync<T>(this ISqlSugarClient db, long[] ids) where T : class, ISoftDelete, IEntity<long>, new()
    {
        return await db.Updateable<T>()
            .SetColumns(it => new T
            {
                IsDeleted = true,
                DeleteTime = DateTime.UtcNow
            })
            .Where(it => ids.Contains(it.Id))
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 恢复软删除的实体（直接通过 ID 更新，不受过滤器影响）
    /// </summary>
    public static async Task<int> RestoreAsync<T>(this ISqlSugarClient db, long id) where T : class, ISoftDelete, IEntity<long>, new()
    {
        // 直接通过 ID 更新，不受软删除过滤器影响
        return await db.Updateable<T>()
            .SetColumns(it => new T
            {
                IsDeleted = false,
                DeleteTime = null
            })
            .Where(it => it.Id == id && it.IsDeleted == true)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 查询包含已删除的数据（禁用软删除过滤器）
    /// </summary>
    public static ISugarQueryable<T> IncludeDeleted<T>(this ISugarQueryable<T> queryable) where T : class, ISoftDelete, new()
    {
        return queryable.ClearFilter();
    }
}
