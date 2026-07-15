using CodeMaster.Application.Dtos.Auth;
using CodeMaster.Application.Services.Auth;
using CodeMaster.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeMaster.WebApi.Controllers;

[ApiController]
[Route("api/mcp/token")]
[Authorize]
public class McpTokenController : ControllerBase
{
    private readonly IMcpTokenService _mcpTokenService;

    public McpTokenController(IMcpTokenService mcpTokenService)
    {
        _mcpTokenService = mcpTokenService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var result = await _mcpTokenService.GetCurrentUserTokensAsync();
        return Ok(ApiResponse<List<McpTokenDto>>.Success(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMcpTokenDto dto)
    {
        var result = await _mcpTokenService.CreateTokenAsync(dto);
        return Ok(ApiResponse<CreateMcpTokenResultDto>.Success(result, "MCP token created"));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Revoke(long id)
    {
        var result = await _mcpTokenService.RevokeTokenAsync(id);
        return Ok(ApiResponse<bool>.Success(result, result ? "MCP token revoked" : "MCP token not found"));
    }
}
