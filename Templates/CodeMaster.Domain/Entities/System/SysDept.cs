using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.System;


/// <summary>
/// 部门实体
/// </summary>
public class SysDept : TreeEntityBase, ITenantEntity
{
    /// <summary>
    /// 部门名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnName = "tenant_id")]
    public long TenantId { get; set; }
}
