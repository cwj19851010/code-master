namespace CodeMaster.Infrastructure.VueParser.Model;

public class ComponentSlot
{
    public string Name { get; set; } = string.Empty;
    public string? Parameter { get; set; }
    public List<Component>? Components { get; set; }
}
