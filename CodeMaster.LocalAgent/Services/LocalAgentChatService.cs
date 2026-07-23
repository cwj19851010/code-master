using System.Text.Json;
using System.Text.Json.Serialization;
using CodeMaster.Agent.Contracts;
using CodeMaster.Agent.Serialization;
using CodeMaster.Agent.Services;
using CodeMaster.Agent.Tools;
using CodeMaster.LocalAgent.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CodeMaster.LocalAgent.Services;

public sealed class LocalAgentChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly CodeMasterServerClient _serverClient;
    private readonly LocalAiProviderStore _providerStore;

    public LocalAgentChatService(CodeMasterServerClient serverClient, LocalAiProviderStore providerStore)
    {
        _serverClient = serverClient;
        _providerStore = providerStore;
    }

    public async Task<LocalExecutionResult> SaveProviderAsync(LocalExecutionRequest request)
    {
        var input = DeserializePayload<SaveLocalAiProviderRequest>(request.Payload);
        await _providerStore.SaveAsync(request.ServerBaseUrl, input);
        return LocalExecutionResult.Ok("Local AI provider saved.");
    }

    public async Task<LocalExecutionResult> DeleteProviderAsync(LocalExecutionRequest request)
    {
        var providerId = GetRequiredLong(request.Payload, "providerId", "id");
        await _providerStore.DeleteAsync(request.ServerBaseUrl, providerId);
        return LocalExecutionResult.Ok("Local AI provider deleted.");
    }

    public async Task<LocalExecutionResult> TestProviderAsync(LocalExecutionRequest request)
    {
        var providerId = GetRequiredLong(request.Payload, "providerId", "id");
        var settings = await _providerStore.GetAsync(request.ServerBaseUrl, providerId);
        using var client = AiChatClientFactory.Create(settings);
        var response = await client.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, "Reply with OK.") },
            new ChatOptions { MaxOutputTokens = 16 });
        var text = response.Text?.Trim();
        return LocalExecutionResult.OkData(new AiProviderTestResult
        {
            Success = true,
            SupportsTools = true,
            SupportsStreaming = true,
            Message = "Local model connection succeeded.",
            ResponseText = text
        });
    }

    public async Task<LocalExecutionResult> ChatAsync(LocalExecutionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            throw new InvalidOperationException("The CodeMaster login token is required for local Agent execution.");

        var input = DeserializePayload<SendAiMessageRequest>(request.Payload);
        var begin = await _serverClient.BeginLocalAiChatAsync(
            request.ServerBaseUrl,
            input,
            request.AccessToken);
        if (begin.AlreadyCompleted && begin.CompletedResult != null)
            return LocalExecutionResult.OkData(begin.CompletedResult);

        var settings = await _providerStore.GetAsync(request.ServerBaseUrl, begin.ProviderId);
        using var chatClient = AiChatClientFactory.Create(settings);
        var availableTools = new HashSet<string>(begin.AvailableTools, StringComparer.Ordinal);

        Task<string> InvokeAsync(string toolName)
        {
            var element = JsonSerializer.SerializeToElement(new { }, AgentJsonSerializer.Options);
            return InvokeRemoteToolAsync(request, begin, toolName, element);
        }

        Task<string> InvokeWithArgumentAsync(string toolName, string parameterName, object value)
        {
            var element = JsonSerializer.SerializeToElement(
                new Dictionary<string, object?> { [parameterName] = value },
                AgentJsonSerializer.Options);
            return InvokeRemoteToolAsync(request, begin, toolName, element);
        }

        var tools = CodeMasterAgentToolCatalog.Create(
            availableTools.Contains(CodeMasterAgentToolCatalog.GetProjectStructure),
            () => InvokeAsync(CodeMasterAgentToolCatalog.GetProjectStructure),
            query => InvokeWithArgumentAsync(CodeMasterAgentToolCatalog.GetEntityBlueprint, "query", query),
            query => InvokeWithArgumentAsync(CodeMasterAgentToolCatalog.GetUiPageBlueprint, "query", query),
            availableTools.Contains(CodeMasterAgentToolCatalog.ProposeProjectChangeSet),
            proposal => InvokeWithArgumentAsync(CodeMasterAgentToolCatalog.ProposeProjectChangeSet, "proposal", proposal),
            proposal => InvokeWithArgumentAsync(CodeMasterAgentToolCatalog.ProposeUiPageEnhancement, "proposal", proposal));

        var agent = new ChatClientAgent(
            chatClient,
            instructions: begin.Instructions,
            name: "CodeMasterLocalAgent",
            description: "CodeMaster project modeling and code generation assistant running on the user's computer.",
            tools: tools);
        var session = await agent.CreateSessionAsync();
        var messages = begin.Messages.Select(ToChatMessage).ToList();
        var response = await agent.RunAsync(messages, session);
        var responseText = response.ToString();
        if (string.IsNullOrWhiteSpace(responseText))
            responseText = "The model returned an empty response.";

        var result = await _serverClient.CompleteLocalAiChatAsync(
            request.ServerBaseUrl,
            new CompleteLocalAiChatRequest
            {
                ConversationId = begin.ConversationId,
                RequestId = begin.RequestId,
                ResponseText = responseText
            },
            request.AccessToken);
        return LocalExecutionResult.OkData(result);
    }

    private async Task<string> InvokeRemoteToolAsync(
        LocalExecutionRequest request,
        BeginLocalAiChatResult begin,
        string toolName,
        JsonElement arguments)
    {
        var result = await _serverClient.InvokeLocalAiToolAsync(
            request.ServerBaseUrl,
            new InvokeLocalAiToolRequest
            {
                ConversationId = begin.ConversationId,
                RequestId = begin.RequestId,
                ToolName = toolName,
                Arguments = arguments
            },
            request.AccessToken);
        return result.Output;
    }

    private static ChatMessage ToChatMessage(AiMessageDto message)
    {
        var role = message.Role switch
        {
            "assistant" => ChatRole.Assistant,
            "system" => ChatRole.System,
            _ => ChatRole.User
        };
        return new ChatMessage(role, message.Content);
    }

    private static T DeserializePayload<T>(JsonElement payload)
    {
        return payload.Deserialize<T>(JsonOptions)
            ?? throw new ArgumentException("Local Agent request payload is invalid.");
    }

    private static long GetRequiredLong(JsonElement payload, params string[] names)
    {
        foreach (var name in names)
        {
            if (!payload.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number)) return number;
            if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out number)) return number;
        }
        throw new ArgumentException($"Missing required value: {string.Join("/", names)}");
    }
}
