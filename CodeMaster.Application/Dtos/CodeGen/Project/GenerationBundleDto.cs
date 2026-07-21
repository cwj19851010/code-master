using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Application.Dtos.CodeGen;

/// <summary>
/// 客户端本地执行代码生成所需的服务端上下文快照。
/// </summary>
public class GenerationBundleDto
{
    public int Version { get; set; } = 2;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = new();

    public List<ProjectModule> Modules { get; set; } = new();

    public List<ModuleEntity> Entities { get; set; } = new();

    public List<EntityField> Fields { get; set; } = new();

    public List<OneToManyRelation> Relations { get; set; } = new();

    public List<EntityRelation> EntityRelations { get; set; } = new();

    public List<SysPageTemplate> PageTemplates { get; set; } = new();

    public List<SysFieldControlTemplate> FieldControlTemplates { get; set; } = new();

    public List<SysChildTemplate> ChildTemplates { get; set; } = new();
}
