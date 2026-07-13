using Microsoft.AspNetCore.Mvc;
using CodeMaster.Core.Common;
using CodeMaster.Application.Dtos.System;
using CodeMaster.Application.Services.System;

namespace CodeMaster.WebApi.Controllers.System;

/// <summary>
/// SysDept
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SysDeptController : ControllerBase
{
    private readonly ISysDeptService _deptService;

    public SysDeptController(ISysDeptService deptService)
    {
        _deptService = deptService;
    }

    /// <summary>
    /// 分页查询SysDept
    /// </summary>
    [HttpGet("page")]
    public async Task<ApiResponse<PagedResultDto<SysDeptDto>>> GetPagedListAsync([FromQuery] SysDeptQueryDto query)
    {
        var result = await _deptService.GetPagedListAsync(query);
        return ApiResponse<PagedResultDto<SysDeptDto>>.Success(result);
    }

    /// <summary>
    /// 获取SysDept详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<SysDeptDto>> GetByIdAsync(long id)
    {
        var result = await _deptService.GetByIdAsync(id);
        return ApiResponse<SysDeptDto>.Success(result);
    }

    /// <summary>
    /// 创建SysDept
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<long>> CreateAsync([FromBody] CreateSysDeptDto dto)
    {
        var id = await _deptService.CreateAsync(dto);
        return ApiResponse<long>.Success(id);
    }

    /// <summary>
    /// 更新SysDept
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse<bool>> UpdateAsync(long id, [FromBody] UpdateSysDeptDto dto)
    {
        await _deptService.UpdateAsync(id, dto);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 删除SysDept
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> DeleteAsync(long id)
    {
        await _deptService.DeleteAsync(id);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 批量删除SysDept
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ApiResponse<bool>> BatchDeleteAsync([FromBody] long[] ids)
    {
        await _deptService.BatchDeleteAsync(ids);
        return ApiResponse<bool>.Success(true);
    }
}
