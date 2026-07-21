namespace CodeMaster.Agent.Contracts;

public static class AiProviderTypes
{
    public const string OpenAiCompatible = "OpenAICompatible";
    public const string Anthropic = "Anthropic";
}

public static class AiExecutionModes
{
    public const string Server = "Server";
    public const string Local = "Local";
}

public sealed class AiProviderDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = AiProviderTypes.OpenAiCompatible;
    public string ExecutionMode { get; set; } = AiExecutionModes.Server;
    public string? BaseUrl { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string? ExtraHeadersJson { get; set; }
    public bool HasApiKey { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; }
    public bool SupportsTools { get; set; }
    public bool SupportsStreaming { get; set; }
    public DateTime? LastTestAt { get; set; }
    public string? LastTestStatus { get; set; }
    public string? LastTestMessage { get; set; }
}

public sealed class SaveAiProviderRequest
{
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = AiProviderTypes.OpenAiCompatible;
    public string ExecutionMode { get; set; } = AiExecutionModes.Server;
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public bool ClearApiKey { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string? ExtraHeadersJson { get; set; }
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class AiProviderTestResult
{
    public bool Success { get; set; }
    public bool SupportsTools { get; set; }
    public bool SupportsStreaming { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ResponseText { get; set; }
}
