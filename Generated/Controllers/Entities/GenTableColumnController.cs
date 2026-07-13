using Microsoft.AspNetCore.Mvc;
using CodeMaster.Core.Common;
using CodeMaster.Application.Dtos.Entities;
using CodeMaster.Application.Services.Entities;

namespace CodeMaster.WebApi.Controllers.Entities;

/// <summary>
/// GenTableColumn
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GenTableColumnController : ControllerBase
{
    private readonly IGenTableColumnService _genTableColumnService;

    public GenTableColumnController(IGenTableColumnService genTableColumnService)
    {
        _genTableColumnService = genTableColumnService;
    }

    /// <summary>
    /// 分页查询GenTableColumn
    /// </summary>
    [HttpGet("page")]
    public async Task<ApiResponse<PagedResultDto<GenTableColumnDto>>> GetPagedListAsync([FromQuery] GenTableColumnQueryDto query)
    {
        var result = await _genTableColumnService.GetPagedListAsync(query);
        return ApiResponse<PagedResultDto<GenTableColumnDto>>.Success(result);
    }

    /// <summary>
    /// 获取GenTableColumn详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<GenTableColumnDto>> GetByIdAsync(long id)
    {
        var result = await _genTableColumnService.GetByIdAsync(id);
        return ApiResponse<GenTableColumnDto>.Success(result);
    }

    /// <summary>
    /// 创建GenTableColumn
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<long>> CreateAsync([FromBody] CreateGenTableColumnDto dto)
    {
        var id = await _genTableColumnService.CreateAsync(dto);
        return ApiResponse<long>.Success(id);
    }

    /// <summary>
    /// 更新GenTableColumn
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse<bool>> UpdateAsync(long id, [FromBody] UpdateGenTableColumnDto dto)
    {
        await _genTableColumnService.UpdateAsync(id, dto);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 删除GenTableColumn
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> DeleteAsync(long id)
    {
        await _genTableColumnService.DeleteAsync(id);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 批量删除GenTableColumn
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ApiResponse<bool>> BatchDeleteAsync([FromBody] long[] ids)
    {
        await _genTableColumnService.BatchDeleteAsync(ids);
        return ApiResponse<bool>.Success(true);
    }
}
