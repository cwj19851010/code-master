namespace CodeMaster.Core.Entities;

/// <summary>
/// 树形实体接口。
/// </summary>
public interface ITree
{
    /// <summary>
    /// 父节点ID。
    /// </summary>
    long? ParentId { get; set; }

    /// <summary>
    /// 祖先节点路径，例如 0,1,2。
    /// </summary>
    string? Ancestors { get; set; }
}
