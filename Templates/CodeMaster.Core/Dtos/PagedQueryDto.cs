namespace CodeMaster.Core.Dtos;

/// <summary>
/// 分页查询DTO
/// </summary>
public class PagedQueryDto
{
    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int PageNum { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// 是否降序
    /// </summary>
    public bool IsDesc { get; set; } = false;
}
