using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.System;

/// <summary>
/// 字典类型
/// </summary>
public class SysDictType : EntityBaseWithTenant
{
    /// <summary>
    /// 字典名称
    /// </summary>
    public string DictName { get; set; } = string.Empty;

    /// <summary>
    /// 字典类型
    /// </summary>
    public string DictType { get; set; } = string.Empty;

    /// <summary>
    /// 国际化键
    /// </summary>
    public string? LangKey { get; set; }

    /// <summary>
    /// 状态（0正常 1停用）
    /// </summary>
    public int Status { get; set; } = 0;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 导航属性：字典数据列表
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SysDictData.DictType), nameof(DictType))]
    public List<SysDictData>? Children { get; set; }
}
