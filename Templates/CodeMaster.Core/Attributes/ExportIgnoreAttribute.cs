namespace CodeMaster.Core.Attributes;

/// <summary>
/// 导出时忽略该属性
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExportIgnoreAttribute : Attribute
{
}
