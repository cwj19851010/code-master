using System.Text.Json;
using CodeMaster.Agent.Contracts;
using CodeMaster.Agent.Serialization;
using CodeMaster.Agent.Services;
using CodeMaster.Application.Dtos.CodeGen;

namespace CodeMaster.Agent.Tests.Services;

public class AgentClientActionTests
{
    private const long UnsafeJavascriptId = 2077646988983275501;

    [Fact]
    public void Serializer_WritesLongIdsAsStrings()
    {
        var json = JsonSerializer.Serialize(new AiClientActionDto
        {
            Action = "generateProjectIncrementalCode",
            ProjectId = UnsafeJavascriptId,
            EntityIds = { UnsafeJavascriptId }
        }, AgentJsonSerializer.Options);

        Assert.Contains($"\"projectId\":\"{UnsafeJavascriptId}\"", json, StringComparison.Ordinal);
        Assert.Contains($"\"entityIds\":[\"{UnsafeJavascriptId}\"]", json, StringComparison.Ordinal);

        var restored = JsonSerializer.Deserialize<AiClientActionDto>(json, AgentJsonSerializer.Options);
        Assert.NotNull(restored);
        Assert.Equal(UnsafeJavascriptId, restored.ProjectId);
        Assert.Equal(UnsafeJavascriptId, Assert.Single(restored.EntityIds));
    }

    [Fact]
    public void Serializer_PreservesUiEnhancementIdsAndOperations()
    {
        var json = JsonSerializer.Serialize(new AiClientActionDto
        {
            Action = "enhanceUiPage",
            ProjectId = UnsafeJavascriptId,
            EntityId = UnsafeJavascriptId + 1,
            TargetKind = "EntityPage",
            PageType = "edit",
            Operations =
            {
                new ProjectUiNodeOperationDto
                {
                    Type = "SetGrid",
                    TargetGenId = $"gen_field_{UnsafeJavascriptId + 2}",
                    Md = 12
                }
            }
        }, AgentJsonSerializer.Options);

        Assert.Contains($"\"projectId\":\"{UnsafeJavascriptId}\"", json, StringComparison.Ordinal);
        Assert.Contains($"\"entityId\":\"{UnsafeJavascriptId + 1}\"", json, StringComparison.Ordinal);
        Assert.Contains($"gen_field_{UnsafeJavascriptId + 2}", json, StringComparison.Ordinal);

        var restored = JsonSerializer.Deserialize<AiClientActionDto>(json, AgentJsonSerializer.Options);
        Assert.NotNull(restored);
        Assert.Equal(UnsafeJavascriptId + 1, restored.EntityId);
        Assert.Equal(12, Assert.Single(restored.Operations).Md);
    }

    [Fact]
    public void AddClientActions_CollapsesFullProjectGenerationToOneProjectAction()
    {
        var proposal = new ProjectChangeSetProposal { GenerationMode = "Full" };
        var result = new ProjectChangeSetExecutionResult { ProjectId = 10 };
        var entities = new[]
        {
            new ModuleEntityDto { Id = UnsafeJavascriptId, Name = "Order" },
            new ModuleEntityDto { Id = UnsafeJavascriptId + 1, Name = "OrderItem" }
        };

        CodeMasterProjectChangeSetService.AddClientActions(10, proposal, entities, result);

        Assert.Equal(2, result.ClientActions.Count);
        var action = result.ClientActions[0];
        Assert.Equal("generateProjectCode", action.Action);
        Assert.Equal(10, action.ProjectId);
        Assert.Empty(action.EntityIds);
        Assert.Equal("buildProject", result.ClientActions[1].Action);
    }

    [Fact]
    public void AddClientActions_CollapsesIncrementalGenerationAndKeepsExactIds()
    {
        var proposal = new ProjectChangeSetProposal
        {
            GenerationMode = "Incremental",
            GenerationEntityIds = { UnsafeJavascriptId, UnsafeJavascriptId + 1 }
        };
        var result = new ProjectChangeSetExecutionResult { ProjectId = 10 };

        CodeMasterProjectChangeSetService.AddClientActions(10, proposal, Array.Empty<ModuleEntityDto>(), result);

        Assert.Equal(2, result.ClientActions.Count);
        var action = result.ClientActions[0];
        Assert.Equal("generateProjectIncrementalCode", action.Action);
        Assert.Equal(new[] { UnsafeJavascriptId, UnsafeJavascriptId + 1 }, action.EntityIds);
        Assert.Equal("buildProject", result.ClientActions[1].Action);
    }

    [Fact]
    public void BuildFailure_TriggersAutomaticRepairAnalysis()
    {
        Assert.True(AiConversationService.IsBuildVerificationFailure(new CompleteAiClientActionsRequest
        {
            Success = false,
            FailedAction = "buildProject",
            ErrorMessage = "dotnet build failed"
        }));
        Assert.False(AiConversationService.IsBuildVerificationFailure(new CompleteAiClientActionsRequest
        {
            Success = false,
            FailedAction = "generateProjectCode"
        }));
    }

    [Fact]
    public void RepairRequestIds_TrackAndLimitTheRepairAttempt()
    {
        var first = AiConversationService.BuildRepairRequestId("request-1", 10, 1);
        var third = AiConversationService.BuildRepairRequestId(first, 10, 3);

        Assert.Equal("request-1.repair1", first);
        Assert.Equal("request-1.repair3", third);
        Assert.Equal(3, AiConversationService.GetRepairAttempt(third));
        Assert.True(third.Length <= 64);
    }
}
