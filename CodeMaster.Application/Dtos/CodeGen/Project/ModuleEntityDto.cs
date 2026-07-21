using CodeMaster.Core.Dtos;

namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 模块实体DTO
/// </summary>
public class ModuleEntityDto : EntityDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    public long ModuleId { get; set; }

    /// <summary>
    /// 实体名称（英文 PascalCase）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实体描述（中文）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否有主键
    /// </summary>
    public bool HasPrimaryKey { get; set; }

    /// <summary>
    /// 表名（snake_case）
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// 是否树形结构
    /// </summary>
    public bool IsTree { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 是否启用多租户
    /// </summary>
    public bool HasTenant { get; set; }

    /// <summary>
    /// 是否启用数据权限
    /// </summary>
    public bool HasDataPermission { get; set; }

    /// <summary>
    /// 是否启用审计字段
    /// </summary>
    public bool HasAudit { get; set; }

    /// <summary>
    /// 是否启用软删除字段
    /// </summary>
    public bool HasSoftDelete { get; set; }

    /// <summary>
    /// 是否生成前端页面
    /// </summary>
    public bool GenerateFrontend { get; set; }

    /// <summary>
    /// 前端路由路径
    /// </summary>
    public string? FrontendRoute { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    public string? MenuIcon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 是否被引用为子表
    /// </summary>
    public bool IsChildTable { get; set; }

    /// <summary>
    /// 是否已生成代码
    /// </summary>
    public bool IsGenerated { get; set; }

    /// <summary>
    /// 最后生成时间
    /// </summary>
    public DateTime? LastGeneratedTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 字段列表
    /// </summary>
    public List<EntityFieldDto> Fields { get; set; } = new();

    /// <summary>
    /// 一对多关系列表
    /// </summary>
    public List<OneToManyRelationDto> OneToManyRelations { get; set; } = new();

    public List<EntityRelationDto> EntityRelations { get; set; } = new();
}

public class ReferenceEntityDto
{
    public long? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsBuiltin { get; set; }

    public bool IsTree { get; set; }

    public string ValueField { get; set; } = "Id";

    public List<string> DisplayFields { get; set; } = new();

    public string? SelectOptions { get; set; }

    public List<EntityFieldDto> Fields { get; set; } = new();
}

/// <summary>
/// 创建模块实体DTO
/// </summary>
public class CreateModuleEntityDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long ProjectId { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    public long ModuleId { get; set; }

    /// <summary>
    /// 实体名称（英文 PascalCase）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实体描述（中文）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否有主键
    /// </summary>
    public bool HasPrimaryKey { get; set; } = true;

    /// <summary>
    /// 表名（snake_case，为空则自动生成）
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// 是否树形结构
    /// </summary>
    public bool IsTree { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 是否启用多租户
    /// </summary>
    public bool HasTenant { get; set; }

    /// <summary>
    /// 是否启用数据权限
    /// </summary>
    public bool HasDataPermission { get; set; }

    /// <summary>
    /// 是否启用审计（IAuditEntity：CreateUserId/CreateBy/CreateTime/UpdateUserId/UpdateBy/UpdateTime）
    /// </summary>
    public bool HasAudit { get; set; } = true;

    /// <summary>
    /// 是否启用软删除（ISoftDelete：IsDeleted/DeleteTime/DeleteBy/DeleteUserId）
    /// </summary>
    public bool HasSoftDelete { get; set; } = true;

    /// <summary>
    /// 是否生成前端页面
    /// </summary>
    public bool GenerateFrontend { get; set; } = true;

    /// <summary>
    /// 是否为子表
    /// </summary>
    public bool IsChildTable { get; set; }

    /// <summary>
    /// 前端路由路径
    /// </summary>
    public string? FrontendRoute { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    public string? MenuIcon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 字段列表
    /// </summary>
    public List<CreateEntityFieldDto> Fields { get; set; } = new();

    /// <summary>
    /// 一对多关系列表
    /// </summary>
    public List<CreateOneToManyRelationDto> OneToManyRelations { get; set; } = new();

    public List<CreateEntityRelationDto> EntityRelations { get; set; } = new();
}

/// <summary>
/// 更新模块实体DTO
/// </summary>
public class UpdateModuleEntityDto
{
    /// <summary>
    /// 实体名称（英文 PascalCase）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实体描述（中文）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否有主键
    /// </summary>
    public bool? HasPrimaryKey { get; set; }

    /// <summary>
    /// 表名（snake_case）
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// 是否树形结构
    /// </summary>
    public bool? IsTree { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool? IsReadOnly { get; set; }

    /// <summary>
    /// 是否启用多租户
    /// </summary>
    public bool? HasTenant { get; set; }

    /// <summary>
    /// 是否启用数据权限
    /// </summary>
    public bool? HasDataPermission { get; set; }

    /// <summary>
    /// 是否启用审计字段；未传时保留当前值
    /// </summary>
    public bool? HasAudit { get; set; }

    /// <summary>
    /// 是否启用软删除字段；未传时保留当前值
    /// </summary>
    public bool? HasSoftDelete { get; set; }

    /// <summary>
    /// 是否生成前端页面
    /// </summary>
    public bool? GenerateFrontend { get; set; }

    /// <summary>
    /// 是否为子表
    /// </summary>
    public bool? IsChildTable { get; set; }

    /// <summary>
    /// 前端路由路径
    /// </summary>
    public string? FrontendRoute { get; set; }

    /// <summary>
    /// 菜单图标
    /// </summary>
    public string? MenuIcon { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 新增的字段列表
    /// </summary>
    public List<CreateEntityFieldDto> NewFields { get; set; } = new();

    /// <summary>
    /// 更新的字段列表
    /// </summary>
    public List<UpdateEntityFieldWithIdDto> UpdatedFields { get; set; } = new();

    /// <summary>
    /// 删除的字段ID列表
    /// </summary>
    public List<long> DeletedFieldIds { get; set; } = new();

    /// <summary>
    /// 新增的一对多关系列表
    /// </summary>
    public List<CreateOneToManyRelationDto> NewRelations { get; set; } = new();

    /// <summary>
    /// 更新的一对多关系列表
    /// </summary>
    public List<UpdateOneToManyRelationWithIdDto> UpdatedRelations { get; set; } = new();

    /// <summary>
    /// 删除的一对多关系ID列表
    /// </summary>
    public List<long> DeletedRelationIds { get; set; } = new();

    public List<CreateEntityRelationDto> NewEntityRelations { get; set; } = new();

    public List<UpdateEntityRelationWithIdDto> UpdatedEntityRelations { get; set; } = new();

    public List<long> DeletedEntityRelationIds { get; set; } = new();
}

/// <summary>
/// 模块实体查询DTO
/// </summary>
public class ModuleEntityQueryDto : PagedQueryDto
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public long? ProjectId { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    public long? ModuleId { get; set; }

    /// <summary>
    /// 实体名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 实体描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否树形结构
    /// </summary>
    public bool? IsTree { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool? IsReadOnly { get; set; }

    /// <summary>
    /// 是否已生成代码
    /// </summary>
    public bool? IsGenerated { get; set; }
}
