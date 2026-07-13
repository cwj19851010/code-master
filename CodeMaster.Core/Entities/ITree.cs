namespace CodeMaster.Core.Entities;

/// <summary>
/// 部门数据权限接口
/// 实现此接口的实体将启用部门/本人数据可见范围过滤
/// </summary>
public interface ITree
{
    /// <summary>
    /// 部门ID（创建人所在部门）
    /// </summary>
    long? ParentId { get; set; }
}
