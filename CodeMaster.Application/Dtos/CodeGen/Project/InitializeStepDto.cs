using CodeMaster.Core.Enums;

namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 初始化步骤请求DTO
/// </summary>
public class InitializeStepDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 目标路径（可选，如果不指定则使用项目记录中的路径）
    /// </summary>
    public string? TargetPath { get; set; }
}

/// <summary>
/// 初始化步骤结果DTO
/// </summary>
public class InitializeStepResultDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 当前步骤
    /// </summary>
    public string Step { get; set; } = string.Empty;

    /// <summary>
    /// 进度百分比（0-100）
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 额外数据
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// 初始化状态DTO
/// </summary>
public class InitializationStateDto
{
    public bool Extracted { get; set; }
    public bool Renamed { get; set; }
    public bool Replaced { get; set; }
    public bool SolutionGenerated { get; set; }
    public bool DatabaseConfigured { get; set; }
    public bool PortConfigured { get; set; }
    public bool MigrationCreated { get; set; }
    public bool MigrationApplied { get; set; }
    public bool DotnetRestored { get; set; }
    public bool TranslationsWritten { get; set; }
    public bool NpmInstalled { get; set; }
    public bool BackendStarted { get; set; }
    public bool FrontendStarted { get; set; }
}

/// <summary>
/// 项目操作请求DTO（启动/停止/迁移/编译等）
/// </summary>
public class ProjectActionDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long ProjectId { get; set; }
}

/// <summary>
/// 项目操作结果DTO
/// </summary>
public class ProjectActionResultDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 命令输出（编译/迁移日志）
    /// </summary>
    public string? Output { get; set; }
}

/// <summary>
/// 项目前后端运行状态DTO
/// </summary>
public class ProjectStatusDto
{
    /// <summary>
    /// 后端是否运行中
    /// </summary>
    public bool BackendRunning { get; set; }

    /// <summary>
    /// 前端是否运行中
    /// </summary>
    public bool FrontendRunning { get; set; }

    /// <summary>
    /// 后端进程ID
    /// </summary>
    public int? BackendPid { get; set; }

    /// <summary>
    /// 前端进程ID
    /// </summary>
    public int? FrontendPid { get; set; }
}

/// <summary>
/// 字典类型选项DTO
/// </summary>
public class DictTypeOptionDto
{
    public string DictType { get; set; } = string.Empty;
    public string DictName { get; set; } = string.Empty;
}
