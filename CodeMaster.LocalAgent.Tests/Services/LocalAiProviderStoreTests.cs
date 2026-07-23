using CodeMaster.Agent.Contracts;
using CodeMaster.LocalAgent.Models;
using CodeMaster.LocalAgent.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace CodeMaster.LocalAgent.Tests.Services;

public class LocalAiProviderStoreTests
{
    [Fact]
    public async Task SaveAsync_encrypts_api_key_and_scopes_provider_to_server()
    {
        var root = Path.Combine(Path.GetTempPath(), "CodeMaster.LocalAgent.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var metadataStore = new LocalMetadataStore(Options.Create(new LocalAgentOptions
            {
                MetadataRoot = root
            }));
            var store = new LocalAiProviderStore(
                metadataStore,
                DataProtectionProvider.Create(Path.Combine(root, "keys")));

            await store.SaveAsync("https://codemaster.example.com/", new SaveLocalAiProviderRequest
            {
                ProviderId = 2077646988983275500,
                ProviderType = AiProviderTypes.OpenAiCompatible,
                BaseUrl = "http://127.0.0.1:11434/v1",
                ModelName = "qwen3-coder",
                ApiKey = "local-secret",
                ExtraHeadersJson = "{\"X-Test\":\"1\"}"
            });

            var settings = await store.GetAsync("https://codemaster.example.com", 2077646988983275500);
            Assert.Equal(AiProviderTypes.OpenAiCompatible, settings.ProviderType);
            Assert.Equal("http://127.0.0.1:11434/v1", settings.BaseUrl);
            Assert.Equal("qwen3-coder", settings.ModelName);
            Assert.Equal("local-secret", settings.ApiKey);
            Assert.Equal("{\"X-Test\":\"1\"}", settings.ExtraHeadersJson);

            var persisted = await File.ReadAllTextAsync(Path.Combine(root, "ai-providers.json"));
            Assert.DoesNotContain("local-secret", persisted, StringComparison.Ordinal);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                store.GetAsync("https://another.example.com", 2077646988983275500));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
