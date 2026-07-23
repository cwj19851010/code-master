using CodeMaster.Agent.Contracts;
using CodeMaster.Agent.Services;
using CodeMaster.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeMaster.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/agent")]
public sealed class AgentController : ControllerBase
{
    private readonly IAiProviderService _providerService;
    private readonly IAiConversationService _conversationService;

    public AgentController(
        IAiProviderService providerService,
        IAiConversationService conversationService)
    {
        _providerService = providerService;
        _conversationService = conversationService;
    }

    [HttpGet("providers")]
    public Task<IActionResult> GetProviders() => ExecuteAsync(async () => await _providerService.GetListAsync());

    [HttpPost("providers")]
    public Task<IActionResult> CreateProvider([FromBody] SaveAiProviderRequest input)
        => ExecuteAsync(async () => await _providerService.CreateAsync(input));

    [HttpPut("providers/{id:long}")]
    public Task<IActionResult> UpdateProvider(long id, [FromBody] SaveAiProviderRequest input)
        => ExecuteAsync(async () => await _providerService.UpdateAsync(id, input));

    [HttpDelete("providers/{id:long}")]
    public Task<IActionResult> DeleteProvider(long id)
        => ExecuteAsync(async () => await _providerService.DeleteAsync(id));

    [HttpPost("providers/{id:long}/test")]
    public Task<IActionResult> TestProvider(long id)
        => ExecuteAsync(async () => await _providerService.TestAsync(id));

    [HttpGet("conversations")]
    public Task<IActionResult> GetConversations([FromQuery] long projectId)
        => ExecuteAsync(async () => await _conversationService.GetListAsync(projectId));

    [HttpPost("conversations")]
    public Task<IActionResult> CreateConversation([FromBody] CreateAiConversationRequest input)
        => ExecuteAsync(async () => await _conversationService.CreateAsync(input));

    [HttpDelete("conversations/{id:long}")]
    public Task<IActionResult> ArchiveConversation(long id)
        => ExecuteAsync(async () => await _conversationService.ArchiveAsync(id));

    [HttpGet("conversations/{id:long}/messages")]
    public Task<IActionResult> GetMessages(long id)
        => ExecuteAsync(async () => await _conversationService.GetMessagesAsync(id));

    [HttpGet("conversations/{id:long}/tools")]
    public Task<IActionResult> GetToolExecutions(long id)
        => ExecuteAsync(async () => await _conversationService.GetToolExecutionsAsync(id));

    [HttpPost("chat")]
    public Task<IActionResult> Chat([FromBody] SendAiMessageRequest input)
        => ExecuteAsync(async () => await _conversationService.SendAsync(input));

    [HttpPost("local/chat/begin")]
    public Task<IActionResult> BeginLocalChat([FromBody] SendAiMessageRequest input)
        => ExecuteAsync(async () => await _conversationService.BeginLocalAsync(input));

    [HttpPost("local/chat/invoke-tool")]
    public Task<IActionResult> InvokeLocalTool([FromBody] InvokeLocalAiToolRequest input)
        => ExecuteAsync(async () => await _conversationService.InvokeLocalToolAsync(input));

    [HttpPost("local/chat/complete")]
    public Task<IActionResult> CompleteLocalChat([FromBody] CompleteLocalAiChatRequest input)
        => ExecuteAsync(async () => await _conversationService.CompleteLocalAsync(input));

    [HttpPost("tools/{id:long}/approve")]
    public Task<IActionResult> ApproveTool(long id)
        => ExecuteAsync(async () => await _conversationService.ApproveAsync(id));

    [HttpPost("tools/{id:long}/reject")]
    public Task<IActionResult> RejectTool(long id)
        => ExecuteAsync(async () => await _conversationService.RejectAsync(id));

    [HttpPost("tools/{id:long}/complete-client-actions")]
    public Task<IActionResult> CompleteClientActions(long id, [FromBody] CompleteAiClientActionsRequest input)
        => ExecuteAsync(async () => await _conversationService.CompleteClientActionsAsync(id, input));

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return Ok(ApiResponse<T>.Success(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
