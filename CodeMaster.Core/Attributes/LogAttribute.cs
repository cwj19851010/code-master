using CodeMaster.Core.Enums;

namespace CodeMaster.Core.Attributes;

/// <summary>
/// 操作日志记录特性
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LogAttribute : Attribute
{
    /// <summary>
    /// 模块标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 业务类型
    /// </summary>
    public BusinessType BusinessType { get; set; } = BusinessType.Other;

    /// <summary>
    /// 操作人类别
    /// </summary>
    public OperatorType OperatorType { get; set; } = OperatorType.Manage;

    /// <summary>
    /// 是否保存请求数据
    /// </summary>
    public bool IsSaveRequestData { get; set; } = true;

    /// <summary>
    /// 是否保存响应数据
    /// </summary>
    public bool IsSaveResponseData { get; set; } = true;

    public LogAttribute()
    {
    }

    public LogAttribute(string title, BusinessType businessType = BusinessType.Other)
    {
        Title = title;
        BusinessType = businessType;
    }
}
