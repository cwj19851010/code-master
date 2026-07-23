using CodeMaster.Agent.Contracts;
using CodeMaster.Agent.Tools;
using Microsoft.Extensions.AI;

namespace CodeMaster.Agent.Tests.Services;

public class CodeMasterAgentToolCatalogTests
{
    [Fact]
    public async Task Entity_blueprint_tool_accepts_named_complex_argument()
    {
        EntityBlueprintQuery? captured = null;
        var tools = CodeMasterAgentToolCatalog.Create(
            includeReadTools: true,
            getProjectStructure: () => Task.FromResult("project"),
            getEntityBlueprint: query =>
            {
                captured = query;
                return Task.FromResult("entity");
            },
            getUiPageBlueprint: _ => Task.FromResult("page"),
            includeChangeTools: false,
            proposeProjectChangeSet: _ => Task.FromResult("change"),
            proposeUiPageEnhancement: _ => Task.FromResult("ui"));
        var function = tools.OfType<AIFunction>()
            .Single(item => item.Name == CodeMasterAgentToolCatalog.GetEntityBlueprint);

        var result = await function.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["query"] = new EntityBlueprintQuery
            {
                EntityId = 2077646988983275500,
                EntityName = "Order"
            }
        }));

        Assert.Equal("entity", result?.ToString());
        Assert.NotNull(captured);
        Assert.Equal(2077646988983275500, captured.EntityId);
        Assert.Equal("Order", captured.EntityName);
    }
}
