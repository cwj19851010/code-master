using Microsoft.AspNetCore.Mvc;

namespace CodeMaster.WebApi.Controllers.System;

/// <summary>
/// 健康检查控制器
/// </summary>
public class HealthController : BaseController
{
    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet("check")]
    public IActionResult Check()
    {
        return Success(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        }, "系统运行正常");
    }

    /// <summary>
    /// 获取系统信息
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Success(new
        {
            projectName = "CodeMaster",
            description = "代码大师 - 企业级快速开发平台",
            version = "1.0.0",
            framework = ".NET 8.0",
            database = "SQL Server",
            orm = "EF Core + SqlSugar",
            ui = "Vue 3 + Element Plus"
        });
    }
}
