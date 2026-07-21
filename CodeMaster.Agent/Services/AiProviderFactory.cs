using Anthropic;
using CodeMaster.Agent.Contracts;
using CodeMaster.Domain.Entities.Ai;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;

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

        return provider.ProviderType switch
        {
            AiProviderTypes.OpenAiCompatible => CreateOpenAiCompatibleClient(provider, apiKey),
            AiProviderTypes.Anthropic => CreateAnthropicClient(provider, apiKey),
            _ => throw new InvalidOperationException($"Unsupported AI provider type: {provider.ProviderType}")
        };
    }

    private static IChatClient CreateOpenAiCompatibleClient(SysAiProvider provider, string? apiKey)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(provider.BaseUrl))
        {
            options.Endpoint = new Uri(provider.BaseUrl.Trim().TrimEnd('/') + "/");
        }

        var httpClient = new HttpClient(new OpenAiCompatibleHttpHandler(provider.ExtraHeadersJson));
        options.Transport = new HttpClientPipelineTransport(httpClient);

        var client = new OpenAIClient(new ApiKeyCredential(string.IsNullOrWhiteSpace(apiKey) ? "local-model" : apiKey), options);
        return client.GetChatClient(provider.ModelName).AsIChatClient();
    }

    private static IChatClient CreateAnthropicClient(SysAiProvider provider, string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Anthropic requires an API key.");
        }

        var client = new AnthropicClient
        {
            ApiKey = apiKey,
            BaseUrl = string.IsNullOrWhiteSpace(provider.BaseUrl)
                ? "https://api.anthropic.com"
                : provider.BaseUrl.Trim().TrimEnd('/')
        };

        return client.AsIChatClient(provider.ModelName);
    }
}
