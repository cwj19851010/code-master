using CodeMaster.Agent.Contracts;
using CodeMaster.Domain.Entities.Ai;
using Microsoft.Extensions.AI;

namespace CodeMaster.Agent.Services;

public interface IAiProviderFactory
{
    IChatClient CreateChatClient(SysAiProvider provider);
}

internal sealed class AiProviderFactory : IAiProviderFactory
{
    private readonly IAiSecretProtector _secretProtector;

    public AiProviderFactory(IAiSecretProtector secretProtector)
    {
        _secretProtector = secretProtector;
    }

    public IChatClient CreateChatClient(SysAiProvider provider)
    {
        if (!string.Equals(provider.ExecutionMode, AiExecutionModes.Server, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This provider is configured for LocalAgent execution and cannot run in WebApi.");
        }

        var apiKey = string.IsNullOrWhiteSpace(provider.ApiKeyCipherText)
            ? null
            : _secretProtector.Unprotect(provider.ApiKeyCipherText);

        return AiChatClientFactory.Create(new AiProviderConnectionSettings
        {
            ProviderType = provider.ProviderType,
            BaseUrl = provider.BaseUrl,
            ApiKey = apiKey,
            ModelName = provider.ModelName,
            ExtraHeadersJson = provider.ExtraHeadersJson
        });
    }
}
