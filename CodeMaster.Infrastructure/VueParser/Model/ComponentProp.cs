namespace CodeMaster.Infrastructure.VueParser.Model;

public class ComponentProp
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool IsBind { get; set; }
    public bool IsSingle { get; set; }
    public string Type { get; set; } = "string";
}
