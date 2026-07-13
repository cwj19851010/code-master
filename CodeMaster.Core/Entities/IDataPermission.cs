namespace CodeMaster.Core.Entities;

/// <summary>
/// 数据权限接口
/// 实现此接口的实体将自动启用数据权限过滤
/// </summary>
public interface IDataPermission
{
    /// <summary>
    /// 部门ID
    /// </summary>
    long? DeptId { get; set; }

    /// <summary>
    /// 创建人ID
    /// </summary>
    long? CreateUserId { get; set; }
}
