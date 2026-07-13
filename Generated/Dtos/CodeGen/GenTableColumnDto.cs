using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// GenTableColumn查询DTO
/// </summary>
public class GenTableColumnQueryDto : PagedQueryDto
{
    /// <summary>
    /// TableId
    /// </summary>
    public long? TableId { get; set; }

    /// <summary>
    /// ColumnName
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// PropertyName
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// ColumnComment
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// ColumnType
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// CsharpType
    /// </summary>
    public string? CsharpType { get; set; }

    /// <summary>
    /// IsPk
    /// </summary>
    public int? IsPk { get; set; }

    /// <summary>
    /// IsIncrement
    /// </summary>
    public int? IsIncrement { get; set; }

    /// <summary>
    /// IsRequired
    /// </summary>
    public int? IsRequired { get; set; }

    /// <summary>
    /// ShowInList
    /// </summary>
    public int? ShowInList { get; set; }

    /// <summary>
    /// ShowInAdd
    /// </summary>
    public int? ShowInAdd { get; set; }

    /// <summary>
    /// ShowInEdit
    /// </summary>
    public int? ShowInEdit { get; set; }

    /// <summary>
    /// ShowInDetail
    /// </summary>
    public int? ShowInDetail { get; set; }

    /// <summary>
    /// IsQuery
    /// </summary>
    public int? IsQuery { get; set; }

    /// <summary>
    /// QueryType
    /// </summary>
    public string? QueryType { get; set; }

    /// <summary>
    /// HtmlType
    /// </summary>
    public string? HtmlType { get; set; }

    /// <summary>
    /// DictType
    /// </summary>
    public string? DictType { get; set; }

    /// <summary>
    /// Sort
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// GenTableColumnDTO
/// </summary>
public class GenTableColumnDto : DtoBase
{
    /// <summary>
    /// TableId
    /// </summary>
    public long TableId { get; set; }

    /// <summary>
    /// ColumnName
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// PropertyName
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// ColumnComment
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// ColumnType
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// CsharpType
    /// </summary>
    public string? CsharpType { get; set; }

    /// <summary>
    /// IsPk
    /// </summary>
    public int IsPk { get; set; }

    /// <summary>
    /// IsIncrement
    /// </summary>
    public int IsIncrement { get; set; }

    /// <summary>
    /// IsRequired
    /// </summary>
    public int IsRequired { get; set; }

    /// <summary>
    /// ShowInList
    /// </summary>
    public int ShowInList { get; set; }

    /// <summary>
    /// ShowInAdd
    /// </summary>
    public int ShowInAdd { get; set; }

    /// <summary>
    /// ShowInEdit
    /// </summary>
    public int ShowInEdit { get; set; }

    /// <summary>
    /// ShowInDetail
    /// </summary>
    public int ShowInDetail { get; set; }

    /// <summary>
    /// IsQuery
    /// </summary>
    public int IsQuery { get; set; }

    /// <summary>
    /// QueryType
    /// </summary>
    public string? QueryType { get; set; }

    /// <summary>
    /// HtmlType
    /// </summary>
    public string? HtmlType { get; set; }

    /// <summary>
    /// DictType
    /// </summary>
    public string? DictType { get; set; }

    /// <summary>
    /// Sort
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Id
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// CreateBy
    /// </summary>
    public string? CreateBy { get; set; }

    /// <summary>
    /// CreateTime
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// UpdateBy
    /// </summary>
    public string? UpdateBy { get; set; }

    /// <summary>
    /// UpdateTime
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// 创建GenTableColumnDTO
/// </summary>
public class CreateGenTableColumnDto : CreateDtoBase
{
    /// <summary>
    /// TableId
    /// </summary>
    public long TableId { get; set; }

    /// <summary>
    /// ColumnName
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// PropertyName
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// ColumnComment
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// ColumnType
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// CsharpType
    /// </summary>
    public string? CsharpType { get; set; }

    /// <summary>
    /// IsPk
    /// </summary>
    public int IsPk { get; set; }

    /// <summary>
    /// IsIncrement
    /// </summary>
    public int IsIncrement { get; set; }

    /// <summary>
    /// IsRequired
    /// </summary>
    public int IsRequired { get; set; }

    /// <summary>
    /// ShowInList
    /// </summary>
    public int ShowInList { get; set; }

    /// <summary>
    /// ShowInAdd
    /// </summary>
    public int ShowInAdd { get; set; }

    /// <summary>
    /// ShowInEdit
    /// </summary>
    public int ShowInEdit { get; set; }

    /// <summary>
    /// ShowInDetail
    /// </summary>
    public int ShowInDetail { get; set; }

    /// <summary>
    /// IsQuery
    /// </summary>
    public int IsQuery { get; set; }

    /// <summary>
    /// QueryType
    /// </summary>
    public string? QueryType { get; set; }

    /// <summary>
    /// HtmlType
    /// </summary>
    public string? HtmlType { get; set; }

    /// <summary>
    /// DictType
    /// </summary>
    public string? DictType { get; set; }

    /// <summary>
    /// Sort
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// 更新GenTableColumnDTO
/// </summary>
public class UpdateGenTableColumnDto : UpdateDtoBase
{
    /// <summary>
    /// TableId
    /// </summary>
    public long TableId { get; set; }

    /// <summary>
    /// ColumnName
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// PropertyName
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// ColumnComment
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// ColumnType
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// CsharpType
    /// </summary>
    public string? CsharpType { get; set; }

    /// <summary>
    /// IsPk
    /// </summary>
    public int IsPk { get; set; }

    /// <summary>
    /// IsIncrement
    /// </summary>
    public int IsIncrement { get; set; }

    /// <summary>
    /// IsRequired
    /// </summary>
    public int IsRequired { get; set; }

    /// <summary>
    /// ShowInList
    /// </summary>
    public int ShowInList { get; set; }

    /// <summary>
    /// ShowInAdd
    /// </summary>
    public int ShowInAdd { get; set; }

    /// <summary>
    /// ShowInEdit
    /// </summary>
    public int ShowInEdit { get; set; }

    /// <summary>
    /// ShowInDetail
    /// </summary>
    public int ShowInDetail { get; set; }

    /// <summary>
    /// IsQuery
    /// </summary>
    public int IsQuery { get; set; }

    /// <summary>
    /// QueryType
    /// </summary>
    public string? QueryType { get; set; }

    /// <summary>
    /// HtmlType
    /// </summary>
    public string? HtmlType { get; set; }

    /// <summary>
    /// DictType
    /// </summary>
    public string? DictType { get; set; }

    /// <summary>
    /// Sort
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}
