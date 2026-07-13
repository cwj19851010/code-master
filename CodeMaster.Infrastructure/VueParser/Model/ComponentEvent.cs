namespace CodeMaster.Infrastructure.VueParser.Model;

public class ComponentEvent
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Body { get; set; }
    public string? Expression { get; set; }
    public string? Parameter { get; set; }
    public bool IsCustom { get; set; }
    public bool IsSingle { get; set; }
    public bool IsAsync { get; set; }
}
