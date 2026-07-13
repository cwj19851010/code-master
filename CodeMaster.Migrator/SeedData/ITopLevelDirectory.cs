namespace CodeMaster.Migrator.SeedData;

/// <summary>
/// 顶级目录接口
/// </summary>
public interface ITopLevelDirectory
{
    /// <summary>
    /// 目录名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 翻译键
    /// </summary>
    string TitleKey { get; }

    /// <summary>
    /// 路径
    /// </summary>
    string Path { get; }

    /// <summary>
    /// 图标
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// 排序
    /// </summary>
    int Order { get; }
}
