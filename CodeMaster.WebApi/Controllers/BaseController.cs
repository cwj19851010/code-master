using Microsoft.AspNetCore.Mvc;

namespace CodeMaster.WebApi.Controllers;

/// <summary>
/// 基础控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// 成功响应
    /// </summary>
    protected IActionResult Success(object? data = null, string message = "操作成功")
    {
        return Ok(new
        {
            code = 200,
            msg = message,
            data
        });
    }

    /// <summary>
    /// 失败响应
    /// </summary>
    protected IActionResult Fail(string message = "操作失败", int code = 500)
    {
        return Ok(new
        {
            code,
            msg = message,
            data = (object?)null
        });
    }

    /// <summary>
    /// 分页响应
    /// </summary>
    protected IActionResult PageSuccess<T>(List<T> items, int total, string message = "查询成功")
    {
        return Ok(new
        {
            code = 200,
            msg = message,
            data = new
            {
                items,
                total
            }
        });
    }
}
