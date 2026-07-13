using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 模块实体（表）
/// </summary>
[SugarTable("sys_module_entity")]
public class ModuleEntity : EntityBaseWithTenant
{
    /// <summary>
    /// 项目ID
    /// </summary>
    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    [SugarColumn(ColumnName = "module_id", IsNullable = false)]
    public long ModuleId { get; set; }

    /// <summary>
    /// 实体名称（英文 PascalCase，如 SysUser）
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实体描述（中文，如"系统用户"）
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 200, IsNullable = false)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否有主键（非ReadOnly时必须为true）
    /// </summary>
    [SugarColumn(ColumnName = "has_primary_key", IsNullable = false)]
    public bool HasPrimaryKey { get; set; } = true;

    /// <summary>
    /// 表名（snake_case，如 sys_user，为空则自动生成）
    /// </summary>
    [SugarColumn(ColumnName = "table_name", Length = 100, IsNullable = true)]
    public string? TableName { get; set; }

    /// <summary>
    /// 是否树形结构
    /// </summary>
    [SugarColumn(ColumnName = "is_tree", IsNullable = false)]
    public bool IsTree { get; set; }

    /// <summary>
    /// 是否只读（只生成查询接口）
    /// </summary>
    [SugarColumn(ColumnName = "is_read_only", IsNullable = false)]
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 是否启用多租户
    /// </summary>
    [SugarColumn(ColumnName = "has_tenant", IsNullable = false)]
    public bool HasTenant { get; set; }

    /// <summary>
    /// 是否启用数据权限
    /// </summary>
    [SugarColumn(ColumnName = "has_data_permission", IsNullable = false)]
    public bool HasDataPermission { get; set; }

    /// <summary>
    /// 是否启用审计（IAuditEntity：CreateUserId/CreateBy/CreateTime/UpdateUserId/UpdateBy/UpdateTime）
    /// </summary>
    [SugarColumn(ColumnName = "has_audit", IsNullable = false)]
    public bool HasAudit { get; set; } = true;

    /// <summary>
    /// 是否启用软删除（ISoftDelete：IsDeleted/DeleteTime/DeleteBy/DeleteUserId）
    /// </summary>
    [SugarColumn(ColumnName = "has_soft_delete", IsNullable = false)]
    public bool HasSoftDelete { get; set; } = true;

    /// <summary>
    /// 是否生成前端页面
    /// </summary>
    [SugarColumn(ColumnName = "generate_frontend", IsNullable = false)]
    public bool GenerateFrontend { get; set; } = true;

    /// <summary>
    /// 前端路由路径（如 /system/user）
    /// </summary>
    [SugarColumn(ColumnName = "frontend_route", Length = 200, IsNullable = true)]
    public string? FrontendRoute { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    [SugarColumn(ColumnName = "menu_icon", Length = 100, IsNullable = true)]
    public string? MenuIcon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order_num", IsNullable = false)]
    public int OrderNum { get; set; }

    /// <summary>
    /// 是否被引用为子表（不生成前端页面和菜单）
    /// </summary>
    [SugarColumn(ColumnName = "is_child_table", IsNullable = false)]
    public bool IsChildTable { get; set; }

    /// <summary>
    /// 是否已生成代码
    /// </summary>
    [SugarColumn(ColumnName = "is_generated", IsNullable = false)]
    public bool IsGenerated { get; set; }

    /// <summary>
    /// 最后生成时间
    /// </summary>
    [SugarColumn(ColumnName = "last_generated_time", IsNullable = true)]
    public DateTime? LastGeneratedTime { get; set; }
}
