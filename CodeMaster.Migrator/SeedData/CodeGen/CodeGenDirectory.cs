namespace CodeMaster.Migrator.SeedData.CodeGen;

/// <summary>
/// 代码管理顶级目录
/// </summary>
public class CodeGenDirectory : ITopLevelDirectory
{
    public string Name => "代码管理";
    public string TitleKey => "";
    public string Path => "codegen";
    public string Icon => "Files";
    public int Order => 3;
}
