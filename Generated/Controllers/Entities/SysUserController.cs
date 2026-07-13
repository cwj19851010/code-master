using Microsoft.AspNetCore.Mvc;
using CodeMaster.Core.Common;
using CodeMaster.Application.Dtos.Entities;
using CodeMaster.Application.Services.Entities;

namespace CodeMaster.WebApi.Controllers.Entities;

/// <summary>
/// SysUser
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SysUserController : ControllerBase
{
    private readonly ISysUserService _userService;

    public SysUserController(ISysUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 分页查询SysUser
    /// </summary>
    [HttpGet("page")]
    public async Task<ApiResponse<PagedResultDto<SysUserDto>>> GetPagedListAsync([FromQuery] SysUserQueryDto query)
    {
        var result = await _userService.GetPagedListAsync(query);
        return ApiResponse<PagedResultDto<SysUserDto>>.Success(result);
    }

    /// <summary>
    /// 获取SysUser详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<SysUserDto>> GetByIdAsync(long id)
    {
        var result = await _userService.GetByIdAsync(id);
        return ApiResponse<SysUserDto>.Success(result);
    }

    /// <summary>
    /// 创建SysUser
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<long>> CreateAsync([FromBody] CreateSysUserDto dto)
    {
        var id = await _userService.CreateAsync(dto);
        return ApiResponse<long>.Success(id);
    }

    /// <summary>
    /// 更新SysUser
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse<bool>> UpdateAsync(long id, [FromBody] UpdateSysUserDto dto)
    {
        await _userService.UpdateAsync(id, dto);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 删除SysUser
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> DeleteAsync(long id)
    {
        await _userService.DeleteAsync(id);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 批量删除SysUser
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ApiResponse<bool>> BatchDeleteAsync([FromBody] long[] ids)
    {
        await _userService.BatchDeleteAsync(ids);
        return ApiResponse<bool>.Success(true);
    }
}
