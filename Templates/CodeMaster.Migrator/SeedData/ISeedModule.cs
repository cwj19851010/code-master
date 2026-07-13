using CodeMaster.Migrator.Persistence.EfCore;

namespace CodeMaster.Migrator.SeedData;

/// <summary>
/// 种子数据模块接口
/// </summary>
public interface ISeedModule
{
    /// <summary>
    /// 模块名称（用于日志输出）
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// 1.1 判断该模块是否有菜单（通过检查特征菜单是否存在）
    /// </summary>
    Task<bool> HasMenuAsync(CodeMasterDbContext dbContext);

    /// <summary>
    /// 1.2 添加该模块菜单
    /// </summary>
    Task AddMenusAsync(CodeMasterDbContext dbContext, long adminRoleId);

    /// <summary>
    /// 1.3 添加该模块初始数据（字典、配置等业务数据）
    /// </summary>
    Task AddInitialDataAsync(CodeMasterDbContext dbContext);

    /// <summary>
    /// 1.4 获取该模块翻译键（返回字典：key -> {zh-CN, en-US}）
    /// </summary>
    Dictionary<string, Dictionary<string, string>> GetTranslations();
}
