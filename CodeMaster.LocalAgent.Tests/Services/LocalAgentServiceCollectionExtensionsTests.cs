using CodeMaster.LocalAgent.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CodeMaster.LocalAgent.Tests.Services;

public class LocalAgentServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCodeMasterLocalExecution_RegistersCompleteExecutionGraph()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalAgent:MetadataRoot"] = Path.Combine(Path.GetTempPath(), "codemaster-local-agent-tests")
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddCodeMasterLocalExecution(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<LocalAiProviderStore>());
        Assert.NotNull(provider.GetRequiredService<LocalAgentChatService>());
        Assert.NotNull(provider.GetRequiredService<LocalCodegenExecutionService>());
    }
}
