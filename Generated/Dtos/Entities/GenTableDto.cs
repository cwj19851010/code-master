using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.Entities;

/// <summary>
/// GenTable查询DTO
/// </summary>
public class GenTableQueryDto : PagedQueryDto
{
    /// <summary>
    /// TableName
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// EntityName
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// TableComment
    /// </summary>
    public string? TableComment { get; set; }

    /// <summary>
    /// FunctionName
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// ModuleId
    /// </summary>
    public long ModuleId { get; set; }

    /// <summary>
    /// IsReadOnly
    /// </summary>
    public int IsReadOnly { get; set; }

    /// <summary>
    /// OnlyDto
    /// </summary>
    public int OnlyDto { get; set; }

    /// <summary>
    /// IsTree
    /// </summary>
    public int IsTree { get; set; }

    /// <summary>
    /// IsChild
    /// </summary>
    public int IsChild { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// TreeParentField
    /// </summary>
    public string? TreeParentField { get; set; }

    /// <summary>
    /// TreeNameField
    /// </summary>
    public string? TreeNameField { get; set; }

    /// <summary>
    /// FunctionAuthor
    /// </summary>
    public string? FunctionAuthor { get; set; }

    /// <summary>
    /// GenPath
    /// </summary>
    public string? GenPath { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// GenTableDTO
/// </summary>
public class GenTableDto : DtoBase
{
    /// <summary>
    /// TableName
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// EntityName
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// TableComment
    /// </summary>
    public string? TableComment { get; set; }

    /// <summary>
    /// FunctionName
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// ModuleId
    /// </summary>
    public long ModuleId { get; set; }

    /// <summary>
    /// IsReadOnly
    /// </summary>
    public int IsReadOnly { get; set; }

    /// <summary>
    /// OnlyDto
    /// </summary>
    public int OnlyDto { get; set; }

    /// <summary>
    /// IsTree
    /// </summary>
    public int IsTree { get; set; }

    /// <summary>
    /// IsChild
    /// </summary>
    public int IsChild { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// TreeParentField
    /// </summary>
    public string? TreeParentField { get; set; }

    /// <summary>
    /// TreeNameField
    /// </summary>
    public string? TreeNameField { get; set; }

    /// <summary>
    /// FunctionAuthor
    /// </summary>
    public string? FunctionAuthor { get; set; }

    /// <summary>
    /// GenPath
    /// </summary>
    public string? GenPath { get; set; }

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
/// 创建GenTableDTO
/// </summary>
public class CreateGenTableDto : CreateDtoBase
{
    /// <summary>
    /// TableName
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// EntityName
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// TableComment
    /// </summary>
    public string? TableComment { get; set; }

    /// <summary>
    /// FunctionName
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// ModuleId
    /// </summary>
    public long ModuleId { get; set; }

    /// <summary>
    /// IsReadOnly
    /// </summary>
    public int IsReadOnly { get; set; }

    /// <summary>
    /// OnlyDto
    /// </summary>
    public int OnlyDto { get; set; }

    /// <summary>
    /// IsTree
    /// </summary>
    public int IsTree { get; set; }

    /// <summary>
    /// IsChild
    /// </summary>
    public int IsChild { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// TreeParentField
    /// </summary>
    public string? TreeParentField { get; set; }

    /// <summary>
    /// TreeNameField
    /// </summary>
    public string? TreeNameField { get; set; }

    /// <summary>
    /// FunctionAuthor
    /// </summary>
    public string? FunctionAuthor { get; set; }

    /// <summary>
    /// GenPath
    /// </summary>
    public string? GenPath { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// 更新GenTableDTO
/// </summary>
public class UpdateGenTableDto : UpdateDtoBase
{
    /// <summary>
    /// TableName
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// EntityName
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// TableComment
    /// </summary>
    public string? TableComment { get; set; }

    /// <summary>
    /// FunctionName
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// ModuleId
    /// </summary>
    public long ModuleId { get; set; }

    /// <summary>
    /// IsReadOnly
    /// </summary>
    public int IsReadOnly { get; set; }

    /// <summary>
    /// OnlyDto
    /// </summary>
    public int OnlyDto { get; set; }

    /// <summary>
    /// IsTree
    /// </summary>
    public int IsTree { get; set; }

    /// <summary>
    /// IsChild
    /// </summary>
    public int IsChild { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// TreeParentField
    /// </summary>
    public string? TreeParentField { get; set; }

    /// <summary>
    /// TreeNameField
    /// </summary>
    public string? TreeNameField { get; set; }

    /// <summary>
    /// FunctionAuthor
    /// </summary>
    public string? FunctionAuthor { get; set; }

    /// <summary>
    /// GenPath
    /// </summary>
    public string? GenPath { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}
