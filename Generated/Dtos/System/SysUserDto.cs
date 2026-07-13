using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// SysUser查询DTO
/// </summary>
public class SysUserQueryDto : PagedQueryDto
{
    /// <summary>
    /// UserName
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// NickName
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// UserType
    /// </summary>
    public string? UserType { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// PhoneNumber
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Sex
    /// </summary>
    public Int32? Sex { get; set; }

    /// <summary>
    /// Avatar
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// LoginIp
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// LoginDate
    /// </summary>
    public DateTime? LoginDate { get; set; }

    /// <summary>
    /// DeptId
    /// </summary>
    public Int64? DeptId { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// SysUserDTO
/// </summary>
public class SysUserDto : DtoBase
{
    /// <summary>
    /// UserName
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// NickName
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// UserType
    /// </summary>
    public string? UserType { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// PhoneNumber
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Sex
    /// </summary>
    public Int32? Sex { get; set; }

    /// <summary>
    /// Avatar
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// DelFlag
    /// </summary>
    public int DelFlag { get; set; }

    /// <summary>
    /// LoginIp
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// LoginDate
    /// </summary>
    public DateTime? LoginDate { get; set; }

    /// <summary>
    /// DeptId
    /// </summary>
    public Int64? DeptId { get; set; }

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
/// 创建SysUserDTO
/// </summary>
public class CreateSysUserDto : CreateDtoBase
{
    /// <summary>
    /// UserName
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// NickName
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// UserType
    /// </summary>
    public string? UserType { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// PhoneNumber
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Sex
    /// </summary>
    public Int32? Sex { get; set; }

    /// <summary>
    /// Avatar
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// LoginIp
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// LoginDate
    /// </summary>
    public DateTime? LoginDate { get; set; }

    /// <summary>
    /// DeptId
    /// </summary>
    public Int64? DeptId { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}

/// <summary>
/// 更新SysUserDTO
/// </summary>
public class UpdateSysUserDto : UpdateDtoBase
{
    /// <summary>
    /// UserName
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// NickName
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// UserType
    /// </summary>
    public string? UserType { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// PhoneNumber
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Sex
    /// </summary>
    public Int32? Sex { get; set; }

    /// <summary>
    /// Avatar
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// LoginIp
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// LoginDate
    /// </summary>
    public DateTime? LoginDate { get; set; }

    /// <summary>
    /// DeptId
    /// </summary>
    public Int64? DeptId { get; set; }

    /// <summary>
    /// Remark
    /// </summary>
    public string? Remark { get; set; }

}
