namespace CodeMaster.Core.Enums;

/// <summary>
/// 项目状态
/// </summary>
public enum ProjectStatus
{
    /// <summary>
    /// 未初始化
    /// </summary>
    NotInitialized = 0,

    /// <summary>
    /// 已初始化
    /// </summary>
    Initialized = 1,

    /// <summary>
    /// 运行中
    /// </summary>
    Running = 2,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped = 3,

    /// <summary>
    /// 初始化失败
    /// </summary>
    InitializeFailed = 4
}
