namespace CodeMaster.Application.Dtos.System;

/// <summary>
/// 文件查询DTO
/// </summary>
public class SysFileQueryDto
{
    /// <summary>
    /// 原始文件名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 文件分类
    /// </summary>
    public string? FileCategory { get; set; }

    /// <summary>
    /// 存储类型
    /// </summary>
    public int? StoreType { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? BeginTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int PageNum { get; set; } = 1;

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// 文件DTO
/// </summary>
public class SysFileDto
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 原始文件名
    /// </summary>
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 存储文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string FileExt { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件URL
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// 访问URL
    /// </summary>
    public string? AccessUrl { get; set; }

    /// <summary>
    /// 存储类型
    /// </summary>
    public int StoreType { get; set; }

    /// <summary>
    /// 文件分类
    /// </summary>
    public string? FileCategory { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
}
