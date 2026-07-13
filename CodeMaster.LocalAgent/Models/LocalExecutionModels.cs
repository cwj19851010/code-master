using System.Text.Json;

namespace CodeMaster.LocalAgent.Models;

public class LocalExecutionRequest
{
    public string Action { get; set; } = string.Empty;

    public JsonElement Payload { get; set; }

    public string ServerBaseUrl { get; set; } = string.Empty;

    public string? AccessToken { get; set; }
}

public class StdioLocalExecutionRequest : LocalExecutionRequest
{
    public string Id { get; set; } = string.Empty;
}

public class LocalExecutionResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public object? Data { get; set; }

    public string? Output { get; set; }

    public bool DataOnly { get; set; }

    public static LocalExecutionResult Ok(string message, object? data = null, string? output = null)
    {
        return new LocalExecutionResult
        {
            Success = true,
            Message = message,
            Data = data,
            Output = output
        };
    }

    public static LocalExecutionResult Fail(string message, string? output = null)
    {
        return new LocalExecutionResult
        {
            Success = false,
            Message = message,
            Output = output
        };
    }

    public static LocalExecutionResult OkData(object? data)
    {
        return new LocalExecutionResult
        {
            Success = true,
            Data = data,
            DataOnly = true
        };
    }
}

public class StdioLocalExecutionResponse : LocalExecutionResult
{
    public string Id { get; set; } = string.Empty;
}

public class LocalAgentOptions
{
    public string MetadataRoot { get; set; } = string.Empty;

    public int Port { get; set; } = 39721;

    public string ClientToken { get; set; } = string.Empty;

    public string Mode { get; set; } = string.Empty;
}
