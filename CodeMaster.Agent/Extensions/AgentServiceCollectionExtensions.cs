using CodeMaster.Agent.Services;
using CodeMaster.Agent.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace CodeMaster.Agent.Extensions;

public static class AgentServiceCollectionExtensions
{
    public static IServiceCollection AddCodeMasterAgent(this IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddScoped<IAiCurrentUser, AiCurrentUser>();
        services.AddScoped<IAiAuthorizationService, AiAuthorizationService>();
        services.AddScoped<IAiSecretProtector, AiSecretProtector>();
        services.AddScoped<IAiProviderFactory, AiProviderFactory>();
        services.AddScoped<IAiProviderService, AiProviderService>();
        services.AddScoped<ICodeMasterProjectBlueprintService, CodeMasterProjectBlueprintService>();
        services.AddScoped<ICodeMasterProjectChangeSetService, CodeMasterProjectChangeSetService>();
        services.AddScoped<ICodeMasterAgentToolFactory, CodeMasterAgentToolFactory>();
        services.AddScoped<IAiConversationService, AiConversationService>();
        return services;
    }
}
