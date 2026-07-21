using CodeMaster.Agent.Contracts;
using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.Ai;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SqlSugar;

namespace CodeMaster.Agent.Services;

public interface IAiProviderService
{
    Task<List<AiProviderDto>> GetListAsync();
    Task<AiProviderDto> CreateAsync(SaveAiProviderRequest input);
    Task<AiProviderDto> UpdateAsync(long id, SaveAiProviderRequest input);
    Task<bool> DeleteAsync(long id);
    Task<AiProviderTestResult> TestAsync(long id);
}

internal sealed class AiProviderService : IAiProviderService
{
    private readonly IRepository<SysAiProvider> _repository;
    private readonly IRepository<AiConversation> _conversationRepository;
    private readonly ISqlSugarClient _db;
    private readonly IAiCurrentUser _currentUser;
    private readonly IAiSecretProtector _secretProtector;
    private readonly IAiProviderFactory _providerFactory;

    public AiProviderService(
        IRepository<SysAiProvider> repository,
        IRepository<AiConversation> conversationRepository,
        ISqlSugarClient db,
        IAiCurrentUser currentUser,
        IAiSecretProtector secretProtector,
        IAiProviderFactory providerFactory)
    {
        _repository = repository;
        _conversationRepository = conversationRepository;
        _db = db;
        _currentUser = currentUser;
        _secretProtector = secretProtector;
        _providerFactory = providerFactory;
    }

    public async Task<List<AiProviderDto>> GetListAsync()
    {
        var userId = _currentUser.UserId;
        var items = await _repository.GetQueryable()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .OrderByDescending(x => x.CreateTime)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    public async Task<AiProviderDto> CreateAsync(SaveAiProviderRequest input)
    {
        Validate(input);
        var entity = new SysAiProvider
        {
            UserId = _currentUser.UserId,
            Name = input.Name.Trim(),
            ProviderType = input.ProviderType,
            ExecutionMode = input.ExecutionMode,
            BaseUrl = NormalizeBaseUrl(input.ProviderType, input.BaseUrl),
            ApiKeyCipherText = string.IsNullOrWhiteSpace(input.ApiKey) ? null : _secretProtector.Protect(input.ApiKey.Trim()),
            ModelName = input.ModelName.Trim(),
            ExtraHeadersJson = NormalizeOptional(input.ExtraHeadersJson),
            IsDefault = input.IsDefault,
            IsEnabled = input.IsEnabled,
            CreateUserId = _currentUser.UserId,
            CreateBy = _currentUser.UserName,
            CreateTime = DateTime.UtcNow
        };

        entity.Id = await _repository.InsertAsync(entity);
        if (entity.IsDefault)
        {
            await ClearOtherDefaultsAsync(entity.Id);
        }

        return ToDto(entity);
    }

    public async Task<AiProviderDto> UpdateAsync(long id, SaveAiProviderRequest input)
    {
        Validate(input);
        var entity = await GetOwnedEntityAsync(id);
        entity.Name = input.Name.Trim();
        entity.ProviderType = input.ProviderType;
        entity.ExecutionMode = input.ExecutionMode;
        entity.BaseUrl = NormalizeBaseUrl(input.ProviderType, input.BaseUrl);
        entity.ModelName = input.ModelName.Trim();
        entity.ExtraHeadersJson = NormalizeOptional(input.ExtraHeadersJson);
        entity.IsDefault = input.IsDefault;
        entity.IsEnabled = input.IsEnabled;
        entity.UpdateUserId = _currentUser.UserId;
        entity.UpdateBy = _currentUser.UserName;
        entity.UpdateTime = DateTime.UtcNow;

        if (input.ClearApiKey)
        {
            entity.ApiKeyCipherText = null;
        }
        else if (!string.IsNullOrWhiteSpace(input.ApiKey))
        {
            entity.ApiKeyCipherText = _secretProtector.Protect(input.ApiKey.Trim());
        }

        await _repository.UpdateAsync(entity);
        if (entity.IsDefault)
        {
            await ClearOtherDefaultsAsync(entity.Id);
        }

        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        _ = await GetOwnedEntityAsync(id);
        var activeConversationCount = await _conversationRepository.GetQueryable()
            .Where(x => x.UserId == _currentUser.UserId && x.ProviderId == id && x.Status == "Active")
            .CountAsync();
        if (activeConversationCount > 0)
        {
            throw new InvalidOperationException("This provider is used by active conversations. Archive those conversations or disable the provider first.");
        }

        return await _repository.DeleteAsync(id) > 0;
    }

    public async Task<AiProviderTestResult> TestAsync(long id)
    {
        var entity = await GetOwnedEntityAsync(id);
        if (string.Equals(entity.ExecutionMode, AiExecutionModes.Local, StringComparison.OrdinalIgnoreCase))
        {
            return new AiProviderTestResult
            {
                Success = false,
                Message = "Local providers must be tested by the Tauri LocalAgent."
            };
        }

        try
        {
            var client = _providerFactory.CreateChatClient(entity);
            var toolInvoked = false;
            string CapabilityProbe()
            {
                toolInvoked = true;
                return "CODEMASTER_TOOL_OK";
            }

            var tool = AIFunctionFactory.Create(
                (Func<string>)CapabilityProbe,
                name: "codemaster_capability_probe",
                description: "Return the CodeMaster provider capability probe result.");

            var agent = new ChatClientAgent(
                client,
                instructions: "You are a connectivity probe. Call codemaster_capability_probe exactly once, then reply CODEMASTER_OK.",
                name: "CodeMasterProviderProbe",
                tools: [tool]);

            var response = await agent.RunAsync("Test the configured model connection and tool calling capability.");
            var responseText = response.ToString();
            entity.SupportsTools = toolInvoked;
            entity.SupportsStreaming = true;
            entity.LastTestAt = DateTime.UtcNow;
            entity.LastTestStatus = "Success";
            entity.LastTestMessage = responseText.Length > 1000 ? responseText[..1000] : responseText;
            entity.UpdateTime = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);

            return new AiProviderTestResult
            {
                Success = true,
                SupportsTools = toolInvoked,
                SupportsStreaming = true,
                Message = toolInvoked ? "Connection and tool calling succeeded." : "Connection succeeded, but tool calling was not detected.",
                ResponseText = responseText
            };
        }
        catch (Exception ex)
        {
            entity.SupportsTools = false;
            entity.SupportsStreaming = false;
            entity.LastTestAt = DateTime.UtcNow;
            entity.LastTestStatus = "Failed";
            entity.LastTestMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            entity.UpdateTime = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);

            return new AiProviderTestResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    private async Task<SysAiProvider> GetOwnedEntityAsync(long id)
    {
        var userId = _currentUser.UserId;
        var entity = await _repository.GetQueryable()
            .Where(x => x.Id == id && x.UserId == userId)
            .FirstAsync();

        return entity ?? throw new KeyNotFoundException("AI provider was not found.");
    }

    private async Task ClearOtherDefaultsAsync(long currentId)
    {
        var userId = _currentUser.UserId;
        await _db.Updateable<SysAiProvider>()
            .SetColumns(x => x.IsDefault == false)
            .Where(x => x.UserId == userId && x.Id != currentId && x.IsDefault)
            .ExecuteCommandAsync();
    }

    private static void Validate(SaveAiProviderRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new ArgumentException("Provider name is required.");
        }

        if (string.IsNullOrWhiteSpace(input.ModelName))
        {
            throw new ArgumentException("Model name is required.");
        }

        if (input.ProviderType is not (AiProviderTypes.OpenAiCompatible or AiProviderTypes.Anthropic))
        {
            throw new ArgumentException("Unsupported provider type.");
        }

        if (input.ExecutionMode is not (AiExecutionModes.Server or AiExecutionModes.Local))
        {
            throw new ArgumentException("Unsupported execution mode.");
        }

        if (!string.IsNullOrWhiteSpace(input.BaseUrl) && !Uri.TryCreate(input.BaseUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Base URL is invalid.");
        }

        if (input.ProviderType == AiProviderTypes.OpenAiCompatible)
        {
            _ = OpenAiCompatibleHttpHandler.ParseExtraHeaders(input.ExtraHeadersJson);
        }
    }

    private static string? NormalizeBaseUrl(string providerType, string? baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            return baseUrl.Trim().TrimEnd('/');
        }

        return providerType == AiProviderTypes.Anthropic
            ? "https://api.anthropic.com"
            : "https://api.openai.com/v1";
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AiProviderDto ToDto(SysAiProvider entity)
    {
        return new AiProviderDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ProviderType = entity.ProviderType,
            ExecutionMode = entity.ExecutionMode,
            BaseUrl = entity.BaseUrl,
            ModelName = entity.ModelName,
            ExtraHeadersJson = entity.ExtraHeadersJson,
            HasApiKey = !string.IsNullOrWhiteSpace(entity.ApiKeyCipherText),
            IsDefault = entity.IsDefault,
            IsEnabled = entity.IsEnabled,
            SupportsTools = entity.SupportsTools,
            SupportsStreaming = entity.SupportsStreaming,
            LastTestAt = entity.LastTestAt,
            LastTestStatus = entity.LastTestStatus,
            LastTestMessage = entity.LastTestMessage
        };
    }
}
