using System.Text.Json;
using CodeMaster.Agent.Contracts;
using CodeMaster.LocalAgent.Models;
using Microsoft.AspNetCore.DataProtection;

namespace CodeMaster.LocalAgent.Services;

public sealed class LocalAiProviderStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly LocalMetadataStore _metadataStore;
    private readonly IDataProtector _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LocalAiProviderStore(LocalMetadataStore metadataStore, IDataProtectionProvider dataProtectionProvider)
    {
        _metadataStore = metadataStore;
        _protector = dataProtectionProvider.CreateProtector("CodeMaster.LocalAgent.AiProviders.v1");
    }

    public async Task SaveAsync(string serverBaseUrl, SaveLocalAiProviderRequest input)
    {
        Validate(serverBaseUrl, input);
        await _gate.WaitAsync();
        try
        {
            var records = await LoadAsync();
            var normalizedServer = NormalizeServer(serverBaseUrl);
            var record = records.FirstOrDefault(item =>
                item.ProviderId == input.ProviderId &&
                string.Equals(item.ServerBaseUrl, normalizedServer, StringComparison.OrdinalIgnoreCase));
            if (record == null)
            {
                record = new LocalAiProviderRecord
                {
                    ServerBaseUrl = normalizedServer,
                    ProviderId = input.ProviderId
                };
                records.Add(record);
            }

            record.ProviderType = input.ProviderType.Trim();
            record.BaseUrl = input.BaseUrl?.Trim();
            record.ModelName = input.ModelName.Trim();
            record.ExtraHeadersJson = input.ExtraHeadersJson?.Trim();
            record.UpdateTime = DateTime.UtcNow;
            if (input.ClearApiKey)
                record.ProtectedApiKey = null;
            else if (!string.IsNullOrWhiteSpace(input.ApiKey))
                record.ProtectedApiKey = _protector.Protect(input.ApiKey.Trim());

            await PersistAsync(records);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(string serverBaseUrl, long providerId)
    {
        await _gate.WaitAsync();
        try
        {
            var records = await LoadAsync();
            var normalizedServer = NormalizeServer(serverBaseUrl);
            records.RemoveAll(item => item.ProviderId == providerId &&
                string.Equals(item.ServerBaseUrl, normalizedServer, StringComparison.OrdinalIgnoreCase));
            await PersistAsync(records);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AiProviderConnectionSettings> GetAsync(string serverBaseUrl, long providerId)
    {
        await _gate.WaitAsync();
        try
        {
            var normalizedServer = NormalizeServer(serverBaseUrl);
            var record = (await LoadAsync()).FirstOrDefault(item =>
                item.ProviderId == providerId &&
                string.Equals(item.ServerBaseUrl, normalizedServer, StringComparison.OrdinalIgnoreCase));
            if (record == null)
                throw new KeyNotFoundException("The local AI provider configuration was not found. Open model settings and save it again.");

            return new AiProviderConnectionSettings
            {
                ProviderType = record.ProviderType,
                BaseUrl = record.BaseUrl,
                ApiKey = string.IsNullOrWhiteSpace(record.ProtectedApiKey)
                    ? null
                    : _protector.Unprotect(record.ProtectedApiKey),
                ModelName = record.ModelName,
                ExtraHeadersJson = record.ExtraHeadersJson
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<LocalAiProviderRecord>> LoadAsync()
    {
        var path = GetStorePath();
        if (!File.Exists(path)) return new List<LocalAiProviderRecord>();
        var json = await File.ReadAllTextAsync(path);
        if (string.IsNullOrWhiteSpace(json)) return new List<LocalAiProviderRecord>();
        return JsonSerializer.Deserialize<List<LocalAiProviderRecord>>(json, JsonOptions)
            ?? new List<LocalAiProviderRecord>();
    }

    private async Task PersistAsync(List<LocalAiProviderRecord> records)
    {
        var path = GetStorePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(records, JsonOptions));
        File.Move(tempPath, path, true);
    }

    private string GetStorePath() => Path.Combine(_metadataStore.GetMetadataRoot(), "ai-providers.json");

    private static string NormalizeServer(string serverBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(serverBaseUrl))
            throw new ArgumentException("Server base URL is required.");
        return serverBaseUrl.Trim().TrimEnd('/');
    }

    private static void Validate(string serverBaseUrl, SaveLocalAiProviderRequest input)
    {
        _ = NormalizeServer(serverBaseUrl);
        if (input.ProviderId <= 0) throw new ArgumentException("Provider id is required.");
        if (input.ProviderType is not (AiProviderTypes.OpenAiCompatible or AiProviderTypes.Anthropic))
            throw new ArgumentException("Unsupported AI provider type.");
        if (string.IsNullOrWhiteSpace(input.ModelName)) throw new ArgumentException("Model name is required.");
        if (!string.IsNullOrWhiteSpace(input.BaseUrl) &&
            (!Uri.TryCreate(input.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
        {
            throw new ArgumentException("AI provider base URL must be an absolute HTTP or HTTPS URL.");
        }
    }
}
