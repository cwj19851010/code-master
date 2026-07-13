namespace CodeMaster.Migrator.SeedData.Core;

/// <summary>
/// 系统管理顶级目录
/// </summary>
public class SystemDirectory : ITopLevelDirectory
{
    public string Name => "系统管理";
    public string TitleKey => "";
    public string Path => "system";
    public string Icon => "Setting";
    public int Order => 1;
}
