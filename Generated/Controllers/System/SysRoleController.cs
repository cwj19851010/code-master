using Microsoft.AspNetCore.Mvc;
using CodeMaster.Core.Common;
using CodeMaster.Application.Dtos.System;
using CodeMaster.Application.Services.System;

namespace CodeMaster.WebApi.Controllers.System;

/// <summary>
/// SysRole
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SysRoleController : ControllerBase
{
    private readonly ISysRoleService _roleService;

    public SysRoleController(ISysRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// 分页查询SysRole
    /// </summary>
    [HttpGet("page")]
    public async Task<ApiResponse<PagedResultDto<SysRoleDto>>> GetPagedListAsync([FromQuery] SysRoleQueryDto query)
    {
        var result = await _roleService.GetPagedListAsync(query);
        return ApiResponse<PagedResultDto<SysRoleDto>>.Success(result);
    }

    /// <summary>
    /// 获取SysRole详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<SysRoleDto>> GetByIdAsync(long id)
    {
        var result = await _roleService.GetByIdAsync(id);
        return ApiResponse<SysRoleDto>.Success(result);
    }

    /// <summary>
    /// 创建SysRole
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<long>> CreateAsync([FromBody] CreateSysRoleDto dto)
    {
        var id = await _roleService.CreateAsync(dto);
        return ApiResponse<long>.Success(id);
    }

    /// <summary>
    /// 更新SysRole
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse<bool>> UpdateAsync(long id, [FromBody] UpdateSysRoleDto dto)
    {
        await _roleService.UpdateAsync(id, dto);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 删除SysRole
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> DeleteAsync(long id)
    {
        await _roleService.DeleteAsync(id);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 批量删除SysRole
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ApiResponse<bool>> BatchDeleteAsync([FromBody] long[] ids)
    {
        await _roleService.BatchDeleteAsync(ids);
        return ApiResponse<bool>.Success(true);
    }
}
