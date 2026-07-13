using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// SysMenu查询DTO
/// </summary>
public class SysMenuQueryDto : PagedQueryDto
{
    /// <summary>
    /// MenuName
    /// </summary>
    public string? MenuName { get; set; }

    /// <summary>
    /// ParentId
    /// </summary>
    public Int64? ParentId { get; set; }

    /// <summary>
    /// OrderNum
    /// </summary>
    public int? OrderNum { get; set; }

    /// <summary>
    /// Path
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Component
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// Query
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// IsFrame
    /// </summary>
    public int? IsFrame { get; set; }

    /// <summary>
    /// IsCache
    /// </summary>
    public int? IsCache { get; set; }

    /// <summary>
    /// MenuType
    /// </summary>
    public string? MenuType { get; set; }

    /// <summary>
    /// Visible
    /// </summary>
    public int? Visible { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Perms
    /// </summary>
    public string? Perms { get; set; }

    /// <summary>
    /// Icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// SysMenuDTO
/// </summary>
public class SysMenuDto : DtoBase
{
    /// <summary>
    /// MenuName
    /// </summary>
    public string? MenuName { get; set; }

    /// <summary>
    /// ParentId
    /// </summary>
    public Int64? ParentId { get; set; }

    /// <summary>
    /// OrderNum
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// Path
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Component
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// Query
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// IsFrame
    /// </summary>
    public int IsFrame { get; set; }

    /// <summary>
    /// IsCache
    /// </summary>
    public int IsCache { get; set; }

    /// <summary>
    /// MenuType
    /// </summary>
    public string? MenuType { get; set; }

    /// <summary>
    /// Visible
    /// </summary>
    public int Visible { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Perms
    /// </summary>
    public string? Perms { get; set; }

    /// <summary>
    /// Icon
    /// </summary>
    public string? Icon { get; set; }

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
/// 创建SysMenuDTO
/// </summary>
public class CreateSysMenuDto : CreateDtoBase
{
    /// <summary>
    /// MenuName
    /// </summary>
    public string? MenuName { get; set; }

    /// <summary>
    /// ParentId
    /// </summary>
    public Int64? ParentId { get; set; }

    /// <summary>
    /// OrderNum
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// Path
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Component
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// Query
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// IsFrame
    /// </summary>
    public int IsFrame { get; set; }

    /// <summary>
    /// IsCache
    /// </summary>
    public int IsCache { get; set; }

    /// <summary>
    /// MenuType
    /// </summary>
    public string? MenuType { get; set; }

    /// <summary>
    /// Visible
    /// </summary>
    public int Visible { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Perms
    /// </summary>
    public string? Perms { get; set; }

    /// <summary>
    /// Icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// 更新SysMenuDTO
/// </summary>
public class UpdateSysMenuDto : UpdateDtoBase
{
    /// <summary>
    /// MenuName
    /// </summary>
    public string? MenuName { get; set; }

    /// <summary>
    /// ParentId
    /// </summary>
    public Int64? ParentId { get; set; }

    /// <summary>
    /// OrderNum
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// Path
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Component
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// Query
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// IsFrame
    /// </summary>
    public int IsFrame { get; set; }

    /// <summary>
    /// IsCache
    /// </summary>
    public int IsCache { get; set; }

    /// <summary>
    /// MenuType
    /// </summary>
    public string? MenuType { get; set; }

    /// <summary>
    /// Visible
    /// </summary>
    public int Visible { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Perms
    /// </summary>
    public string? Perms { get; set; }

    /// <summary>
    /// Icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}
