using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 一对多关系DTO
/// </summary>
public class OneToManyRelationDto : EntityDto
{
    /// <summary>
    /// 主表实体 ID
    /// </summary>
    public long ModuleEntityId { get; set; }

    /// <summary>
    /// 主表关联字段名
    /// </summary>
    public string MasterField { get; set; } = string.Empty;

    /// <summary>
    /// 子表实体 ID
    /// </summary>
    public long ChildEntityId { get; set; }

    /// <summary>
    /// 子表实体名称
    /// </summary>
    public string ChildEntityName { get; set; } = string.Empty;

    /// <summary>
    /// 子表外键字段名
    /// </summary>
    public string ChildForeignKey { get; set; } = string.Empty;

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }
}

/// <summary>
/// 创建一对多关系DTO
/// </summary>
public class CreateOneToManyRelationDto
{
    /// <summary>
    /// 主表关联字段名
    /// </summary>
    public string MasterField { get; set; } = string.Empty;

    /// <summary>
    /// 子表实体 ID
    /// </summary>
    public long ChildEntityId { get; set; }

    /// <summary>
    /// 子表实体名称
    /// </summary>
    public string ChildEntityName { get; set; } = string.Empty;

    /// <summary>
    /// 子表外键字段名
    /// </summary>
    public string ChildForeignKey { get; set; } = string.Empty;

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }
}

/// <summary>
/// 更新一对多关系DTO
/// </summary>
public class UpdateOneToManyRelationDto
{
    /// <summary>
    /// 主表关联字段名
    /// </summary>
    public string MasterField { get; set; } = string.Empty;

    /// <summary>
    /// 子表实体 ID
    /// </summary>
    public long ChildEntityId { get; set; }

    /// <summary>
    /// 子表实体名称
    /// </summary>
    public string ChildEntityName { get; set; } = string.Empty;

    /// <summary>
    /// 子表外键字段名
    /// </summary>
    public string ChildForeignKey { get; set; } = string.Empty;

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }
}

/// <summary>
/// 带ID的更新一对多关系DTO
/// </summary>
public class UpdateOneToManyRelationWithIdDto : UpdateOneToManyRelationDto
{
    /// <summary>
    /// 关系ID
    /// </summary>
    public long Id { get; set; }
}
