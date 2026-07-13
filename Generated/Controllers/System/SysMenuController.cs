using Microsoft.AspNetCore.Mvc;
using CodeMaster.Core.Common;
using CodeMaster.Application.Dtos.System;
using CodeMaster.Application.Services.System;

namespace CodeMaster.WebApi.Controllers.System;

/// <summary>
/// SysMenu
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SysMenuController : ControllerBase
{
    private readonly ISysMenuService _menuService;

    public SysMenuController(ISysMenuService menuService)
    {
        _menuService = menuService;
    }

    /// <summary>
    /// 分页查询SysMenu
    /// </summary>
    [HttpGet("page")]
    public async Task<ApiResponse<PagedResultDto<SysMenuDto>>> GetPagedListAsync([FromQuery] SysMenuQueryDto query)
    {
        var result = await _menuService.GetPagedListAsync(query);
        return ApiResponse<PagedResultDto<SysMenuDto>>.Success(result);
    }

    /// <summary>
    /// 获取SysMenu详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<SysMenuDto>> GetByIdAsync(long id)
    {
        var result = await _menuService.GetByIdAsync(id);
        return ApiResponse<SysMenuDto>.Success(result);
    }

    /// <summary>
    /// 创建SysMenu
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<long>> CreateAsync([FromBody] CreateSysMenuDto dto)
    {
        var id = await _menuService.CreateAsync(dto);
        return ApiResponse<long>.Success(id);
    }

    /// <summary>
    /// 更新SysMenu
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse<bool>> UpdateAsync(long id, [FromBody] UpdateSysMenuDto dto)
    {
        await _menuService.UpdateAsync(id, dto);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 删除SysMenu
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> DeleteAsync(long id)
    {
        await _menuService.DeleteAsync(id);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 批量删除SysMenu
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ApiResponse<bool>> BatchDeleteAsync([FromBody] long[] ids)
    {
        await _menuService.BatchDeleteAsync(ids);
        return ApiResponse<bool>.Success(true);
    }
}
