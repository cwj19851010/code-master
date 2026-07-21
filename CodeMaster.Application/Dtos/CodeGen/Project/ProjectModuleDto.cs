using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 项目模块DTO
/// </summary>
public class ProjectModuleDto : EntityDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 模块名称（英文）
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述（中文）
    /// </summary>
    public string ModuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 路由路径
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// 是否已同步
    /// </summary>
    public bool IsSynced { get; set; }

    /// <summary>
    /// 最后同步时间
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 项目模块查询DTO
/// </summary>
public class ProjectModuleQueryDto : PagedQueryDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long? ProjectId { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string? ModuleName { get; set; }
}

/// <summary>
/// 创建项目模块DTO
/// </summary>
public class CreateProjectModuleDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 模块名称（英文）
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述（中文）
    /// </summary>
    public string ModuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 路由路径
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新项目模块DTO
/// </summary>
public class UpdateProjectModuleDto
{
    /// <summary>
    /// 模块名称（英文）
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述（中文）
    /// </summary>
    public string ModuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 路由路径
    /// </summary>
    public string? RoutePath { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 同步模块到菜单DTO
/// </summary>
public class SyncModuleToMenuDto
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public long ModuleId { get; set; }
}

/// <summary>
/// 客户端同步模块到菜单DTO
/// </summary>
public class ClientSyncModuleToMenuDto
{
    /// <summary>
    /// 模块ID
    /// </summary>
    public long ModuleId { get; set; }

    /// <summary>
    /// 项目ID
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 项目名称
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 项目路径
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// 数据库类型
    /// </summary>
    public string DatabaseType { get; set; } = string.Empty;

    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 模块名称（英文）
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述（中文）
    /// </summary>
    public string ModuleDescription { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }
}
