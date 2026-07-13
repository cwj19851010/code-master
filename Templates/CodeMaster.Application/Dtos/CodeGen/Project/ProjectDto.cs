using CodeMaster.Core.Dtos;
using CodeMaster.Core.Enums;

namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 项目DTO
/// </summary>
public class ProjectDto : EntityDto
{
    /// <summary>
    /// 项目名称（英文，用于代码生成）
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 项目显示名称（中文）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 项目显示名称（英文）
    /// </summary>
    public string? DisplayNameEn { get; set; }

    /// <summary>
    /// 项目描述（中文）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 项目描述（英文）
    /// </summary>
    public string? DescriptionEn { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 项目路径
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Logo文件路径
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// 项目类型
    /// </summary>
    public ProjectType ProjectType { get; set; }

    /// <summary>
    /// 项目状态
    /// </summary>
    public ProjectStatus Status { get; set; }

    /// <summary>
    /// 前端端口
    /// </summary>
    public int? FrontendPort { get; set; }

    /// <summary>
    /// 后端端口
    /// </summary>
    public int? BackendPort { get; set; }

    /// <summary>
    /// 初始化时间
    /// </summary>
    public DateTime? InitializedAt { get; set; }

    /// <summary>
    /// 初始化错误信息
    /// </summary>
    public string? InitError { get; set; }
}

/// <summary>
/// 创建项目DTO
/// </summary>
public class CreateProjectDto
{
    /// <summary>
    /// 项目名称（英文，用于代码生成）
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 项目显示名称（中文）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 项目显示名称（英文）
    /// </summary>
    public string? DisplayNameEn { get; set; }

    /// <summary>
    /// 项目描述（中文）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 项目描述（英文）
    /// </summary>
    public string? DescriptionEn { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 项目路径
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Logo文件路径
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// 项目类型
    /// </summary>
    public ProjectType ProjectType { get; set; }

    /// <summary>
    /// 前端端口
    /// </summary>
    public int? FrontendPort { get; set; }

    /// <summary>
    /// 后端端口
    /// </summary>
    public int? BackendPort { get; set; }
}

/// <summary>
/// 更新项目DTO
/// </summary>
public class UpdateProjectDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 项目名称（英文，用于代码生成）
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 项目显示名称（中文）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 项目显示名称（英文）
    /// </summary>
    public string? DisplayNameEn { get; set; }

    /// <summary>
    /// 项目描述（中文）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 项目描述（英文）
    /// </summary>
    public string? DescriptionEn { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 项目路径
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Logo文件路径
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// 项目类型
    /// </summary>
    public ProjectType ProjectType { get; set; }

    /// <summary>
    /// 前端端口
    /// </summary>
    public int? FrontendPort { get; set; }

    /// <summary>
    /// 后端端口
    /// </summary>
    public int? BackendPort { get; set; }
}

/// <summary>
/// 项目查询DTO
/// </summary>
public class ProjectQueryDto : PagedQueryDto
{
    /// <summary>
    /// 项目名称
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType? DatabaseType { get; set; }

    /// <summary>
    /// 项目类型
    /// </summary>
    public ProjectType? ProjectType { get; set; }

    /// <summary>
    /// 项目状态
    /// </summary>
    public ProjectStatus? Status { get; set; }
}

/// <summary>
/// 初始化项目DTO
/// </summary>
public class InitializeProjectDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 目标路径（可选，如果不指定则使用项目记录中的路径）
    /// </summary>
    public string? TargetPath { get; set; }
}

/// <summary>
/// 客户端初始化项目DTO（用于客户端 WebView）
/// </summary>
public class ClientInitializeProjectDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 模板 ZIP 文件的 Base64 编码
    /// </summary>
    public string TemplateBase64 { get; set; } = string.Empty;

    /// <summary>
    /// 项目名称
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 项目路径（客户端本地路径）
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// 数据库类型
    /// </summary>
    public string DatabaseType { get; set; } = string.Empty;

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// 导出模板DTO
/// </summary>
public class ExportTemplateDto
{
    /// <summary>
    /// 输出路径
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;
}
