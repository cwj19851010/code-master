using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 实体字段DTO
/// </summary>
public class EntityFieldDto : EntityDto
{
    /// <summary>
    /// 所属实体ID
    /// </summary>
    public long ModuleEntityId { get; set; }

    /// <summary>
    /// 字段名称（英文 PascalCase）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段描述（中文）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否系统字段
    /// </summary>
    public bool IsSystemField { get; set; }

    #region 数据类型

    /// <summary>
    /// C# 数据类型
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 最大长度
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 精度
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    #endregion

    #region 数据库配置

    /// <summary>
    /// 是否忽略
    /// </summary>
    public bool IsIgnore { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    #endregion

    #region 验证规则

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// 是否邮箱格式
    /// </summary>
    public bool IsEmail { get; set; }

    /// <summary>
    /// 是否手机号格式
    /// </summary>
    public bool IsPhone { get; set; }

    #endregion

    #region 前端显示配置

    /// <summary>
    /// 是否在列表中显示
    /// </summary>
    public bool ShowInList { get; set; }

    /// <summary>
    /// 是否在详情中显示
    /// </summary>
    public bool ShowInDetail { get; set; }

    /// <summary>
    /// 是否在添加表单中显示
    /// </summary>
    public bool ShowInAddForm { get; set; }

    /// <summary>
    /// 是否在编辑表单中显示
    /// </summary>
    public bool ShowInEditForm { get; set; }

    /// <summary>
    /// 是否作为搜索条件
    /// </summary>
    public bool ShowInSearch { get; set; }

    /// <summary>
    /// 表单控件类型
    /// </summary>
    public string FormControlType { get; set; } = "input";

    /// <summary>
    /// 列表列宽
    /// </summary>
    public int? ListWidth { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    #endregion

    #region Select 数据源配置

    /// <summary>
    /// 数据源类型
    /// </summary>
    public string? SelectDataSource { get; set; }

    /// <summary>
    /// 选项数据
    /// </summary>
    public string? SelectOptions { get; set; }

    /// <summary>
    /// 是否多选
    /// </summary>
    public bool IsMultiple { get; set; }

    #endregion

    #region 关联表配置

    /// <summary>
    /// 关联表实体名称
    /// </summary>
    public string? RelatedEntityName { get; set; }

    /// <summary>
    /// 关联表 Id 字段
    /// </summary>
    public string? RelatedEntityIdField { get; set; }

    /// <summary>
    /// 关联表显示字段列表（JSON 数组）
    /// </summary>
    public string? RelatedEntityDisplayFields { get; set; }

    public string? ResultMappings { get; set; }

    #endregion

    #region 计算/统计字段配置

    public string FieldCategory { get; set; } = "Normal";
    public string? Formula { get; set; }
    public string? AggregateType { get; set; }
    public long? AggregateChildEntityId { get; set; }
    public string? AggregateChildFieldName { get; set; }
    public string? AggregateSeparator { get; set; }

    #endregion

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建实体字段DTO
/// </summary>
public class CreateEntityFieldDto
{
    /// <summary>
    /// 所属实体ID
    /// </summary>
    public long ModuleEntityId { get; set; }

    /// <summary>
    /// 字段名称（英文 PascalCase）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段描述（中文）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否系统字段
    /// </summary>
    public bool IsSystemField { get; set; }

    /// <summary>
    /// C# 数据类型
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 最大长度
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 精度
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 是否忽略
    /// </summary>
    public bool IsIgnore { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// 是否邮箱格式
    /// </summary>
    public bool IsEmail { get; set; }

    /// <summary>
    /// 是否手机号格式
    /// </summary>
    public bool IsPhone { get; set; }

    /// <summary>
    /// 是否在列表中显示
    /// </summary>
    public bool ShowInList { get; set; } = true;

    /// <summary>
    /// 是否在详情中显示
    /// </summary>
    public bool ShowInDetail { get; set; } = true;

    /// <summary>
    /// 是否在添加表单中显示
    /// </summary>
    public bool ShowInAddForm { get; set; } = true;

    /// <summary>
    /// 是否在编辑表单中显示
    /// </summary>
    public bool ShowInEditForm { get; set; } = true;

    /// <summary>
    /// 是否作为搜索条件
    /// </summary>
    public bool ShowInSearch { get; set; }

    /// <summary>
    /// 表单控件类型
    /// </summary>
    public string FormControlType { get; set; } = "input";

    /// <summary>
    /// 列表列宽
    /// </summary>
    public int? ListWidth { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 数据源类型
    /// </summary>
    public string? SelectDataSource { get; set; }

    /// <summary>
    /// 选项数据
    /// </summary>
    public string? SelectOptions { get; set; }

    /// <summary>
    /// 是否多选
    /// </summary>
    public bool IsMultiple { get; set; }

    /// <summary>
    /// 关联表实体名称
    /// </summary>
    public string? RelatedEntityName { get; set; }

    /// <summary>
    /// 关联表 Id 字段
    /// </summary>
    public string? RelatedEntityIdField { get; set; }

    /// <summary>
    /// 关联表显示字段列表（JSON 数组）
    /// </summary>
    public string? RelatedEntityDisplayFields { get; set; }

    public string? ResultMappings { get; set; }

    public string FieldCategory { get; set; } = "Normal";
    public string? Formula { get; set; }
    public string? AggregateType { get; set; }
    public long? AggregateChildEntityId { get; set; }
    public string? AggregateChildFieldName { get; set; }
    public string? AggregateSeparator { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新实体字段DTO
/// </summary>
public class UpdateEntityFieldDto
{
    /// <summary>
    /// 字段名称（英文 PascalCase）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段描述（中文）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// C# 数据类型
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 最大长度
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 精度
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 是否忽略
    /// </summary>
    public bool IsIgnore { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// 是否邮箱格式
    /// </summary>
    public bool IsEmail { get; set; }

    /// <summary>
    /// 是否手机号格式
    /// </summary>
    public bool IsPhone { get; set; }

    /// <summary>
    /// 是否在列表中显示
    /// </summary>
    public bool ShowInList { get; set; }

    /// <summary>
    /// 是否在详情中显示
    /// </summary>
    public bool ShowInDetail { get; set; }

    /// <summary>
    /// 是否在添加表单中显示
    /// </summary>
    public bool ShowInAddForm { get; set; }

    /// <summary>
    /// 是否在编辑表单中显示
    /// </summary>
    public bool ShowInEditForm { get; set; }

    /// <summary>
    /// 是否作为搜索条件
    /// </summary>
    public bool ShowInSearch { get; set; }

    /// <summary>
    /// 表单控件类型
    /// </summary>
    public string FormControlType { get; set; } = "input";

    /// <summary>
    /// 列表列宽
    /// </summary>
    public int? ListWidth { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 数据源类型
    /// </summary>
    public string? SelectDataSource { get; set; }

    /// <summary>
    /// 选项数据
    /// </summary>
    public string? SelectOptions { get; set; }

    /// <summary>
    /// 是否多选
    /// </summary>
    public bool IsMultiple { get; set; }

    /// <summary>
    /// 关联表实体名称
    /// </summary>
    public string? RelatedEntityName { get; set; }

    /// <summary>
    /// 关联表 Id 字段
    /// </summary>
    public string? RelatedEntityIdField { get; set; }

    /// <summary>
    /// 关联表显示字段列表（JSON 数组）
    /// </summary>
    public string? RelatedEntityDisplayFields { get; set; }

    public string? ResultMappings { get; set; }

    public string FieldCategory { get; set; } = "Normal";
    public string? Formula { get; set; }
    public string? AggregateType { get; set; }
    public long? AggregateChildEntityId { get; set; }
    public string? AggregateChildFieldName { get; set; }
    public string? AggregateSeparator { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 带ID的更新实体字段DTO（用于批量更新）
/// </summary>
public class UpdateEntityFieldWithIdDto : UpdateEntityFieldDto
{
    /// <summary>
    /// 字段ID
    /// </summary>
    public long Id { get; set; }
}

/// <summary>
/// 实体字段查询DTO
/// </summary>
public class EntityFieldQueryDto : PagedQueryDto
{
    /// <summary>
    /// 所属实体ID
    /// </summary>
    public long? ModuleEntityId { get; set; }

    /// <summary>
    /// 字段名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 字段描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// 是否系统字段
    /// </summary>
    public bool? IsSystemField { get; set; }
}
