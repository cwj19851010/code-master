using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeMaster.McpServer;

// Retained only for the existing per-tool schema metadata. Runtime protocol
// handling is provided by the official ModelContextProtocol SDK.
public sealed class McpTool
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public JsonNode InputSchema { get; init; } =
        JsonSerializer.SerializeToNode(new { type = "object", properties = new { } })!;
    public Type? InputType { get; init; }
    public Func<object?, Task<object?>> Handler { get; init; } =
        _ => Task.FromResult<object?>(null);
}
