namespace CodeMaster.Core.Dtos;

/// <summary>
/// DTO基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class DtoBase<TKey>
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public TKey Id { get; set; } = default!;

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreateBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    public string? UpdateBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
