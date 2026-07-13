using System.Text.Json.Serialization;

namespace CodeMaster.Application.Services.CodeGen.Marker;

/// <summary>
/// ScriptSection — 一个控件/模板需要的 JS 代码片段
/// 序列化为 JSON 存储在 DB 的 script_sections 字段
/// </summary>
public class ScriptSection
{
    [JsonPropertyName("imports")]
    public List<ImportItem> Imports { get; set; } = new();

    [JsonPropertyName("uses")]
    public List<UseItem> Uses { get; set; } = new();

    [JsonPropertyName("refs")]
    public List<RefItem> Refs { get; set; } = new();

    [JsonPropertyName("reactives")]
    public List<ReactiveBlock> Reactives { get; set; } = new();

    [JsonPropertyName("functions")]
    public List<FunctionBlock> Functions { get; set; } = new();

    [JsonPropertyName("hooks")]
    public List<HookBlock> Hooks { get; set; } = new();

    [JsonPropertyName("computed")]
    public List<ComputedBlock> Computed { get; set; } = new();

    [JsonPropertyName("watches")]
    public List<WatchBlock> Watches { get; set; } = new();

    /// <summary>合并另一个 ScriptSection 到当前</summary>
    public void Merge(ScriptSection other)
    {
        MergeList(Imports, other.Imports, x => x.Path);
        MergeList(Uses, other.Uses, x => x.Path);
        MergeList(Refs, other.Refs, x => x.Name);
        Reactives.AddRange(other.Reactives);
        Functions.AddRange(other.Functions);
        Hooks.AddRange(other.Hooks);
        Computed.AddRange(other.Computed);
        Watches.AddRange(other.Watches);
    }

    private static void MergeList<T>(List<T> target, List<T> source, Func<T, string> keySelector) where T : class
    {
        var existing = new HashSet<string>(target.Select(keySelector).Where(k => k != null)!);
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (key != null && !existing.Contains(key))
            {
                target.Add(item);
                existing.Add(key);
            }
        }
    }
}

public class ImportItem
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("destructured")]
    public string? Destructured { get; set; }

    [JsonPropertyName("default")]
    public string? Default { get; set; }
}

public class UseItem
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("as")]
    public string? As { get; set; }
}

public class RefItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("initialValue")]
    public string? InitialValue { get; set; }
}

public class ReactiveBlock
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public Dictionary<string, string> Fields { get; set; } = new();
}

public class FunctionBlock
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public string? Parameters { get; set; }

    [JsonPropertyName("body")]
    public List<string> Body { get; set; } = new();

    [JsonPropertyName("async")]
    public bool IsAsync { get; set; }
}

public class HookBlock
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty; // onMounted / onUnmounted / beforeMount

    [JsonPropertyName("body")]
    public List<string> Body { get; set; } = new();
}

public class ComputedBlock
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public List<string> Body { get; set; } = new();
}

public class WatchBlock
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public List<string> Body { get; set; } = new();

    [JsonPropertyName("deep")]
    public bool Deep { get; set; }

    [JsonPropertyName("immediate")]
    public bool Immediate { get; set; }
}
