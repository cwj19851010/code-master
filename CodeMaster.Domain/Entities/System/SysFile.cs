using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 文件表
/// </summary>
[SugarTable("sys_file")]
public class SysFile : EntityBase
{
    /// <summary>
    /// 原始文件名
    /// </summary>
    [SugarColumn(ColumnName = "real_name", Length = 200)]
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 存储文件名（哈希）
    /// </summary>
    [SugarColumn(ColumnName = "file_name", Length = 200)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件MIME类型
    /// </summary>
    [SugarColumn(ColumnName = "file_type", Length = 100)]
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [SugarColumn(ColumnName = "file_ext", Length = 20)]
    public string FileExt { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [SugarColumn(ColumnName = "file_size")]
    public long FileSize { get; set; }

    /// <summary>
    /// 文件物理路径
    /// </summary>
    [SugarColumn(ColumnName = "file_url", Length = 500)]
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// 存储目录
    /// </summary>
    [SugarColumn(ColumnName = "store_path", Length = 500)]
    public string StorePath { get; set; } = string.Empty;

    /// <summary>
    /// 访问URL
    /// </summary>
    [SugarColumn(ColumnName = "access_url", Length = 500, IsNullable = true)]
    public string? AccessUrl { get; set; }

    /// <summary>
    /// 存储类型（1本地 2阿里云OSS）
    /// </summary>
    [SugarColumn(ColumnName = "store_type")]
    public int StoreType { get; set; } = 1;

    /// <summary>
    /// 文件分类（avatar/document/image等）
    /// </summary>
    [SugarColumn(ColumnName = "file_category", Length = 50, IsNullable = true)]
    public string? FileCategory { get; set; }
}
