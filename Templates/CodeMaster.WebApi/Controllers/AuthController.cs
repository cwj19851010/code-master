using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CodeMaster.Application.Dtos.Auth;
using CodeMaster.Application.Services.Auth;
using CodeMaster.Application.Services.System;
using CodeMaster.Core.Common;

namespace CodeMaster.WebApi.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISysMenuService _menuService;

    public AuthController(IAuthService authService, ISysMenuService menuService)
    {
        _authService = authService;
        _menuService = menuService;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
           
            var result = await _authService.LoginAsync(loginDto);
            return Ok(ApiResponse<LoginResultDto>.Success(result, "登录成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("info")]
    [Authorize]
    public async Task<IActionResult> GetUserInfo()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized(ApiResponse<object>.Fail("未授权"));
        }

        var userId = long.Parse(userIdClaim.Value);
        var userInfo = await _authService.GetUserInfoAsync(userId);

        if (userInfo == null)
        {
            return NotFound(ApiResponse<object>.Fail("用户不存在"));
        }

        // 转换为前端期望的格式
        var response = new UserInfoResponseDto
        {
            User = new UserBasicDto
            {
                UserId = userInfo.UserId,
                UserName = userInfo.UserName,
                NickName = userInfo.NickName,
                Avatar = userInfo.Avatar,
                DeptId = userInfo.DeptId,
                DeptName = userInfo.DeptName,
                IsAdmin = userInfo.IsAdmin,
                IsHostAdmin = userInfo.IsHostAdmin,
                IsTenantAdmin = userInfo.IsTenantAdmin
            },
            Roles = userInfo.Roles,
            Permissions = userInfo.Permissions,
            IsAdmin = userInfo.IsAdmin,
            IsHostAdmin = userInfo.IsHostAdmin,
            IsTenantAdmin = userInfo.IsTenantAdmin
        };

        return Ok(ApiResponse<UserInfoResponseDto>.Success(response, "获取成功"));
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "退出成功" });
    }

    /// <summary>
    /// 获取用户路由
    /// </summary>
    [HttpGet("getRouters")]
    [Authorize]
    public async Task<IActionResult> GetRouters()
    {
        var routes = await _menuService.GetUserRoutesAsync();
        return Ok(ApiResponse<List<RouteDto>>.Success(routes, "获取成功"));
    }
}
