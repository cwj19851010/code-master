namespace CodeMaster.Core.Dtos;

/// <summary>
/// 更新DTO基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class UpdateDtoBase<TKey>
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public TKey Id { get; set; } = default!;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
