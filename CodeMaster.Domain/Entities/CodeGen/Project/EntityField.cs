using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.CodeGen;

/// <summary>
/// 实体字段
/// </summary>
[SugarTable("sys_entity_field")]
public class EntityField : EntityBaseWithTenant
{
    /// <summary>
    /// 所属实体ID
    /// </summary>
    [SugarColumn(ColumnName = "module_entity_id", IsNullable = false)]
    public long ModuleEntityId { get; set; }

    /// <summary>
    /// 字段名称（英文 PascalCase，如 UserName）
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段描述（中文，如"用户名"）
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 200, IsNullable = false)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否系统字段（自动生成的字段，不可删除）
    /// </summary>
    [SugarColumn(ColumnName = "is_system_field", IsNullable = false)]
    public bool IsSystemField { get; set; }

    #region 数据类型

    /// <summary>
    /// C# 数据类型（string, int, long, bool, DateTime, decimal 等）
    /// </summary>
    [SugarColumn(ColumnName = "data_type", Length = 50, IsNullable = false)]
    public string DataType { get; set; } = "string";

    /// <summary>
    /// 是否可空
    /// </summary>
    [SugarColumn(ColumnName = "is_nullable", IsNullable = false)]
    public bool IsNullable { get; set; }

    /// <summary>
    /// 最大长度（string 类型时有效）
    /// </summary>
    [SugarColumn(ColumnName = "max_length", IsNullable = true)]
    public int? MaxLength { get; set; }

    /// <summary>
    /// 精度（decimal 类型时有效，如 18）
    /// </summary>
    [SugarColumn(ColumnName = "precision", IsNullable = true)]
    public int? Precision { get; set; }

    /// <summary>
    /// 小数位数（decimal 类型时有效，如 2）
    /// </summary>
    [SugarColumn(ColumnName = "scale", IsNullable = true)]
    public int? Scale { get; set; }

    /// <summary>
    /// 默认值（字符串形式，如 "0", "''", "DateTime.UtcNow"）
    /// </summary>
    [SugarColumn(ColumnName = "default_value", Length = 200, IsNullable = true)]
    public string? DefaultValue { get; set; }

    #endregion

    #region 数据库配置

    /// <summary>
    /// 是否忽略（不映射到数据库）
    /// </summary>
    [SugarColumn(ColumnName = "is_ignore", IsNullable = false)]
    public bool IsIgnore { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    [SugarColumn(ColumnName = "is_primary_key", IsNullable = false)]
    public bool IsPrimaryKey { get; set; }

    #endregion

    #region 验证规则

    /// <summary>
    /// 是否必填
    /// </summary>
    [SugarColumn(ColumnName = "is_required", IsNullable = false)]
    public bool IsRequired { get; set; }

    /// <summary>
    /// 最小值（数字类型时有效）
    /// </summary>
    [SugarColumn(ColumnName = "min_value", Length = 50, IsNullable = true)]
    public string? MinValue { get; set; }

    /// <summary>
    /// 最大值（数字类型时有效）
    /// </summary>
    [SugarColumn(ColumnName = "max_value", Length = 50, IsNullable = true)]
    public string? MaxValue { get; set; }

    /// <summary>
    /// 正则表达式验证
    /// </summary>
    [SugarColumn(ColumnName = "regex_pattern", Length = 200, IsNullable = true)]
    public string? RegexPattern { get; set; }

    /// <summary>
    /// 是否邮箱格式
    /// </summary>
    [SugarColumn(ColumnName = "is_email", IsNullable = false)]
    public bool IsEmail { get; set; }

    /// <summary>
    /// 是否手机号格式
    /// </summary>
    [SugarColumn(ColumnName = "is_phone", IsNullable = false)]
    public bool IsPhone { get; set; }

    #endregion

    #region 前端显示配置

    /// <summary>
    /// 是否在列表中显示
    /// </summary>
    [SugarColumn(ColumnName = "show_in_list", IsNullable = false)]
    public bool ShowInList { get; set; } = true;

    /// <summary>
    /// 是否在详情中显示
    /// </summary>
    [SugarColumn(ColumnName = "show_in_detail", IsNullable = false)]
    public bool ShowInDetail { get; set; } = true;

    /// <summary>
    /// 是否在添加表单中显示
    /// </summary>
    [SugarColumn(ColumnName = "show_in_add_form", IsNullable = false)]
    public bool ShowInAddForm { get; set; } = true;

    /// <summary>
    /// 是否在编辑表单中显示
    /// </summary>
    [SugarColumn(ColumnName = "show_in_edit_form", IsNullable = false)]
    public bool ShowInEditForm { get; set; } = true;

    /// <summary>
    /// 是否作为搜索条件
    /// </summary>
    [SugarColumn(ColumnName = "show_in_search", IsNullable = false)]
    public bool ShowInSearch { get; set; }

    /// <summary>
    /// 表单控件类型（input, textarea, number, select, switch, checkbox, checkbox-group, radio-group, date, datetime, upload, image, editor, select-table, cascader）
    /// </summary>
    [SugarColumn(ColumnName = "form_control_type", Length = 50, IsNullable = false)]
    public string FormControlType { get; set; } = "input";

    /// <summary>
    /// 列表列宽（像素）
    /// </summary>
    [SugarColumn(ColumnName = "list_width", IsNullable = true)]
    public int? ListWidth { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order_num", IsNullable = false)]
    public int OrderNum { get; set; }

    #endregion

    #region Select 数据源配置

    /// <summary>
    /// 数据源类型（enum=枚举, dict=字典, api=接口）
    /// </summary>
    [SugarColumn(ColumnName = "select_data_source", Length = 50, IsNullable = true)]
    public string? SelectDataSource { get; set; }

    /// <summary>
    /// 选项数据（JSON格式或字典编码）
    /// 枚举示例：[{"value":1,"label":"男"},{"value":2,"label":"女"}]
    /// 字典示例：user_status
    /// 接口示例：/api/users/list
    /// </summary>
    [SugarColumn(ColumnName = "select_options", Length = 2000, IsNullable = true)]
    public string? SelectOptions { get; set; }

    /// <summary>
    /// 是否多选（select / select-table / cascader 适用）
    /// </summary>
    [SugarColumn(ColumnName = "is_multiple", IsNullable = false)]
    public bool IsMultiple { get; set; }

    #endregion

    #region 关联表配置（select-table / cascader）

    /// <summary>
    /// 关联表实体名称（从 ModuleEntity 中选择）
    /// </summary>
    [SugarColumn(ColumnName = "related_entity_name", Length = 100, IsNullable = true)]
    public string? RelatedEntityName { get; set; }

    /// <summary>
    /// 关联表 Id 字段（目标表用于绑定值的字段）
    /// </summary>
    [SugarColumn(ColumnName = "related_entity_id_field", Length = 100, IsNullable = true)]
    public string? RelatedEntityIdField { get; set; }

    /// <summary>
    /// 关联表显示字段列表（JSON 数组，如 ["Name","Age","Gender"]）
    /// </summary>
    [SugarColumn(ColumnName = "related_entity_display_fields", Length = 2000, IsNullable = true)]
    public string? RelatedEntityDisplayFields { get; set; }

    #endregion

    #region 计算/统计字段配置

    /// <summary>
    /// 字段类别：Normal=普通, Computed=计算字段, Aggregate=统计字段
    /// </summary>
    [SugarColumn(ColumnName = "field_category", Length = 20, IsNullable = false)]
    public string FieldCategory { get; set; } = "Normal";

    /// <summary>
    /// 计算公式（计算字段），如 [Price]*[Quantity]
    /// </summary>
    [SugarColumn(ColumnName = "formula", Length = 500, IsNullable = true)]
    public string? Formula { get; set; }

    /// <summary>
    /// 统计类型（统计字段）：Sum=累加, Avg=平均值, Concat=字符串拼接
    /// </summary>
    [SugarColumn(ColumnName = "aggregate_type", Length = 20, IsNullable = true)]
    public string? AggregateType { get; set; }

    /// <summary>
    /// 统计的关联子表实体ID
    /// </summary>
    [SugarColumn(ColumnName = "aggregate_child_entity_id", IsNullable = true)]
    public long? AggregateChildEntityId { get; set; }

    /// <summary>
    /// 统计的子表字段名
    /// </summary>
    [SugarColumn(ColumnName = "aggregate_child_field_name", Length = 100, IsNullable = true)]
    public string? AggregateChildFieldName { get; set; }

    /// <summary>
    /// 字符串拼接分隔符（AggregateType=Concat 时使用）
    /// </summary>
    [SugarColumn(ColumnName = "aggregate_separator", Length = 20, IsNullable = true)]
    public string? AggregateSeparator { get; set; }

    /// <summary>
    /// 显示条件，如 [IsEnabled]=true，生成 v-if="form.isEnabled"
    /// </summary>
    [SugarColumn(ColumnName = "show_condition", Length = 200, IsNullable = true)]
    public string? ShowCondition { get; set; }

    /// <summary>
    /// 列表是否支持排序
    /// </summary>
    [SugarColumn(ColumnName = "is_sortable", IsNullable = false)]
    public bool IsSortable { get; set; }

    #endregion
}
