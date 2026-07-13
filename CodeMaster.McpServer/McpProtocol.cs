using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CodeMaster.McpServer;

/// <summary>
/// MCP JSON-RPC 2.0 protocol handler over stdin/stdout.
/// </summary>
public class McpProtocol
{
    private readonly Dictionary<string, McpTool> _tools;
    private readonly string _serverName;
    private readonly string _serverVersion;
    private readonly TextWriter _output;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public McpProtocol(string serverName, string serverVersion, TextWriter? output = null)
    {
        _serverName = serverName;
        _serverVersion = serverVersion;
        _output = output ?? Console.Out;
        _tools = new Dictionary<string, McpTool>();
    }

    public void RegisterTool(McpTool tool)
    {
        _tools[tool.Name] = tool;
    }

    public async Task RunAsync(CancellationToken cancellation = default)
    {
        using var reader = new StreamReader(Console.OpenStandardInput());
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (!cancellation.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellation);
            if (line == null) break; // EOF
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, JsonOptions);
                if (request == null) continue;

                var response = await HandleRequestAsync(request);
                if (response != null)
                {
                    var json = JsonSerializer.Serialize(response, JsonOptions);
                    await _output.WriteLineAsync(json);
                    await _output.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new JsonRpcResponse
                {
                    JsonRpc = "2.0",
                    Id = null,
                    Error = new JsonRpcError { Code = -32700, Message = $"Parse error: {ex.Message}" }
                };
                await _output.WriteLineAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
                await _output.FlushAsync();
            }
        }
    }

    private async Task<JsonRpcResponse?> HandleRequestAsync(JsonRpcRequest request)
    {
        // Notifications (no id) don't get a response
        var isNotification = request.Id == null;

        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "notifications/initialized" => null, // No response for notifications
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolsCallAsync(request),
            "ping" => isNotification ? null : new JsonRpcResponse { JsonRpc = "2.0", Id = request.Id, Result = JsonSerializer.SerializeToNode(new { }) },
            _ => isNotification ? null : new JsonRpcResponse { JsonRpc = "2.0", Id = request.Id, Error = new JsonRpcError { Code = -32601, Message = $"Method not found: {request.Method}" } }
        };
    }

    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        return new JsonRpcResponse
        {
            JsonRpc = "2.0",
            Id = request.Id,
            Result = JsonSerializer.SerializeToNode(new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new { name = _serverName, version = _serverVersion }
            }, JsonOptions)
        };
    }

    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        var tools = _tools.Values.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            inputSchema = t.InputSchema
        }).ToList();

        return new JsonRpcResponse
        {
            JsonRpc = "2.0",
            Id = request.Id,
            Result = JsonSerializer.SerializeToNode(new { tools }, JsonOptions)
        };
    }

    private async Task<JsonRpcResponse> HandleToolsCallAsync(JsonRpcRequest request)
    {
        var id = request.Id;

        try
        {
            var toolName = request.Params?["name"]?.GetValue<string>();
            var arguments = request.Params?["arguments"];

            if (string.IsNullOrEmpty(toolName))
            {
                return ErrorResponse(id, -32602, "Missing tool name");
            }

            if (!_tools.TryGetValue(toolName, out var tool))
            {
                return ErrorResponse(id, -32601, $"Unknown tool: {toolName}");
            }

            // Parse arguments into the tool's input type
            object? parsedArgs = null;
            if (arguments != null && tool.InputType != null)
            {
                var argsJson = arguments.Deserialize(tool.InputType, JsonOptions);
                parsedArgs = argsJson;
            }

            var result = await tool.Handler(parsedArgs);

            return new JsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = id,
                Result = JsonSerializer.SerializeToNode(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = JsonSerializer.Serialize(result, JsonOptions)
                        }
                    }
                }, JsonOptions)
            };
        }
        catch (Exception ex)
        {
            return ErrorResponse(id, -32000, $"Tool execution error: {ex.Message}");
        }
    }

    private static JsonRpcResponse ErrorResponse(JsonNode? id, int code, string message)
    {
        return new JsonRpcResponse
        {
            JsonRpc = "2.0",
            Id = id,
            Error = new JsonRpcError { Code = code, Message = message }
        };
    }
}

public class McpTool
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public JsonNode InputSchema { get; init; } = JsonSerializer.SerializeToNode(new { type = "object", properties = new { } })!;
    public Type? InputType { get; init; }
    public Func<object?, Task<object?>> Handler { get; init; } = _ => Task.FromResult<object?>(null);
}

// ── JSON-RPC 2.0 message types ──

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonNode? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public JsonNode? Params { get; set; }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonNode? Id { get; set; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; set; }
}

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
