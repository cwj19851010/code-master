using SqlSugar;

namespace CodeMaster.Core.Entities;

/// <summary>
/// 实体基类（雪花ID，不包含租户字段）
/// </summary>
public abstract class EntityBase : IEntity, IEntity<long>, IAuditEntity, ISoftDelete
{
    /// <summary>
    /// 主键ID（雪花ID）
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
    public long Id { get; set; }



    /// <summary>
    /// 创建人
    /// </summary>
    [SugarColumn(ColumnName = "create_by", Length = 64, IsOnlyIgnoreUpdate = true)]
    public string? CreateBy { get; set; }

    /// <summary>
    /// 创建人ID
    /// </summary>
    [SugarColumn(ColumnName = "create_user_id", IsOnlyIgnoreUpdate = true, IsNullable = true)]
    public long? CreateUserId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "create_time", IsOnlyIgnoreUpdate = true)]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新人
    /// </summary>
    [SugarColumn(ColumnName = "update_user_id", Length = 64, IsOnlyIgnoreUpdate = true)]
    public long? UpdateUserId { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    [SugarColumn(ColumnName = "update_by", Length = 64, IsOnlyIgnoreInsert = true, IsNullable = true)]
    public string? UpdateBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnName = "update_time", IsOnlyIgnoreInsert = true, IsNullable = true)]
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    [SugarColumn(ColumnName = "is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 删除人
    /// </summary>
    [SugarColumn(ColumnName = "delete_by", Length = 64, IsNullable = true)]
    public string? DeleteBy { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    [SugarColumn(ColumnName = "delete_time", IsNullable = true)]
    public DateTime? DeleteTime { get; set; }
    /// <summary>
    /// 删除人ID
    /// </summary>
    [SugarColumn(ColumnName = "delete_user_id", IsNullable = true)]
    public long? DeleteUserId { get; set; }
}
