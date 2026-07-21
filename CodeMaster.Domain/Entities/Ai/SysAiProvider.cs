using CodeMaster.Core.Entities;
using SqlSugar;

namespace CodeMaster.Domain.Entities.Ai;

/// <summary>
/// User-owned AI model provider configuration.
/// </summary>
[SugarTable("sys_ai_provider")]
public class SysAiProvider : EntityBaseWithTenant
{
    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "provider_type", Length = 40, IsNullable = false)]
    public string ProviderType { get; set; } = "OpenAICompatible";

    [SugarColumn(ColumnName = "execution_mode", Length = 20, IsNullable = false)]
    public string ExecutionMode { get; set; } = "Server";

    [SugarColumn(ColumnName = "base_url", Length = 500, IsNullable = true)]
    public string? BaseUrl { get; set; }

    [SugarColumn(ColumnName = "api_key_cipher_text", Length = 2000, IsNullable = true)]
    public string? ApiKeyCipherText { get; set; }

    [SugarColumn(ColumnName = "model_name", Length = 200, IsNullable = false)]
    public string ModelName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "extra_headers_json", Length = 4000, IsNullable = true)]
    public string? ExtraHeadersJson { get; set; }

    [SugarColumn(ColumnName = "is_default", IsNullable = false)]
    public bool IsDefault { get; set; }

    [SugarColumn(ColumnName = "is_enabled", IsNullable = false)]
    public bool IsEnabled { get; set; } = true;

    [SugarColumn(ColumnName = "supports_tools", IsNullable = false)]
    public bool SupportsTools { get; set; }

    [SugarColumn(ColumnName = "supports_streaming", IsNullable = false)]
    public bool SupportsStreaming { get; set; }

    [SugarColumn(ColumnName = "last_test_at", IsNullable = true)]
    public DateTime? LastTestAt { get; set; }

    [SugarColumn(ColumnName = "last_test_status", Length = 20, IsNullable = true)]
    public string? LastTestStatus { get; set; }

    [SugarColumn(ColumnName = "last_test_message", Length = 1000, IsNullable = true)]
    public string? LastTestMessage { get; set; }
}
