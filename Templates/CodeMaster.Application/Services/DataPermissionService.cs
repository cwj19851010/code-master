using System.Linq.Expressions;
using CodeMaster.Core.Entities;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.System;
using SqlSugar;

namespace CodeMaster.Application.Services;

/// <summary>
/// 部门节点（用于递归查询）
/// </summary>
internal class DeptNode
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
}

/// <summary>
/// 数据权限服务实现
/// </summary>
public class DataPermissionService : IDataPermissionService
{
    private readonly ISqlSugarClient _db;

    public DataPermissionService(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 构建数据权限表达式
    /// </summary>
    public Expression<Func<T, bool>> BuildDataPermissionExpression<T>(
        long userId,
        int dataScope,
        long? deptId,
        List<long>? customDeptIds = null) where T : IDataPermission
    {
        return dataScope switch
        {
            // 1 - 全部数据权限
            1 => entity => true,

            // 2 - 自定义数据权限
            2 => entity => customDeptIds != null && customDeptIds.Contains(entity.DeptId ?? 0),

            // 3 - 本部门数据权限
            3 => entity => entity.DeptId == deptId,

            // 4 - 本部门及以下数据权限（需要异步获取子部门，这里先返回本部门）
            4 => entity => entity.DeptId == deptId,

            // 5 - 仅本人数据权限
            5 => entity => entity.CreateUserId == userId,

            // 默认：无权限
            _ => entity => false
        };
    }

    /// <summary>
    /// 获取用户的子部门ID列表（包含本部门）
    /// </summary>
    public async Task<List<long>> GetChildDeptIdsAsync(long deptId)
    {
        var allDepts = await _db.Queryable<SysDept>()
            .Select(d => new { Id = d.Id, ParentId = d.ParentId })
            .ToListAsync();

        var result = new List<long> { deptId };
        GetChildDeptIdsRecursive(deptId, allDepts.Select(d => new DeptNode { Id = d.Id, ParentId = d.ParentId }).ToList(), result);

        return result;
    }

    /// <summary>
    /// 递归获取子部门ID
    /// </summary>
    private void GetChildDeptIdsRecursive(long parentId, List<DeptNode> allDepts, List<long> result)
    {
        var children = allDepts.Where(d => d.ParentId == parentId).ToList();

        foreach (var child in children)
        {
            long childId = child.Id;
            if (!result.Contains(childId))
            {
                result.Add(childId);
                GetChildDeptIdsRecursive(childId, allDepts, result);
            }
        }
    }
}
