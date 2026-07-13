using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Core.Services;
using CodeMaster.Domain.Entities.CodeGen;

namespace CodeMaster.Application.Services.CodeGen;

/// <summary>
/// 模块实体服务接口
/// </summary>
public interface IModuleEntityService : ICrudApplicationService<ModuleEntity, ModuleEntityDto, ModuleEntityDto, ModuleEntityQueryDto, CreateModuleEntityDto, UpdateModuleEntityDto>, IApplicationService
{
    /// <summary>
    /// 获取全量实体列表
    /// </summary>
    Task<List<ModuleEntityDto>> GetListAsync(ModuleEntityQueryDto input);

    Task<List<ReferenceEntityDto>> GetReferenceEntitiesAsync(long? projectId = null);

    /// <summary>
    /// 根据模块ID获取实体列表
    /// </summary>
    Task<List<ModuleEntityDto>> GetByModuleIdAsync(long moduleId);

    /// <summary>
    /// 生成代码
    /// </summary>
    Task<bool> GenerateCodeAsync(long id);

    /// <summary>
    /// 增量生成代码（按字段/子表变更合并 tree.json 与页面）
    /// </summary>
    Task<bool> GenerateIncrementalCodeAsync(long id);

    /// <summary>
    /// 同步菜单到目标项目数据库
    /// </summary>
    Task<bool> SyncMenuToTargetAsync(long id);

    /// <summary>
    /// 同步字段多语言到目标项目数据库
    /// </summary>
    Task<bool> SyncLanguageToTargetAsync(long id);

    /// <summary>
    /// 获取页面模板内容（用于可视化设计器加载）
    /// </summary>
    Task<PageContentDto> GetPageContentAsync(long id, string pageType);

    /// <summary>
    /// 保存页面模板内容（可视化设计器回写）
    /// </summary>
    Task<bool> SavePageContentAsync(long id, string pageType, SavePageContentDto input);

    /// <summary>
    /// 获取页面 ScriptSection JSON（供设计器 Script 标签页）
    /// </summary>
    Task<string?> GetPageScriptAsync(long id, string pageType);

    /// <summary>
    /// 保存页面 ScriptSection JSON（设计器 Script 标签页回写）
    /// </summary>
    Task<bool> SavePageScriptAsync(long id, string pageType, string scriptJson);

    /// <summary>
    /// 获取所有字段级 ScriptSection（key=gen_id → ScriptSection JSON）
    /// </summary>
    Task<Dictionary<string, string>?> GetFieldScriptsAsync(long id, string pageType);

    /// <summary>
    /// 保存所有字段级 ScriptSection
    /// </summary>
    Task<bool> SaveFieldScriptsAsync(long id, string pageType, Dictionary<string, string> scripts);

    /// <summary>
    /// 获取组件属性面板数据（设计器右侧面板用）
    /// 根据组件 tag 查询 SysComponent 元数据，合并解析结果
    /// </summary>
    Task<FieldPropertyPanelDto> GetFieldPropertyPanelAsync(string tag);

    /// <summary>
    /// 获取所有页面模板
    /// </summary>
    Task<List<SysPageTemplate>> GetPageTemplatesAsync();

    /// <summary>
    /// 保存页面模板
    /// </summary>
    Task<bool> SavePageTemplateAsync(SysPageTemplate template);

    Task<bool> SaveFieldControlTemplateAsync(SysFieldControlTemplate template);

    /// <summary>
    /// 获取所有控件模板
    /// </summary>
    Task<List<SysFieldControlTemplate>> GetFieldControlTemplatesAsync();

    /// <summary>
    /// 获取所有子表模板
    /// </summary>
    Task<List<SysChildTemplate>> GetChildTemplatesAsync();

    Task<bool> SaveChildTemplateAsync(SysChildTemplate template);
}
