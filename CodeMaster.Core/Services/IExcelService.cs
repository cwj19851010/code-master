using System.Reflection;
using CodeMaster.Core.Attributes;

namespace CodeMaster.Core.Services;

/// <summary>
/// Excel 导入导出服务接口
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// 导出数据到 Excel
    /// </summary>
    /// <typeparam name="T">DTO 类型</typeparam>
    /// <param name="data">数据列表</param>
    /// <param name="sheetName">工作表名称</param>
    /// <returns>Excel 文件字节数组</returns>
    Task<byte[]> ExportAsync<T>(List<T> data, string sheetName = "Sheet1") where T : class;

    /// <summary>
    /// 从 Excel 导入数据
    /// </summary>
    /// <typeparam name="T">DTO 类型</typeparam>
    /// <param name="fileBytes">Excel 文件字节数组</param>
    /// <param name="sheetName">工作表名称</param>
    /// <returns>数据列表</returns>
    Task<List<T>> ImportAsync<T>(byte[] fileBytes, string sheetName = "Sheet1") where T : class, new();
}

/// <summary>
/// 导出列信息
/// </summary>
public class ExportColumnInfo
{
    /// <summary>
    /// 属性信息
    /// </summary>
    public PropertyInfo Property { get; set; } = null!;

    /// <summary>
    /// 列名
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 翻译键
    /// </summary>
    public string? TitleKey { get; set; }

    /// <summary>
    /// 顺序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 是否是外键
    /// </summary>
    public bool IsForeignKey { get; set; }

    /// <summary>
    /// 外键配置
    /// </summary>
    public ExportForeignKeyAttribute? ForeignKeyConfig { get; set; }
}
