namespace CodeMaster.Infrastructure.VueParser.Model;

public class ComponentInstruction
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool IsSingle { get; set; }
}
