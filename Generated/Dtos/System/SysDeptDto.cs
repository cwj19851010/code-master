using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// SysDept查询DTO
/// </summary>
public class SysDeptQueryDto : PagedQueryDto
{
    /// <summary>
    /// ParentId
    /// </summary>
    public Int64? ParentId { get; set; }

    /// <summary>
    /// Ancestors
    /// </summary>
    public string? Ancestors { get; set; }

    /// <summary>
    /// DeptName
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// OrderNum
    /// </summary>
    public int? OrderNum { get; set; }

    /// <summary>
    /// Leader
    /// </summary>
    public string? Leader { get; set; }

    /// <summary>
    /// Phone
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

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
/// SysDeptDTO
/// </summary>
public class SysDeptDto : DtoBase
{
    /// <summary>
    /// ParentId
    /// </summary>
    public Int64? ParentId { get; set; }

    /// <summary>
    /// Ancestors
    /// </summary>
    public string? Ancestors { get; set; }

    /// <summary>
    /// DeptName
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// OrderNum
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// Leader
    /// </summary>
    public string? Leader { get; set; }

    /// <summary>
    /// Phone
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// DelFlag
    /// </summary>
    public int DelFlag { get; set; }

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
/// 创建SysDeptDTO
/// </summary>
public class CreateSysDeptDto : CreateDtoBase
{
    /// <summary>
    /// ParentId
    /// </summary>
    public Int64? ParentId { get; set; }

    /// <summary>
    /// Ancestors
    /// </summary>
    public string? Ancestors { get; set; }

    /// <summary>
    /// DeptName
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// OrderNum
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// Leader
    /// </summary>
    public string? Leader { get; set; }

    /// <summary>
    /// Phone
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

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
/// 更新SysDeptDTO
/// </summary>
public class UpdateSysDeptDto : UpdateDtoBase
{
    /// <summary>
    /// ParentId
    /// </summary>
    public Int64? ParentId { get; set; }

    /// <summary>
    /// Ancestors
    /// </summary>
    public string? Ancestors { get; set; }

    /// <summary>
    /// DeptName
    /// </summary>
    public string? DeptName { get; set; }

    /// <summary>
    /// OrderNum
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// Leader
    /// </summary>
    public string? Leader { get; set; }

    /// <summary>
    /// Phone
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}
