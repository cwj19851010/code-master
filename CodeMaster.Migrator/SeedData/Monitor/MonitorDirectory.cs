namespace CodeMaster.Migrator.SeedData.Monitor;

/// <summary>
/// 监控管理顶级目录
/// </summary>
public class MonitorDirectory : ITopLevelDirectory
{
    public string Name => "监控管理";
    public string TitleKey => "";
    public string Path => "monitor";
    public string Icon => "Monitor";
    public int Order => 2;
}
