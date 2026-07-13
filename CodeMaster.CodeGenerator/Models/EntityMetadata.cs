namespace CodeMaster.CodeGenerator.Models;

/// <summary>
/// 实体元数据
/// </summary>
public class EntityMetadata
{
    /// <summary>
    /// 实体名称（如：SysUser）
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// 实体命名空间
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// 业务名称（如：User）
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// 模块名称（如：System）
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// 实体描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 属性列表
    /// </summary>
    public List<PropertyMetadata> Properties { get; set; } = new();

    /// <summary>
    /// 是否有租户字段
    /// </summary>
    public bool HasTenantId { get; set; }
}

/// <summary>
/// 属性元数据
/// </summary>
public class PropertyMetadata
{
    /// <summary>
    /// 属性名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 属性类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 属性描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否为主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }
}
