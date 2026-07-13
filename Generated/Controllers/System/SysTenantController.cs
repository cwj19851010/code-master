using Microsoft.AspNetCore.Mvc;
using CodeMaster.Core.Common;
using CodeMaster.Application.Dtos.System;
using CodeMaster.Application.Services.System;

namespace CodeMaster.WebApi.Controllers.System;

/// <summary>
/// SysTenant
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SysTenantController : ControllerBase
{
    private readonly ISysTenantService _tenantService;

    public SysTenantController(ISysTenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// 分页查询SysTenant
    /// </summary>
    [HttpGet("page")]
    public async Task<ApiResponse<PagedResultDto<SysTenantDto>>> GetPagedListAsync([FromQuery] SysTenantQueryDto query)
    {
        var result = await _tenantService.GetPagedListAsync(query);
        return ApiResponse<PagedResultDto<SysTenantDto>>.Success(result);
    }

    /// <summary>
    /// 获取SysTenant详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<SysTenantDto>> GetByIdAsync(long id)
    {
        var result = await _tenantService.GetByIdAsync(id);
        return ApiResponse<SysTenantDto>.Success(result);
    }

    /// <summary>
    /// 创建SysTenant
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<long>> CreateAsync([FromBody] CreateSysTenantDto dto)
    {
        var id = await _tenantService.CreateAsync(dto);
        return ApiResponse<long>.Success(id);
    }

    /// <summary>
    /// 更新SysTenant
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse<bool>> UpdateAsync(long id, [FromBody] UpdateSysTenantDto dto)
    {
        await _tenantService.UpdateAsync(id, dto);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 删除SysTenant
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> DeleteAsync(long id)
    {
        await _tenantService.DeleteAsync(id);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 批量删除SysTenant
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ApiResponse<bool>> BatchDeleteAsync([FromBody] long[] ids)
    {
        await _tenantService.BatchDeleteAsync(ids);
        return ApiResponse<bool>.Success(true);
    }
}
