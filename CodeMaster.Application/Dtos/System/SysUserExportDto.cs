using CodeMaster.Core.Attributes;
using CodeMaster.Domain.Entities.System;

namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 用户DTO（带导出特性示例）
/// </summary>
public class SysUserExportDto
{
    [ExportColumn("id", Order = 1)]
    public long Id { get; set; }

    [ExportColumn("username", Order = 2)]
    public string UserName { get; set; } = string.Empty;

    [ExportColumn("nickname", Order = 3)]
    public string NickName { get; set; } = string.Empty;

    [ExportColumn("email", Order = 4)]
    public string Email { get; set; } = string.Empty;

    [ExportColumn("phone", Order = 5)]
    public string PhoneNumber { get; set; } = string.Empty;

    [ExportColumn("sex", Order = 6)]
    public int? Sex { get; set; }

    [ExportIgnore] // 头像不导出
    public string Avatar { get; set; } = string.Empty;

    [ExportColumn("status", Order = 7)]
    public int Status { get; set; }

    // 部门ID - 有外键特性，会显示部门名称
    // 同时该字段本身被忽略，不显示部门ID
    [ExportIgnore]
    [ExportForeignKey(typeof(SysDept), "Id", "Name", TitleKey = "dept", Order = 8)]
    public long? DeptId { get; set; }

    // 部门名称 - 这个字段用于显示关联的部门名称
    [ExportIgnore] // 不需要单独导出，因为外键特性已经处理
    public string? DeptName { get; set; }

    // 岗位ID - 有外键特性，会显示岗位名称
    // 该字段本身不被忽略，所以会同时显示岗位ID和岗位名称
    [ExportColumn("postId", Order = 9)]
    [ExportForeignKey(typeof(SysPost), "Id", "PostName", TitleKey = "post_name", Order = 10)]
    public long? PostId { get; set; }

    // 岗位名称
    [ExportIgnore]
    public string? PostName { get; set; }

    [ExportColumn("create_time", Order = 11)]
    public DateTime CreateTime { get; set; }

    [ExportColumn("remark", Order = 12)]
    public string Remark { get; set; } = string.Empty;
}
