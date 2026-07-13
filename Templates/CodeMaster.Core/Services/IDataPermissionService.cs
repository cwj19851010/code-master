using System.Linq.Expressions;
using CodeMaster.Core.Entities;

namespace CodeMaster.Core.Services;

/// <summary>
/// 数据权限服务接口
/// </summary>
public interface IDataPermissionService
{
    /// <summary>
    /// 构建数据权限表达式
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="dataScope">数据权限范围（1全部/2自定义/3本部门/4本部门及以下/5仅本人）</param>
    /// <param name="deptId">用户部门ID</param>
    /// <param name="customDeptIds">自定义部门ID列表（当dataScope=2时使用）</param>
    /// <returns>数据权限过滤表达式</returns>
    Expression<Func<T, bool>> BuildDataPermissionExpression<T>(
        long userId,
        int dataScope,
        long? deptId,
        List<long>? customDeptIds = null) where T : IDataPermission;

    /// <summary>
    /// 获取用户的子部门ID列表（包含本部门）
    /// </summary>
    /// <param name="deptId">部门ID</param>
    /// <returns>部门ID列表</returns>
    Task<List<long>> GetChildDeptIdsAsync(long deptId);
}
