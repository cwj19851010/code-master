using System.Security.Cryptography;
using System.Text;
using CodeMaster.Application.Dtos.System;
using CodeMaster.Core.Dtos;
using CodeMaster.Core.Repositories;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.System;
using Mapster;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using CodeMaster.Core.Authorization;

namespace CodeMaster.Application.Services.System;

/// <summary>
/// 文件服务接口
/// </summary>
public interface ISysFileService : IApplicationService
{
    Task<SysFileDto> SaveFileToLocalAsync(IFormFile file, string? category = null);
    Task<SysFileDto?> GetFileByIdAsync(long id);
    Task<PagedResultDto<SysFileDto>> GetPagedListAsync([FromQuery] SysFileQueryDto query);
    Task<int> DeleteFileAsync(long id);
    Task<int> DeleteBatchAsync(long[] ids);
    Task<(Stream stream, string contentType, string fileName)> DownloadFileAsync(long id);
}

/// <summary>
/// 文件服务实现
/// </summary>
public class SysFileService : ISysFileService
{
    private readonly IRepository<SysFile> _fileRepository;
    private readonly ISqlSugarClient _db;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment? _webHostEnvironment;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    public SysFileService(
        IRepository<SysFile> fileRepository,
        ISqlSugarClient db,
        IConfiguration configuration,
        IWebHostEnvironment? webHostEnvironment = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _fileRepository = fileRepository;
        _db = db;
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<SysFileDto> SaveFileToLocalAsync(IFormFile file, string? category = null)
    {
        if (file == null || file.Length == 0)
            throw new Exception("文件不能为空");

        var fileExt = Path.GetExtension(file.FileName).ToLower();
        var uploadPath = "upload";
        var datePath = $"{uploadPath}/{DateTime.UtcNow:yyyy/MMdd}";
        var fileName = HashFileName(file.FileName);

        // 存储到 wwwroot/upload/yyyy/MMdd/
        var webRootPath = _webHostEnvironment?.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
            webRootPath = Path.Combine(_webHostEnvironment?.ContentRootPath ?? AppContext.BaseDirectory, "wwwroot");

        var storeDir = Path.Combine(webRootPath, datePath);
        if (!Directory.Exists(storeDir))
            Directory.CreateDirectory(storeDir);

        var filePath = Path.Combine(storeDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        // 拼接后端域名
        var accessUrl = $"/{datePath}/{fileName}".Replace("\\", "/");
        var serverUrl = _configuration["Upload:ServerUrl"];
        if (string.IsNullOrWhiteSpace(serverUrl) && _httpContextAccessor?.HttpContext != null)
        {
            var req = _httpContextAccessor.HttpContext.Request;
            serverUrl = $"{req.Scheme}://{req.Host}{req.PathBase}";
        }
        if (!string.IsNullOrWhiteSpace(serverUrl))
            accessUrl = $"{serverUrl.TrimEnd('/')}{accessUrl}";

        // 保存文件记录
        var fileEntity = new SysFile
        {
            RealName = file.FileName,
            FileName = fileName,
            FileType = file.ContentType,
            FileExt = fileExt,
            FileSize = file.Length,
            FileUrl = filePath,
            StorePath = datePath,
            AccessUrl = accessUrl,
            StoreType = 1,
            FileCategory = category
        };

        await _fileRepository.InsertAsync(fileEntity);

        return fileEntity.Adapt<SysFileDto>();
    }

    [Permission("system:file:view")]
    public async Task<SysFileDto?> GetFileByIdAsync(long id)
    {
        var entity = await _fileRepository.GetByIdAsync(id);
        return entity?.Adapt<SysFileDto>();
    }

    [Permission("system:file:list")]
    public async Task<PagedResultDto<SysFileDto>> GetPagedListAsync(SysFileQueryDto query)
    {
        RefAsync<int> totalCount = 0;
        var items = await _db.Queryable<SysFile>()
            .WhereIF(!string.IsNullOrEmpty(query.RealName), f => f.RealName.Contains(query.RealName!))
            .WhereIF(!string.IsNullOrEmpty(query.FileCategory), f => f.FileCategory == query.FileCategory)
            .WhereIF(query.StoreType != null, f => f.StoreType == query.StoreType!.Value)
            .WhereIF(query.BeginTime != null, f => f.CreateTime >= query.BeginTime!.Value)
            .WhereIF(query.EndTime != null, f => f.CreateTime <= query.EndTime!.Value)
            .OrderByDescending(f => f.CreateTime)
            .ToPageListAsync(query.PageNum, query.PageSize, totalCount);

        return new PagedResultDto<SysFileDto>
        {
            Items = items.Adapt<List<SysFileDto>>(),
            Total = totalCount.Value,
            PageNum = query.PageNum,
            PageSize = query.PageSize
        };
    }

    [Permission("system:file:delete")]
    public async Task<int> DeleteFileAsync(long id)
    {
        var entity = await _fileRepository.GetByIdAsync(id);
        if (entity == null)
        {
            return 0;
        }

        // 删除物理文件
        if (entity.StoreType == 1 && File.Exists(entity.FileUrl))
        {
            try
            {
                File.Delete(entity.FileUrl);
            }
            catch
            {
                // 忽略文件删除失败
            }
        }

        // 删除数据库记录
        return await _fileRepository.DeleteAsync(id);
    }

    [Permission("system:file:delete")]
    public async Task<int> DeleteBatchAsync(long[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            return 0;
        }

        // 获取所有要删除的文件
        var files = await _db.Queryable<SysFile>()
            .Where(f => ids.Contains(f.Id))
            .ToListAsync();

        // 删除物理文件
        foreach (var file in files)
        {
            if (file.StoreType == 1 && File.Exists(file.FileUrl))
            {
                try
                {
                    File.Delete(file.FileUrl);
                }
                catch
                {
                    // 忽略文件删除失败
                }
            }
        }

        // 删除数据库记录
        return await _db.Deleteable<SysFile>()
            .Where(f => ids.Contains(f.Id))
            .ExecuteCommandAsync();
    }

    public async Task<(Stream stream, string contentType, string fileName)> DownloadFileAsync(long id)
    {
        var entity = await _fileRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception("文件不存在");
        }

        if (!File.Exists(entity.FileUrl))
        {
            throw new Exception("文件已被删除");
        }

        var stream = new FileStream(entity.FileUrl, FileMode.Open, FileAccess.Read);
        return (stream, entity.FileType, entity.RealName);
    }

    /// <summary>
    /// 生成哈希文件名
    /// </summary>
    private string HashFileName(string originalFileName)
    {
        var ext = Path.GetExtension(originalFileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var hash = ComputeMd5Hash(nameWithoutExt + timestamp);
        return $"{hash}{ext}";
    }

    /// <summary>
    /// 计算MD5哈希
    /// </summary>
    private string ComputeMd5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }
}
