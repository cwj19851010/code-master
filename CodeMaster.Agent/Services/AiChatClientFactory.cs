using Anthropic;
using CodeMaster.Agent.Contracts;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace CodeMaster.Agent.Services;

public static class AiChatClientFactory
{
    public static IChatClient Create(AiProviderConnectionSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ModelName))
            throw new ArgumentException("Model name is required.");

        return settings.ProviderType switch
        {
            AiProviderTypes.OpenAiCompatible => CreateOpenAiCompatibleClient(settings),
            AiProviderTypes.Anthropic => CreateAnthropicClient(settings),
            _ => throw new InvalidOperationException($"Unsupported AI provider type: {settings.ProviderType}")
        };
    }

    private static IChatClient CreateOpenAiCompatibleClient(AiProviderConnectionSettings settings)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
            options.Endpoint = new Uri(settings.BaseUrl.Trim().TrimEnd('/') + "/");

        var httpClient = new HttpClient(new OpenAiCompatibleHttpHandler(settings.ExtraHeadersJson));
        options.Transport = new HttpClientPipelineTransport(httpClient);

        var apiKey = string.IsNullOrWhiteSpace(settings.ApiKey) ? "local-model" : settings.ApiKey;
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(settings.ModelName).AsIChatClient();
    }

    private static IChatClient CreateAnthropicClient(AiProviderConnectionSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("Anthropic requires an API key.");

        var client = new AnthropicClient
        {
            ApiKey = settings.ApiKey,
            BaseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
                ? "https://api.anthropic.com"
                : settings.BaseUrl.Trim().TrimEnd('/')
        };

        return client.AsIChatClient(settings.ModelName);
    }
}
