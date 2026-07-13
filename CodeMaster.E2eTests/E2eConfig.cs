using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeMaster.E2eTests;

/// <summary>
/// E2E 测试配置
/// </summary>
public static class E2eConfig
{
    /// <summary>
    /// CodeMaster 后台 API 基地址
    /// </summary>
    public static string BaseUrl { get; } = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5000";

    /// <summary>
    /// 测试项目名称
    /// </summary>
    public static string TestProjectName { get; } = "E2eTestProject";

    /// <summary>
    /// 测试项目路径父目录
    /// </summary>
    public static string TestProjectParentPath { get; } =
        Environment.GetEnvironmentVariable("E2E_PROJECT_PATH") ?? "C:/E2eTestProjects";

    /// <summary>
    /// 测试项目完整路径
    /// </summary>
    public static string TestProjectFullPath { get; } =
        Path.Combine(TestProjectParentPath, TestProjectName);

    /// <summary>
    /// 测试项目后端端口
    /// </summary>
    public static int BackendPort { get; } = 15200;

    /// <summary>
    /// 测试项目前端端口
    /// </summary>
    public static int FrontendPort { get; } = 15201;

    /// <summary>
    /// 测试数据库连接字符串（SQLite，相对路径，初始化时会被转换为绝对路径）
    /// </summary>
    public static string ConnectionString { get; } = $"Data Source=../{TestProjectName}.db";
}

/// <summary>
/// 测试状态共享（在顺序执行的测试集合之间共享状态）
/// </summary>
public static class TestState
{
    public static long ProjectId { get; set; }
    public static long ModuleId { get; set; }
    public static long ArticleEntityId { get; set; }
    public static long ArticleCommentEntityId { get; set; }
}
