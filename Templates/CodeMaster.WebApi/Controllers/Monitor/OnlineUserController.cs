using CodeMaster.Application.Dtos.Monitor;
using CodeMaster.Application.Services.Monitor;
using CodeMaster.Core.Common;
using CodeMaster.WebApi.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CodeMaster.WebApi.Controllers.Monitor;

[ApiController]
[Route("api/monitor/online")]
[Authorize]
public class OnlineUserController : ControllerBase
{
    private readonly IOnlineUserService _onlineUserService;
    private readonly IHubContext<OnlineUserHub> _hubContext;

    public OnlineUserController(
        IOnlineUserService onlineUserService,
        IHubContext<OnlineUserHub> hubContext)
    {
        _onlineUserService = onlineUserService;
        _hubContext = hubContext;
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
            // 通知客户端强制下线
            await _hubContext.Clients.All.SendAsync("ForceOffline", userId);
            return Ok(ApiResponse<bool>.Success(true, "强制下线成功"));
        }
        return BadRequest(ApiResponse<bool>.Fail("用户不在线或操作失败"));
    }
}
