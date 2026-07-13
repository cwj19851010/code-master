namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 用户DTO
/// </summary>
public class SysUserDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int? Sex { get; set; }
    public string Avatar { get; set; } = string.Empty;
    public int Status { get; set; }
    public long? DeptId { get; set; }
    public string? DeptName { get; set; }
    public long? PostId { get; set; }
    public string? PostName { get; set; }
    public DateTime CreateTime { get; set; }
    public string Remark { get; set; } = string.Empty;
}

/// <summary>
/// 创建用户DTO
/// </summary>
public class CreateSysUserDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int? Sex { get; set; } = 2;
    public long? DeptId { get; set; }
    public long? PostId { get; set; }
    public int Status { get; set; } = 0;
    public string Remark { get; set; } = string.Empty;
}

/// <summary>
/// 更新用户DTO
/// </summary>
public class UpdateSysUserDto
{
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int? Sex { get; set; }
    public string? Avatar { get; set; }
    public long? DeptId { get; set; }
    public long? PostId { get; set; }
    public int? Status { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 用户查询DTO
/// </summary>
public class SysUserQueryDto
{
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public int? Status { get; set; }
    public long? DeptId { get; set; }
    public DateTime? BeginTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int PageNum { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
