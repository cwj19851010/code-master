using SqlSugar;

namespace CodeMaster.Core.Entities;

/// <summary>
/// 实体基类（包含租户字段）
/// </summary>
public abstract class EntityBaseWithTenant : EntityBase, ITenant
{
    /// <summary>
    /// 租户ID（0 表示宿主）
    /// </summary>
    [SugarColumn(ColumnName = "tenant_id", IsNullable = false)]
    public long TenantId { get; set; } = 0;
}
