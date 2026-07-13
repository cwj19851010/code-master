using CodeMaster.Core.Entities;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 用户实体
/// </summary>
public class SysUser : DataPermissionEntityBase
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    public string NickName { get; set; } = string.Empty;

    /// <summary>
    /// 用户类型（00系统用户）
    /// </summary>
    public string? UserType { get; set; } = "00";

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 性别（0男 1女 2未知）
    /// </summary>
    public int? Sex { get; set; } = 2;

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 状态（0正常 1停用）
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 删除标志（0未删除 1已删除）
    /// </summary>
    public int DelFlag { get; set; } = 0;

    /// <summary>
    /// 最后登录IP
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LoginDate { get; set; }

    // DeptId 已在 DataPermissionEntityBase 中定义

    /// <summary>
    /// 角色列表（不映射到数据库）
    /// </summary>
    [SqlSugar.SugarColumn(IsIgnore = true)]
    public List<string>? RoleNames { get; set; }

    /// <summary>
    /// 部门名称（不映射到数据库）
    /// </summary>
    [SqlSugar.SugarColumn(IsIgnore = true)]
    public string? DeptName { get; set; }

    /// <summary>
    /// 职位ID
    /// </summary>
    public long? PostId { get; set; }

    /// <summary>
    /// 职位名称（不映射到数据库）
    /// </summary>
    [SqlSugar.SugarColumn(IsIgnore = true)]
    public string? PostName { get; set; }
}
