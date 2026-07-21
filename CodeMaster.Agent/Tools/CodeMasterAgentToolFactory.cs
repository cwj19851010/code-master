using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeMaster.Agent.Contracts;
using CodeMaster.Agent.Serialization;
using CodeMaster.Agent.Services;
using CodeMaster.Application.Dtos.CodeGen;
using CodeMaster.Application.Services.CodeGen;
using CodeMaster.Core.Repositories;
using CodeMaster.Domain.Entities.Ai;
using Microsoft.Extensions.AI;
using SqlSugar;

namespace CodeMaster.Agent.Tools;

public interface ICodeMasterAgentToolFactory
{
    Task<IList<AITool>> CreateToolsAsync(long conversationId, long projectId, string requestId);
    Task<AiToolExecutionDto> ApproveAsync(long executionId);
    Task<AiToolExecutionDto> RejectAsync(long executionId);
    Task<AiToolExecutionDto> CompleteClientActionsAsync(long executionId, CompleteAiClientActionsRequest request);
}

internal sealed class CodeMasterAgentToolFactory : ICodeMasterAgentToolFactory
{
    private static readonly JsonSerializerOptions JsonOptions = AgentJsonSerializer.Options;

    private readonly IProjectService _projectService;
    private readonly IProjectModuleService _moduleService;
    private readonly IModuleEntityService _entityService;
    private readonly ICodeMasterProjectBlueprintService _blueprintService;
    private readonly ICodeMasterProjectChangeSetService _changeSetService;
    private readonly IRepository<AiToolExecution> _executionRepository;
    private readonly IRepository<AiConversation> _conversationRepository;
    private readonly ISqlSugarClient _db;
    private readonly IAiCurrentUser _currentUser;
    private readonly IAiAuthorizationService _authorizationService;

    public CodeMasterAgentToolFactory(
        IProjectService projectService,
        IProjectModuleService moduleService,
        IModuleEntityService entityService,
        ICodeMasterProjectBlueprintService blueprintService,
        ICodeMasterProjectChangeSetService changeSetService,
        IRepository<AiToolExecution> executionRepository,
        IRepository<AiConversation> conversationRepository,
        ISqlSugarClient db,
        IAiCurrentUser currentUser,
        IAiAuthorizationService authorizationService)
    {
        _projectService = projectService;
        _moduleService = moduleService;
        _entityService = entityService;
        _blueprintService = blueprintService;
        _changeSetService = changeSetService;
        _executionRepository = executionRepository;
        _conversationRepository = conversationRepository;
        _db = db;
        _currentUser = currentUser;
        _authorizationService = authorizationService;
    }

    public async Task<IList<AITool>> CreateToolsAsync(long conversationId, long projectId, string requestId)
    {
        async Task<string> GetProjectStructureAsync()
        {
            return JsonSerializer.Serialize(await _blueprintService.GetProjectAsync(projectId), JsonOptions);
        }

        async Task<string> GetEntityBlueprintAsync(EntityBlueprintQuery query)
        {
            var entity = await _blueprintService.GetEntityAsync(projectId, query.EntityId, query.EntityName);
            return JsonSerializer.Serialize(entity, JsonOptions);
        }

        async Task<string> GetUiPageBlueprintAsync(UiPageBlueprintQuery query)
        {
            var page = await _blueprintService.GetUiPageAsync(
                projectId,
                query.EntityId,
                query.EntityName,
                query.PageType);
            return JsonSerializer.Serialize(page, JsonOptions);
        }

        async Task<string> ProposeProjectChangeSetAsync(ProjectChangeSetProposal proposal)
        {
            _ = await GetProjectAsync(projectId);
            var validation = await _changeSetService.ValidateAsync(projectId, proposal);
            if (!validation.IsValid)
            {
                return JsonSerializer.Serialize(new
                {
                    success = false,
                    projectId,
                    validation.Errors,
                    validation.Warnings,
                    message = "The proposal is invalid. Correct the errors and submit a new change set."
                }, JsonOptions);
            }

            var execution = await CreatePendingExecutionAsync(conversationId, projectId, requestId, "project_change_set", proposal);
            return JsonSerializer.Serialize(new
            {
                success = true,
                pendingApprovalId = execution.Id,
                projectId,
                action = "project_change_set",
                proposal.Summary,
                validation.ModuleCount,
                validation.EntityCount,
                validation.FieldCount,
                validation.RelationCount,
                validation.UpdatedModuleCount,
                validation.UpdatedEntityCount,
                validation.DeletedModuleCount,
                validation.DeletedEntityCount,
                proposal.GenerationMode,
                validation.Warnings,
                message = "The complete project change set is waiting for user approval. Do not claim that any metadata has been created."
            }, JsonOptions);
        }

        async Task<string> ProposeUiPageEnhancementAsync(UiPageEnhancementProposal proposal)
        {
            _ = await GetProjectAsync(projectId);
            await ValidateUiPageEnhancementAsync(projectId, proposal);
            var execution = await CreatePendingExecutionAsync(
                conversationId,
                projectId,
                requestId,
                "ui_page_enhancement",
                proposal);

            return JsonSerializer.Serialize(new
            {
                success = true,
                pendingApprovalId = execution.Id,
                projectId,
                action = "ui_page_enhancement",
                proposal.Summary,
                proposal.TargetKind,
                proposal.Page,
                proposal.EntityId,
                proposal.EntityName,
                proposal.PageType,
                operationCount = proposal.Operations.Count,
                proposal.BuildAfterApply,
                message = "The UI design change is waiting for user approval. Stable genId values and business bindings will be preserved."
            }, JsonOptions);
        }

        var tools = new List<AITool>();
        if (await _authorizationService.HasAnyAsync("codegen:project:list", "codegen:project:view"))
        {
            tools.Add(AIFunctionFactory.Create(
                (Func<Task<string>>)GetProjectStructureAsync,
                name: "get_project_structure",
                description: "Read the complete CodeMaster blueprint for the project bound to this conversation, including modules, entities, every field option, relations, generation state, and the available control/template catalog."));

            tools.Add(AIFunctionFactory.Create(
                (Func<EntityBlueprintQuery, Task<string>>)GetEntityBlueprintAsync,
                name: "get_entity_blueprint",
                description: "Read one entity in full detail by entityId or entityName from the project bound to this conversation."));

            tools.Add(AIFunctionFactory.Create(
                (Func<UiPageBlueprintQuery, Task<string>>)GetUiPageBlueprintAsync,
                name: "get_ui_page_blueprint",
                description: "Read the stable generated node identifiers for one entity page. Use this before proposing page layout, grouping, grid, visual property, movement, or compatible node-type changes."));
        }

        if (await _authorizationService.HasAnyAsync(
                "codegen:projectModule:create",
                "codegen:projectModule:update",
                "codegen:projectModule:delete",
                "codegen:moduleEntity:create",
                "codegen:moduleEntity:update",
                "codegen:moduleEntity:delete",
                "codegen:project:build"))
        {
            tools.Add(AIFunctionFactory.Create(
                (Func<ProjectChangeSetProposal, Task<string>>)ProposeProjectChangeSetAsync,
                name: "propose_project_change_set",
                description: "Validate and create one approval card for a complete CodeMaster project change set. It can create, edit, reorder, and delete modules, entities, fields, and relations, then request full or incremental generation through the existing Web/Tauri execution bridge."));

            tools.Add(AIFunctionFactory.Create(
                (Func<UiPageEnhancementProposal, Task<string>>)ProposeUiPageEnhancementAsync,
                name: "propose_ui_page_enhancement",
                description: "Create an approval card for a controlled visual redesign. Scaffold targets redesign Login or Dashboard. EntityPage targets index/add/edit/detail and uses stable genId-anchored SetTag, SetProp, RemoveProp, SetGrid, Move, and Group operations that are replayed after future code generation."));
        }

        return tools;
    }

    public async Task<AiToolExecutionDto> ApproveAsync(long executionId)
    {
        var execution = await GetOwnedExecutionAsync(executionId);
        if (execution.Status is "Completed" or "AwaitingClientExecution")
            return ToDto(execution);
        if (string.Equals(execution.Status, "Executing", StringComparison.Ordinal))
            throw new InvalidOperationException("This tool execution is already running.");
        if (!string.Equals(execution.Status, "PendingApproval", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("This tool execution is no longer pending approval.");
        }

        var approvedAt = DateTime.UtcNow;
        var claimed = await _db.Updateable<AiToolExecution>()
            .SetColumns(item => item.Status == "Executing")
            .SetColumns(item => item.ApprovedAt == approvedAt)
            .SetColumns(item => item.UpdateUserId == _currentUser.UserId)
            .SetColumns(item => item.UpdateBy == _currentUser.UserName)
            .SetColumns(item => item.UpdateTime == approvedAt)
            .Where(item => item.Id == executionId &&
                           item.UserId == _currentUser.UserId &&
                           item.Status == "PendingApproval")
            .ExecuteCommandAsync();
        if (claimed == 0)
        {
            execution = await GetOwnedExecutionAsync(executionId);
            if (execution.Status is "Completed" or "AwaitingClientExecution")
                return ToDto(execution);
            throw new InvalidOperationException("This tool execution was already claimed by another request.");
        }

        execution = await GetOwnedExecutionAsync(executionId);

        try
        {
            await DemandToolPermissionAsync(execution);
            object result = execution.ToolName switch
            {
                "create_module" => await ExecuteCreateModuleAsync(execution),
                "create_entity" => await ExecuteCreateEntityAsync(execution),
                "project_change_set" => await ExecuteProjectChangeSetAsync(execution),
                "ui_page_enhancement" => await ExecuteUiPageEnhancementAsync(execution),
                _ => throw new InvalidOperationException($"Unsupported approved tool: {execution.ToolName}")
            };

            var waitsForClient = result switch
            {
                ProjectChangeSetExecutionResult { ClientActions.Count: > 0 } => true,
                UiPageEnhancementExecutionResult { ClientActions.Count: > 0 } => true,
                _ => false
            };
            execution.Status = waitsForClient ? "AwaitingClientExecution" : "Completed";
            execution.ApprovedAt = DateTime.UtcNow;
            execution.CompletedAt = waitsForClient ? null : DateTime.UtcNow;
            execution.OutputJson = AiToolPayloadCodec.Encode(JsonSerializer.Serialize(result, JsonOptions));
            execution.UpdateUserId = _currentUser.UserId;
            execution.UpdateBy = _currentUser.UserName;
            execution.UpdateTime = DateTime.UtcNow;
            await _executionRepository.UpdateAsync(execution);
        }
        catch (Exception ex)
        {
            execution.Status = "Failed";
            execution.ApprovedAt = DateTime.UtcNow;
            execution.CompletedAt = DateTime.UtcNow;
            execution.ErrorMessage = Truncate(ex.Message, 2000);
            execution.UpdateTime = DateTime.UtcNow;
            await _executionRepository.UpdateAsync(execution);
            throw;
        }

        return ToDto(execution);
    }

    public async Task<AiToolExecutionDto> CompleteClientActionsAsync(
        long executionId,
        CompleteAiClientActionsRequest request)
    {
        var execution = await GetOwnedExecutionAsync(executionId);
        if (string.Equals(execution.Status, "Completed", StringComparison.Ordinal) && request.Success)
            return ToDto(execution);

        if (!string.Equals(execution.Status, "AwaitingClientExecution", StringComparison.Ordinal))
            throw new InvalidOperationException("This tool execution is not waiting for client-side work.");

        var clientOutput = string.IsNullOrWhiteSpace(request.DiagnosticOutput)
            ? request.Output
            : request.DiagnosticOutput;
        if (!string.IsNullOrWhiteSpace(execution.OutputJson) && execution.ToolName == "project_change_set")
        {
            var result = JsonSerializer.Deserialize<ProjectChangeSetExecutionResult>(
                AiToolPayloadCodec.Decode(execution.OutputJson),
                JsonOptions);
            if (result != null)
            {
                result.ClientExecutionOutput = string.IsNullOrWhiteSpace(clientOutput)
                    ? null
                    : Truncate(clientOutput, 6000);
                result.ClientExecutionFailedAction = string.IsNullOrWhiteSpace(request.FailedAction)
                    ? null
                    : Truncate(request.FailedAction, 100);
                result.ClientExecutionError = string.IsNullOrWhiteSpace(request.ErrorMessage)
                    ? null
                    : Truncate(request.ErrorMessage, 2000);
                execution.OutputJson = AiToolPayloadCodec.Encode(JsonSerializer.Serialize(result, JsonOptions));
            }
        }
        else if (!string.IsNullOrWhiteSpace(execution.OutputJson) && execution.ToolName == "ui_page_enhancement")
        {
            var result = JsonSerializer.Deserialize<UiPageEnhancementExecutionResult>(
                AiToolPayloadCodec.Decode(execution.OutputJson),
                JsonOptions);
            if (result != null)
            {
            result.ClientExecutionOutput = string.IsNullOrWhiteSpace(clientOutput)
                ? null
                : Truncate(clientOutput, 6000);
            result.ClientExecutionFailedAction = string.IsNullOrWhiteSpace(request.FailedAction)
                ? null
                : Truncate(request.FailedAction, 100);
            result.ClientExecutionError = string.IsNullOrWhiteSpace(request.ErrorMessage)
                ? null
                : Truncate(request.ErrorMessage, 2000);
            execution.OutputJson = AiToolPayloadCodec.Encode(JsonSerializer.Serialize(result, JsonOptions));
            }
        }

        execution.Status = request.Success ? "Completed" : "Failed";
        execution.CompletedAt = DateTime.UtcNow;
        execution.ErrorMessage = request.Success
            ? null
            : Truncate(request.ErrorMessage ?? "Client-side execution failed.", 2000);
        execution.UpdateUserId = _currentUser.UserId;
        execution.UpdateBy = _currentUser.UserName;
        execution.UpdateTime = DateTime.UtcNow;
        await _executionRepository.UpdateAsync(execution);
        return ToDto(execution);
    }

    public async Task<AiToolExecutionDto> RejectAsync(long executionId)
    {
        var execution = await GetOwnedExecutionAsync(executionId);
        if (string.Equals(execution.Status, "Rejected", StringComparison.Ordinal))
            return ToDto(execution);
        if (!string.Equals(execution.Status, "PendingApproval", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("This tool execution is no longer pending approval.");
        }

        var completedAt = DateTime.UtcNow;
        var updated = await _db.Updateable<AiToolExecution>()
            .SetColumns(item => item.Status == "Rejected")
            .SetColumns(item => item.CompletedAt == completedAt)
            .SetColumns(item => item.UpdateUserId == _currentUser.UserId)
            .SetColumns(item => item.UpdateBy == _currentUser.UserName)
            .SetColumns(item => item.UpdateTime == completedAt)
            .Where(item => item.Id == executionId &&
                           item.UserId == _currentUser.UserId &&
                           item.Status == "PendingApproval")
            .ExecuteCommandAsync();
        if (updated == 0)
            throw new InvalidOperationException("This tool execution was already handled by another request.");
        execution = await GetOwnedExecutionAsync(executionId);
        return ToDto(execution);
    }

    private async Task<object> ExecuteCreateModuleAsync(AiToolExecution execution)
    {
        var proposal = DeserializeRequired<CreateModuleProposal>(execution.InputJson);
        ValidateModuleProposal(proposal);
        var id = await _moduleService.CreateAsync(new CreateProjectModuleDto
        {
            ProjectId = execution.ProjectId,
            ModuleName = proposal.ModuleName.Trim(),
            ModuleDescription = proposal.ModuleDescription.Trim(),
            Icon = NormalizeOptional(proposal.Icon),
            OrderNum = proposal.OrderNum,
            RoutePath = NormalizeOptional(proposal.RoutePath),
            Remark = NormalizeOptional(proposal.Remark)
        });

        return new { id, projectId = execution.ProjectId, proposal.ModuleName };
    }

    private async Task DemandToolPermissionAsync(AiToolExecution execution)
    {
        switch (execution.ToolName)
        {
            case "create_module":
                await _authorizationService.DemandAnyAsync("codegen:projectModule:create");
                break;
            case "create_entity":
                await _authorizationService.DemandAnyAsync("codegen:moduleEntity:create");
                break;
            case "project_change_set":
            {
                var proposal = DeserializeRequired<ProjectChangeSetProposal>(execution.InputJson);
                if (proposal.Modules.Count > 0)
                    await _authorizationService.DemandAnyAsync("codegen:projectModule:create");
                if (proposal.Entities.Count > 0)
                    await _authorizationService.DemandAnyAsync("codegen:moduleEntity:create");
                if (proposal.ModuleUpdates.Count > 0)
                    await _authorizationService.DemandAnyAsync("codegen:projectModule:update");
                if (proposal.EntityUpdates.Count > 0)
                    await _authorizationService.DemandAnyAsync("codegen:moduleEntity:update");
                if (proposal.DeleteModuleIds.Count > 0)
                    await _authorizationService.DemandAnyAsync("codegen:projectModule:delete", "codegen:projectModule:update");
                if (proposal.DeleteEntityIds.Count > 0)
                    await _authorizationService.DemandAnyAsync("codegen:moduleEntity:delete", "codegen:moduleEntity:update");
                if (!string.Equals(proposal.GenerationMode, "None", StringComparison.OrdinalIgnoreCase))
                    await _authorizationService.DemandAnyAsync("codegen:moduleEntity:update");
                if (proposal.BuildAfterGeneration)
                    await _authorizationService.DemandAnyAsync("codegen:project:build");
                break;
            }
            case "ui_page_enhancement":
                await _authorizationService.DemandAnyAsync("codegen:project:build", "codegen:moduleEntity:update");
                break;
            default:
                throw new InvalidOperationException($"Unsupported approved tool: {execution.ToolName}");
        }
    }

    private async Task<object> ExecuteProjectChangeSetAsync(AiToolExecution execution)
    {
        var proposal = DeserializeRequired<ProjectChangeSetProposal>(execution.InputJson);
        return await _changeSetService.ExecuteAsync(execution.ProjectId, proposal);
    }

    private async Task<object> ExecuteUiPageEnhancementAsync(AiToolExecution execution)
    {
        var proposal = DeserializeRequired<UiPageEnhancementProposal>(execution.InputJson);
        await ValidateUiPageEnhancementAsync(execution.ProjectId, proposal);

        var action = new AiClientActionDto
        {
            Action = "enhanceUiPage",
            ProjectId = execution.ProjectId,
            EntityId = proposal.EntityId,
            TargetKind = proposal.TargetKind,
            Page = proposal.Page,
            PageType = proposal.PageType,
            Style = proposal.Style,
            Headline = proposal.Headline,
            Subtitle = proposal.Subtitle,
            Highlights = proposal.Highlights,
            PrimaryColor = proposal.PrimaryColor,
            SecondaryColor = proposal.SecondaryColor,
            ReplaceExistingDesign = proposal.ReplaceExistingDesign,
            Operations = proposal.Operations
        };
        var result = new UiPageEnhancementExecutionResult
        {
            ProjectId = execution.ProjectId,
            Summary = proposal.Summary,
            TargetKind = proposal.TargetKind,
            Page = string.Equals(proposal.TargetKind, "EntityPage", StringComparison.OrdinalIgnoreCase)
                ? proposal.PageType ?? string.Empty
                : proposal.Page ?? string.Empty,
            ClientActions = { action }
        };
        if (proposal.BuildAfterApply)
        {
            result.ClientActions.Add(new AiClientActionDto
            {
                Action = "buildProject",
                ProjectId = execution.ProjectId
            });
        }

        return result;
    }

    private async Task<object> ExecuteCreateEntityAsync(AiToolExecution execution)
    {
        var proposal = DeserializeRequired<CreateEntityProposal>(execution.InputJson);
        await ValidateEntityProposalAsync(execution.ProjectId, proposal);
        var id = await _entityService.CreateAsync(new CreateModuleEntityDto
        {
            ProjectId = execution.ProjectId,
            ModuleId = proposal.ModuleId,
            Name = proposal.Name.Trim(),
            Description = proposal.Description.Trim(),
            HasPrimaryKey = proposal.HasPrimaryKey,
            TableName = NormalizeOptional(proposal.TableName),
            IsTree = proposal.IsTree,
            IsReadOnly = proposal.IsReadOnly,
            HasTenant = proposal.HasTenant,
            HasDataPermission = proposal.HasDataPermission,
            HasAudit = proposal.HasAudit,
            HasSoftDelete = proposal.HasSoftDelete,
            GenerateFrontend = proposal.GenerateFrontend,
            FrontendRoute = NormalizeOptional(proposal.FrontendRoute),
            MenuIcon = NormalizeOptional(proposal.MenuIcon),
            OrderNum = proposal.OrderNum,
            Fields = CodeMasterProjectChangeSetService.MapFields(proposal)
        });

        return new { id, projectId = execution.ProjectId, proposal.ModuleId, proposal.Name };
    }

    private async Task<AiToolExecution> CreatePendingExecutionAsync<T>(
        long conversationId,
        long projectId,
        string requestId,
        string toolName,
        T input)
    {
        var conversation = await _conversationRepository.GetQueryable()
            .Where(x => x.Id == conversationId && x.UserId == _currentUser.UserId && x.ProjectId == projectId)
            .FirstAsync();

        if (conversation == null)
        {
            throw new UnauthorizedAccessException("Conversation context is invalid.");
        }

        var rawInputJson = JsonSerializer.Serialize(input, JsonOptions);
        var inputHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawInputJson)));
        var existing = await _executionRepository.GetQueryable()
            .Where(item => item.ConversationId == conversationId &&
                           item.UserId == _currentUser.UserId &&
                           item.RequestId == requestId &&
                           item.ToolName == toolName &&
                           item.InputHash == inputHash)
            .FirstAsync();
        if (existing != null)
            return existing;

        var inputJson = AiToolPayloadCodec.Encode(rawInputJson);

        var execution = new AiToolExecution
        {
            ConversationId = conversationId,
            UserId = _currentUser.UserId,
            ProjectId = projectId,
            RequestId = requestId,
            InputHash = inputHash,
            ToolName = toolName,
            InputJson = inputJson,
            Status = "PendingApproval",
            CreateUserId = _currentUser.UserId,
            CreateBy = _currentUser.UserName,
            CreateTime = DateTime.UtcNow
        };
        execution.Id = await _executionRepository.InsertAsync(execution);
        return execution;
    }

    private async Task<AiToolExecution> GetOwnedExecutionAsync(long executionId)
    {
        var execution = await _executionRepository.GetQueryable()
            .Where(x => x.Id == executionId && x.UserId == _currentUser.UserId)
            .FirstAsync();
        if (execution == null)
        {
            throw new KeyNotFoundException("Tool execution was not found.");
        }

        var conversation = await _conversationRepository.GetQueryable()
            .Where(x => x.Id == execution.ConversationId && x.UserId == _currentUser.UserId && x.ProjectId == execution.ProjectId)
            .FirstAsync();
        if (conversation == null)
        {
            throw new UnauthorizedAccessException("Conversation context is invalid.");
        }

        return execution;
    }

    private async Task<ProjectDto> GetProjectAsync(long projectId)
    {
        var project = await _projectService.GetByIdAsync(projectId);
        return project ?? throw new KeyNotFoundException("The project bound to this conversation was not found.");
    }

    private async Task ValidateEntityProposalAsync(long projectId, CreateEntityProposal proposal)
    {
        if (string.IsNullOrWhiteSpace(proposal.Name) || string.IsNullOrWhiteSpace(proposal.Description))
        {
            throw new ArgumentException("Entity name and description are required.");
        }

        var module = await _moduleService.GetByIdAsync(proposal.ModuleId);
        if (module == null || module.ProjectId != projectId)
        {
            throw new ArgumentException("The selected module does not belong to the conversation project.");
        }

        if (proposal.Fields.Count == 0)
        {
            throw new ArgumentException("At least one business field is required.");
        }

        foreach (var field in proposal.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.Description) || string.IsNullOrWhiteSpace(field.DataType))
            {
                throw new ArgumentException("Every field requires a name, description, and data type.");
            }
        }
    }

    private async Task ValidateUiPageEnhancementAsync(long projectId, UiPageEnhancementProposal proposal)
    {
        if (string.IsNullOrWhiteSpace(proposal.Summary))
            throw new ArgumentException("UI enhancement summary is required.");
        if (!IsValidColor(proposal.PrimaryColor) || !IsValidColor(proposal.SecondaryColor))
            throw new ArgumentException("UI colors must use six-digit hex format, for example #2563eb.");

        if (string.Equals(proposal.TargetKind, "Scaffold", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(proposal.Page, "Login", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(proposal.Page, "Dashboard", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Scaffold page must be Login or Dashboard.");
            proposal.Page = string.Equals(proposal.Page, "Login", StringComparison.OrdinalIgnoreCase)
                ? "Login"
                : "Dashboard";
            proposal.TargetKind = "Scaffold";
            return;
        }

        if (!string.Equals(proposal.TargetKind, "EntityPage", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("TargetKind must be Scaffold or EntityPage.");
        proposal.TargetKind = "EntityPage";

        var entity = await _blueprintService.GetEntityAsync(projectId, proposal.EntityId, proposal.EntityName);
        proposal.EntityId = entity.Id;
        proposal.EntityName = entity.Name;
        var pageType = proposal.PageType?.Trim().ToLowerInvariant();
        if (pageType is not ("index" or "add" or "edit" or "detail"))
            throw new ArgumentException("Entity page type must be index, add, edit, or detail.");
        proposal.PageType = pageType;
        if (proposal.Operations.Count == 0)
            throw new ArgumentException("EntityPage enhancement requires at least one structured operation.");

        var page = await _blueprintService.GetUiPageAsync(projectId, entity.Id, null, pageType);
        var validGenIds = page.StableNodes.Select(item => item.GenId)
            .Concat(page.Fields.SelectMany(item => item.GenIds))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var operationTypes = new HashSet<string>(page.SupportedOperations, StringComparer.OrdinalIgnoreCase);

        foreach (var operation in proposal.Operations)
        {
            if (string.IsNullOrWhiteSpace(operation.Type) || !operationTypes.Contains(operation.Type))
                throw new ArgumentException($"Unsupported UI operation: {operation.Type}");

            if (!string.IsNullOrWhiteSpace(operation.TargetGenId) &&
                !validGenIds.Contains(operation.TargetGenId) &&
                !IsRelationGenId(operation.TargetGenId))
            {
                throw new ArgumentException($"Target genId is not part of the page blueprint: {operation.TargetGenId}");
            }

            if (operation.Type.Equals("Group", StringComparison.OrdinalIgnoreCase))
            {
                if (operation.MemberGenIds.Count == 0)
                    throw new ArgumentException("Group operation requires member genIds.");
                var invalidMember = operation.MemberGenIds.FirstOrDefault(item =>
                    !validGenIds.Contains(item) && !IsRelationGenId(item));
                if (invalidMember != null)
                    throw new ArgumentException($"Group member genId is not part of the page blueprint: {invalidMember}");
            }
        }
    }

    private static bool IsValidColor(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || Regex.IsMatch(value.Trim(), "^#[0-9a-fA-F]{6}$");
    }

    private static bool IsRelationGenId(string value)
    {
        return value.StartsWith("gen_child_", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("gen_owned_", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateModuleProposal(CreateModuleProposal proposal)
    {
        if (string.IsNullOrWhiteSpace(proposal.ModuleName) || string.IsNullOrWhiteSpace(proposal.ModuleDescription))
        {
            throw new ArgumentException("Module name and description are required.");
        }
    }

    private static T DeserializeRequired<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Tool input is missing.");
        }

        return JsonSerializer.Deserialize<T>(AiToolPayloadCodec.Decode(json), JsonOptions)
            ?? throw new InvalidOperationException("Tool input is invalid.");
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    internal static AiToolExecutionDto ToDto(AiToolExecution entity)
    {
        return new AiToolExecutionDto
        {
            Id = entity.Id,
            ConversationId = entity.ConversationId,
            ProjectId = entity.ProjectId,
            RequestId = entity.RequestId,
            ToolName = entity.ToolName,
            InputJson = string.IsNullOrWhiteSpace(entity.InputJson) ? entity.InputJson : AiToolPayloadCodec.Decode(entity.InputJson),
            OutputJson = string.IsNullOrWhiteSpace(entity.OutputJson) ? entity.OutputJson : AiToolPayloadCodec.Decode(entity.OutputJson),
            Status = entity.Status,
            ErrorMessage = entity.ErrorMessage,
            CreateTime = entity.CreateTime
        };
    }
}
