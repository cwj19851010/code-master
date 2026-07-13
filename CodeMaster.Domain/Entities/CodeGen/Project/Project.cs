using CodeMaster.Core.Entities;
using CodeMaster.Core.Enums;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 项目实体
/// </summary>
[SugarTable("sys_project")]
public class Project : EntityBaseWithTenant
{
    /// <summary>
    /// 项目名称（英文，用于代码生成）
    /// </summary>
    [SugarColumn(ColumnName = "project_name", Length = 100, IsNullable = false)]
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 项目显示名称（中文）
    /// </summary>
    [SugarColumn(ColumnName = "display_name", Length = 200, IsNullable = false)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 项目显示名称（英文）
    /// </summary>
    [SugarColumn(ColumnName = "display_name_en", Length = 200, IsNullable = true)]
    public string? DisplayNameEn { get; set; }

    /// <summary>
    /// 项目描述（中文）
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 项目描述（英文）
    /// </summary>
    [SugarColumn(ColumnName = "description_en", Length = 500, IsNullable = true)]
    public string? DescriptionEn { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    [SugarColumn(ColumnName = "database_type", IsNullable = false)]
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySQL;

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    [SugarColumn(ColumnName = "connection_string", Length = 500, IsNullable = false)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 项目路径
    /// </summary>
    [SugarColumn(ColumnName = "project_path", Length = 500, IsNullable = false)]
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Logo文件路径
    /// </summary>
    [SugarColumn(ColumnName = "logo_path", Length = 500, IsNullable = true)]
    public string? LogoPath { get; set; }

    /// <summary>
    /// 项目类型
    /// </summary>
    [SugarColumn(ColumnName = "project_type", IsNullable = false)]
    public ProjectType ProjectType { get; set; } = ProjectType.Server;

    /// <summary>
    /// 项目状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public ProjectStatus Status { get; set; } = ProjectStatus.NotInitialized;

    /// <summary>
    /// 前端端口
    /// </summary>
    [SugarColumn(ColumnName = "frontend_port", IsNullable = true)]
    public int? FrontendPort { get; set; }

    /// <summary>
    /// 后端端口
    /// </summary>
    [SugarColumn(ColumnName = "backend_port", IsNullable = true)]
    public int? BackendPort { get; set; }

    /// <summary>
    /// 初始化时间
    /// </summary>
    [SugarColumn(ColumnName = "initialized_at", IsNullable = true)]
    public DateTime? InitializedAt { get; set; }

    /// <summary>
    /// 初始化错误信息
    /// </summary>
    [SugarColumn(ColumnName = "init_error", Length = 2000, IsNullable = true)]
    public string? InitError { get; set; }
}
