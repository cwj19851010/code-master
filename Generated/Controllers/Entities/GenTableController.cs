using Microsoft.AspNetCore.Mvc;
using CodeMaster.Core.Common;
using CodeMaster.Application.Dtos.Entities;
using CodeMaster.Application.Services.Entities;

namespace CodeMaster.WebApi.Controllers.Entities;

/// <summary>
/// GenTable
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GenTableController : ControllerBase
{
    private readonly IGenTableService _genTableService;

    public GenTableController(IGenTableService genTableService)
    {
        _genTableService = genTableService;
    }

    /// <summary>
    /// 分页查询GenTable
    /// </summary>
    [HttpGet("page")]
    public async Task<ApiResponse<PagedResultDto<GenTableDto>>> GetPagedListAsync([FromQuery] GenTableQueryDto query)
    {
        var result = await _genTableService.GetPagedListAsync(query);
        return ApiResponse<PagedResultDto<GenTableDto>>.Success(result);
    }

    /// <summary>
    /// 获取GenTable详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ApiResponse<GenTableDto>> GetByIdAsync(long id)
    {
        var result = await _genTableService.GetByIdAsync(id);
        return ApiResponse<GenTableDto>.Success(result);
    }

    /// <summary>
    /// 创建GenTable
    /// </summary>
    [HttpPost]
    public async Task<ApiResponse<long>> CreateAsync([FromBody] CreateGenTableDto dto)
    {
        var id = await _genTableService.CreateAsync(dto);
        return ApiResponse<long>.Success(id);
    }

    /// <summary>
    /// 更新GenTable
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ApiResponse<bool>> UpdateAsync(long id, [FromBody] UpdateGenTableDto dto)
    {
        await _genTableService.UpdateAsync(id, dto);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 删除GenTable
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ApiResponse<bool>> DeleteAsync(long id)
    {
        await _genTableService.DeleteAsync(id);
        return ApiResponse<bool>.Success(true);
    }

    /// <summary>
    /// 批量删除GenTable
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ApiResponse<bool>> BatchDeleteAsync([FromBody] long[] ids)
    {
        await _genTableService.BatchDeleteAsync(ids);
        return ApiResponse<bool>.Success(true);
    }
}
