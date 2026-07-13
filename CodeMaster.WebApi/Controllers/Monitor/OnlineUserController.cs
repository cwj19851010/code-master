using CodeMaster.Application.Dtos.Monitor;
using CodeMaster.Application.Services.Monitor;
using CodeMaster.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeMaster.WebApi.Controllers.Monitor;

[ApiController]
[Route("api/monitor/online")]
[Authorize]
public class OnlineUserController : ControllerBase
{
    private readonly IOnlineUserService _onlineUserService;

    public OnlineUserController(IOnlineUserService onlineUserService)
    {
        _onlineUserService = onlineUserService;
    }

    /// <summary>
    /// 获取在线用户列表
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetOnlineUsers()
    {
        var users = await _onlineUserService.GetOnlineUsersAsync();
        return Ok(ApiResponse<List<OnlineUserDto>>.Success(users));
    }

    /// <summary>
    /// 强制用户下线
    /// </summary>
    [HttpPost("force-offline/{userId}")]
    public async Task<IActionResult> ForceOffline(long userId)
    {
        var result = await _onlineUserService.ForceOfflineAsync(userId);
        if (result)
        {
            return Ok(ApiResponse<bool>.Success(true, "强制下线成功"));
        }
        return BadRequest(ApiResponse<bool>.Fail("用户不在线或操作失败"));
    }
}
