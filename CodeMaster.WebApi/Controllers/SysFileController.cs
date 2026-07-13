using CodeMaster.Application.Dtos.System;
using CodeMaster.Application.Services.System;
using CodeMaster.Core.Attributes;
using CodeMaster.Core.Authorization;
using CodeMaster.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CodeMaster.WebApi.Controllers;

/// <summary>
/// 文件管理控制器
/// </summary>
[ApiController]
[Route("api/system/file")]
public class SysFileController : BaseController
{
    private readonly ISysFileService _fileService;

    public SysFileController(ISysFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    [HttpPost("upload")]
    [Permission("system:file:upload")]
    [Log("上传文件", BusinessType.Insert)]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? category = null)
    {
        try
        {
            var result = await _fileService.SaveFileToLocalAsync(file, category);
            return Success(result, "上传成功");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    /// <summary>
    /// 获取文件列表
    /// </summary>
    [HttpGet("list")]
    [Permission("system:file:list")]
    public async Task<IActionResult> GetList([FromQuery] SysFileQueryDto query)
    {
        var result = await _fileService.GetPagedListAsync(query);
        return PageSuccess(result.Items, (int)result.Total);
    }

    /// <summary>
    /// 获取文件详情
    /// </summary>
    [HttpGet("{id}")]
    [Permission("system:file:query")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _fileService.GetFileByIdAsync(id);
        if (result == null)
        {
            return Fail("文件不存在");
        }
        return Success(result);
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    [HttpGet("download/{id}")]
    [Permission("system:file:download")]
    [Log("下载文件", BusinessType.Export)]
    public async Task<IActionResult> Download(long id)
    {
        try
        {
            var (stream, contentType, fileName) = await _fileService.DownloadFileAsync(id);
            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    [HttpDelete("{id}")]
    [Permission("system:file:remove")]
    [Log("删除文件", BusinessType.Delete)]
    public async Task<IActionResult> Delete(long id)
    {
        var count = await _fileService.DeleteFileAsync(id);
        if (count > 0)
        {
            return Success(null, "删除成功");
        }
        return Fail("删除失败");
    }

    /// <summary>
    /// 批量删除文件
    /// </summary>
    [HttpDelete]
    [Permission("system:file:remove")]
    [Log("批量删除文件", BusinessType.Delete)]
    public async Task<IActionResult> DeleteBatch([FromBody] long[] ids)
    {
        var count = 0;
        foreach (var id in ids)
        {
            count += await _fileService.DeleteFileAsync(id);
        }
        return Success(count, $"成功删除 {count} 个文件");
    }
}
