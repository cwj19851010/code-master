namespace CodeMaster.Core.Entities;

/// <summary>
/// 审计实体接口
/// </summary>
public interface IAuditEntity
{
    /// <summary>
    /// 创建人
    /// </summary>
    long? CreateUserId { get; set; }
    /// <summary>
    /// 创建人
    /// </summary>
    string? CreateBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    DateTime CreateTime { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    long? UpdateUserId { get; set; }
    /// <summary>
    /// 更新人
    /// </summary>
    string? UpdateBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    DateTime? UpdateTime { get; set; }
}
